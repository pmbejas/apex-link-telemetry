namespace Telemetria.Aplicacion.Servicios;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Aplicacion.Mappings;
using Telemetria.Dominio.Contratos.Hardware;
using Telemetria.Dominio.Modelos;

public sealed class ServicioTelemetriaRelay : BackgroundService
{
    private const int ESTADO_SIMULADOR_EN_PISTA = 2;

    private readonly ILectorTelemetria _lectorTelemetria;
    private readonly IControlModoTelemetria? _controlModo;
    private readonly IDifusorTelemetria _difusor;
    private readonly ChannelWriter<CuadroTelemetria>? _canalPersistencia;
    private readonly ILogger<ServicioTelemetriaRelay> _logger;

    private readonly TimeSpan _periodoPista60Hz;
    private readonly TimeSpan _periodoReposo5Hz;
    private readonly TimeSpan _intervaloReconexion;

    private long _ultimaVersionConmutacion = -1;
    private int _ultimaVueltaRegistrada = -1;
    private int _mejorTiempoSesionMs = 0;
    private int _muestrasAcumuladasVuelta = 0;
    private string _circuitoActivo = string.Empty;
    private string _modeloAutoActivo = string.Empty;
    private int _tipoSesionActiva = -1;

    public ServicioTelemetriaRelay(
        ILectorTelemetria lectorTelemetria,
        IDifusorTelemetria difusor,
        IConfiguration configuracion,
        ILogger<ServicioTelemetriaRelay> logger,
        IControlModoTelemetria? controlModo = null,
        ChannelWriter<CuadroTelemetria>? canalPersistencia = null)
    {
        _lectorTelemetria = lectorTelemetria;
        _difusor = difusor;
        _logger = logger;
        _controlModo = controlModo;
        _canalPersistencia = canalPersistencia;

        double msPista = configuracion.GetValue<double>("Telemetria:FrecuenciaPistaMs", 16.6667);
        double msReposo = configuracion.GetValue<double>("Telemetria:FrecuenciaReposoMs", 200.0);
        double msReconexion = configuracion.GetValue<double>("Telemetria:BackoffReconexionMs", 1000.0);

        _periodoPista60Hz = TimeSpan.FromTicks((long)Math.Round(msPista * TimeSpan.TicksPerMillisecond));
        _periodoReposo5Hz = TimeSpan.FromMilliseconds(msReposo);
        _intervaloReconexion = TimeSpan.FromMilliseconds(msReconexion);
    }

    protected override async Task ExecuteAsync(CancellationToken tokenDetencion)
    {
        _logger.LogInformation("Iniciando bucle orquestador de telemetría con PeriodicTimer ({PeriodoPistaMs:F4} ms)...", _periodoPista60Hz.TotalMilliseconds);
        TimeSpan periodoActual = _periodoPista60Hz;
        using var temporizador = new PeriodicTimer(periodoActual);

        while (!tokenDetencion.IsCancellationRequested)
        {
            try
            {
                VerificarConmutacionDeModo();

                if (!_lectorTelemetria.EstaConectado() && !_lectorTelemetria.IntentarConectar())
                {
                    await Task.Delay(_intervaloReconexion, tokenDetencion);
                    continue;
                }

                CuadroTelemetria cuadroActual = _lectorTelemetria.ObtenerCuadroActual();
                cuadroActual = await ProcesarTransicionDeVueltaAsync(cuadroActual, tokenDetencion);

                if (cuadroActual.EstadoSimulador == ESTADO_SIMULADOR_EN_PISTA && !string.IsNullOrWhiteSpace(cuadroActual.Circuito))
                {
                    _muestrasAcumuladasVuelta++;
                }

                if (_difusor.TieneClientesConectados)
                {
                    PaqueteTelemetriaDto paqueteDto = MapeadorTelemetria.MapearAPaqueteDto(cuadroActual);
                    await _difusor.DifundirTelemetriaAsync(paqueteDto, tokenDetencion);
                }

                _canalPersistencia?.TryWrite(cuadroActual);
                ActualizarPeriodoSiCorresponde(temporizador, ref periodoActual, cuadroActual.EstadoSimulador);

                await temporizador.WaitForNextTickAsync(tokenDetencion);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception excepcion)
            {
                _logger.LogError(excepcion, "Excepción en el bucle de telemetría relay");
                await temporizador.WaitForNextTickAsync(tokenDetencion);
            }
        }
    }

    private void VerificarConmutacionDeModo()
    {
        if (_controlModo == null || _controlModo.VersionConmutacion == _ultimaVersionConmutacion)
        {
            return;
        }

        _ultimaVersionConmutacion = _controlModo.VersionConmutacion;
        ReiniciarEstadoSeguimientoSesion(-1, string.Empty, string.Empty, -1, 0);
    }

    private void ActualizarPeriodoSiCorresponde(
        PeriodicTimer temporizador,
        ref TimeSpan periodoActual,
        int estadoSimulador)
    {
        TimeSpan periodoObjetivo = estadoSimulador == ESTADO_SIMULADOR_EN_PISTA ? _periodoPista60Hz : _periodoReposo5Hz;
        if (periodoActual != periodoObjetivo)
        {
            periodoActual = periodoObjetivo;
            temporizador.Period = periodoObjetivo;
        }
    }

    private async Task<CuadroTelemetria> ProcesarTransicionDeVueltaAsync(
        CuadroTelemetria cuadroActual,
        CancellationToken tokenDetencion)
    {
        if (_ultimaVueltaRegistrada == -1 || CambioContextoDeSesion(cuadroActual))
        {
            int mejorTiempoInicial = cuadroActual.MejorTiempoMs > 0 ? cuadroActual.MejorTiempoMs : 0;
            ReiniciarEstadoSeguimientoSesion(
                cuadroActual.VueltasCompletadas,
                cuadroActual.Circuito,
                cuadroActual.ModeloAuto,
                cuadroActual.TipoSesion,
                mejorTiempoInicial);
            return cuadroActual;
        }

        if (cuadroActual.VueltasCompletadas < _ultimaVueltaRegistrada)
        {
            _logger.LogInformation("Reinicio de sesión detectado. Reset de vueltas: {Anterior} -> {Actual}",
                _ultimaVueltaRegistrada, cuadroActual.VueltasCompletadas);
            ReiniciarEstadoSeguimientoSesion(
                cuadroActual.VueltasCompletadas,
                cuadroActual.Circuito,
                cuadroActual.ModeloAuto,
                cuadroActual.TipoSesion,
                0);
            return cuadroActual;
        }

        if (cuadroActual.VueltasCompletadas == _ultimaVueltaRegistrada)
        {
            return cuadroActual;
        }

        if (cuadroActual.UltimoTiempoVueltaMs > 0)
        {
            cuadroActual = await ConsolidarCierreDeVueltaAsync(cuadroActual, tokenDetencion);
        }

        _ultimaVueltaRegistrada = cuadroActual.VueltasCompletadas;
        _muestrasAcumuladasVuelta = 0;
        return cuadroActual;
    }

    private async Task<CuadroTelemetria> ConsolidarCierreDeVueltaAsync(
        CuadroTelemetria cuadroActual,
        CancellationToken tokenDetencion)
    {
        Guid idVueltaCerrada = Guid.NewGuid();
        int tiempoCerradoMs = cuadroActual.UltimoTiempoVueltaMs;
        bool esRecord = (_mejorTiempoSesionMs == 0 || tiempoCerradoMs < _mejorTiempoSesionMs) && cuadroActual.EsVueltaValida;
        if (esRecord) _mejorTiempoSesionMs = tiempoCerradoMs;

        int[] sectoresConsolidados = MapeadorTelemetria.ConsolidarSectoresMs(cuadroActual.TiemposSectoresMs, tiempoCerradoMs);

        cuadroActual = cuadroActual with
        {
            TiemposSectoresMs = sectoresConsolidados,
            VueltaCerradaId = idVueltaCerrada,
            MuestrasVueltaCerrada = _muestrasAcumuladasVuelta,
            EsMejorVueltaCerrada = esRecord
        };

        // Si el relay opera sin canal de persistencia (ej. pruebas unitarias aisladas), emite el evento directamente
        if (_canalPersistencia is null)
        {
            EventoVueltaCompletadaDto eventoVuelta = MapeadorTelemetria.MapearAEventoVueltaDto(
                cuadroActual, idVueltaCerrada, _muestrasAcumuladasVuelta);
            await _difusor.EmitirEventoVueltaAsync(eventoVuelta, tokenDetencion);
        }

        return cuadroActual;
    }

    private bool CambioContextoDeSesion(CuadroTelemetria cuadro) =>
        !string.Equals(_circuitoActivo, cuadro.Circuito, StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(_modeloAutoActivo, cuadro.ModeloAuto, StringComparison.OrdinalIgnoreCase) ||
        _tipoSesionActiva != cuadro.TipoSesion;

    private void ReiniciarEstadoSeguimientoSesion(
        int vueltaInicial,
        string circuito,
        string modeloAuto,
        int tipoSesion,
        int mejorTiempoMs)
    {
        _ultimaVueltaRegistrada = vueltaInicial;
        _circuitoActivo = circuito;
        _modeloAutoActivo = modeloAuto;
        _tipoSesionActiva = tipoSesion;
        _mejorTiempoSesionMs = mejorTiempoMs;
        _muestrasAcumuladasVuelta = 0;
    }
}
