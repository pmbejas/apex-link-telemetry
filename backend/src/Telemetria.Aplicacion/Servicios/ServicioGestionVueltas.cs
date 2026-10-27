namespace Telemetria.Aplicacion.Servicios;

using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Aplicacion.Mappings;
using Telemetria.Dominio.Contratos.Repositorios;
using Telemetria.Dominio.Modelos;

/// <summary>
/// Servicio en segundo plano que consume muestras del canal en memoria,
/// acumula la telemetría del giro actual y persiste la vuelta transaccionalmente al cruzar meta.
/// </summary>
public sealed class ServicioGestionVueltas : BackgroundService
{
    private const int MAXIMO_MUESTRAS_POR_VUELTA = 36000; // Cota máxima: 10 minutos a 60 Hz

    private readonly ChannelReader<CuadroTelemetria> _canalLector;
    private readonly IServiceScopeFactory _factoriaAlcances;
    private readonly IDifusorTelemetria? _difusor;
    private readonly ILogger<ServicioGestionVueltas> _logger;

    private readonly List<MuestraTelemetria> _bufferMuestrasVuelta = new(capacity: 7200);
    private Guid? _sesionActivaId;
    private string _circuitoActivo = string.Empty;
    private string _modeloAutoActivo = string.Empty;
    private int _tipoSesionActiva = -1;
    private bool _modoMockActivo = false;
    private int _numeroVueltaActual = -1;
    private int _mejorTiempoSesionMs = 0;

    public ServicioGestionVueltas(
        ChannelReader<CuadroTelemetria> canalLector,
        IServiceScopeFactory factoriaAlcances,
        ILogger<ServicioGestionVueltas> logger,
        IDifusorTelemetria? difusor = null)
    {
        _canalLector = canalLector;
        _factoriaAlcances = factoriaAlcances;
        _logger = logger;
        _difusor = difusor;
    }

    protected override async Task ExecuteAsync(CancellationToken tokenDetencion)
    {
        _logger.LogInformation("Iniciando servicio consumidor de persistencia de telemetría...");

        try
        {
            await foreach (CuadroTelemetria cuadro in _canalLector.ReadAllAsync(tokenDetencion))
            {
                await ProcesarCuadroTelemetriaAsync(cuadro, tokenDetencion);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelación esperada durante el apagado normal del host
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error crítico en el consumidor de persistencia de telemetría.");
        }
        finally
        {
            await FinalizarSesionActivaAsync(CancellationToken.None);
        }
    }

    private async Task ProcesarCuadroTelemetriaAsync(CuadroTelemetria cuadro, CancellationToken token)
    {
        if (cuadro.EstadoSimulador == 0)
        {
            await CerrarSesionSiEstabaActivaAsync(token);
            return;
        }

        // Ignorar repetición (1) o pausa (3) para no registrar telemetría no competitiva
        if (cuadro.EstadoSimulador != 2 || string.IsNullOrWhiteSpace(cuadro.Circuito))
        {
            return;
        }

        if (DebeReiniciarSesionActiva(cuadro))
        {
            await CerrarSesionSiEstabaActivaAsync(token);
        }

        if (!_sesionActivaId.HasValue)
        {
            await AbrirNuevaSesionAsync(cuadro, token);
        }

        if (cuadro.VueltasCompletadas > _numeroVueltaActual)
        {
            await GuardarVueltaCerradaAsync(cuadro, token);
            _numeroVueltaActual = cuadro.VueltasCompletadas;
        }

        AcumularMuestraEnBuffer(cuadro);
    }

    private bool DebeReiniciarSesionActiva(CuadroTelemetria cuadro)
    {
        if (!_sesionActivaId.HasValue)
        {
            return false;
        }

        bool cambioContexto = !string.Equals(_circuitoActivo, cuadro.Circuito, StringComparison.OrdinalIgnoreCase) ||
                              !string.Equals(_modeloAutoActivo, cuadro.ModeloAuto, StringComparison.OrdinalIgnoreCase) ||
                              _tipoSesionActiva != cuadro.TipoSesion ||
                              _modoMockActivo != cuadro.EsSimulacionMock;

        bool reinicioContador = _numeroVueltaActual >= 0 && cuadro.VueltasCompletadas < _numeroVueltaActual;
        return cambioContexto || reinicioContador;
    }

    private async Task CerrarSesionSiEstabaActivaAsync(CancellationToken token)
    {
        if (!_sesionActivaId.HasValue)
        {
            return;
        }

        _bufferMuestrasVuelta.Clear();
        await FinalizarSesionActivaAsync(token);
    }

    private async Task AbrirNuevaSesionAsync(CuadroTelemetria cuadro, CancellationToken token)
    {
        using var alcance = _factoriaAlcances.CreateScope();
        var repositorioSesiones = alcance.ServiceProvider.GetRequiredService<IRepositorioSesionesLocal>();

        _sesionActivaId = await repositorioSesiones.IniciarSesionAsync(
            cuadro.Circuito,
            cuadro.ConfiguracionCircuito,
            cuadro.ModeloAuto,
            cuadro.TipoSesion,
            cuadro.EsSimulacionMock,
            token);

        _circuitoActivo = cuadro.Circuito;
        _modeloAutoActivo = cuadro.ModeloAuto;
        _tipoSesionActiva = cuadro.TipoSesion;
        _modoMockActivo = cuadro.EsSimulacionMock;
        _numeroVueltaActual = cuadro.VueltasCompletadas;
        _mejorTiempoSesionMs = 0;

        _logger.LogInformation("Nueva sesión de telemetría iniciada: {SesionId} en {Circuito} ({Auto}, Mock={EsMock})",
            _sesionActivaId, cuadro.Circuito, cuadro.ModeloAuto, cuadro.EsSimulacionMock);
    }

    private void AcumularMuestraEnBuffer(CuadroTelemetria cuadro)
    {
        if (_bufferMuestrasVuelta.Count >= MAXIMO_MUESTRAS_POR_VUELTA)
        {
            return;
        }

        _bufferMuestrasVuelta.Add(new MuestraTelemetria
        {
            MarcaTemporalUnixMs = cuadro.MarcaTemporalUnixMs,
            DistanciaPistaMetros = cuadro.DistanciaMetros,
            VelocidadKmh = cuadro.VelocidadKmh,
            Acelerador = cuadro.Acelerador,
            Freno = cuadro.Freno,
            Marcha = cuadro.Marcha,
            Rpms = cuadro.Rpms,
            FuerzaGLat = cuadro.FuerzaGLat,
            FuerzaGLon = cuadro.FuerzaGLon,
            PosicionX = cuadro.PosicionX,
            PosicionY = cuadro.PosicionY,
            PosicionZ = cuadro.PosicionZ
        });
    }

    private async Task GuardarVueltaCerradaAsync(CuadroTelemetria cuadro, CancellationToken token)
    {
        if (_bufferMuestrasVuelta.Count == 0 || !_sesionActivaId.HasValue)
        {
            return;
        }

        int tiempoVueltaMs = cuadro.UltimoTiempoVueltaMs;
        // Descartar vueltas de salida (out-laps) o giros incompletos sin tiempo oficial registrado
        if (tiempoVueltaMs <= 0)
        {
            _bufferMuestrasVuelta.Clear();
            return;
        }

        bool esRecord = (_mejorTiempoSesionMs == 0 || tiempoVueltaMs < _mejorTiempoSesionMs) && cuadro.EsVueltaValida;
        if (esRecord)
        {
            _mejorTiempoSesionMs = tiempoVueltaMs;
        }

        int[] sectores = MapeadorTelemetria.ConsolidarSectoresMs(cuadro.TiemposSectoresMs, tiempoVueltaMs);
        Guid vueltaId = cuadro.VueltaCerradaId ?? Guid.NewGuid();

        var nuevaVuelta = new Vuelta
        {
            Id = vueltaId,
            SesionId = _sesionActivaId.Value,
            NumeroVuelta = cuadro.VueltasCompletadas,
            TiempoVueltaMs = tiempoVueltaMs,
            TiempoVueltaTexto = cuadro.UltimoTiempoVueltaTexto,
            Sector1Ms = sectores[0],
            Sector2Ms = sectores[1],
            Sector3Ms = sectores[2],
            EsValida = cuadro.EsVueltaValida,
            EsMejorVuelta = esRecord
        };

        var muestrasCopia = new List<MuestraTelemetria>(_bufferMuestrasVuelta);
        _bufferMuestrasVuelta.Clear();

        using (var alcance = _factoriaAlcances.CreateScope())
        {
            var repositorioVueltas = alcance.ServiceProvider.GetRequiredService<IRepositorioVueltasLocal>();
            await repositorioVueltas.GuardarVueltaConTelemetriaAsync(nuevaVuelta, muestrasCopia, token);
        }

        if (_difusor is not null)
        {
            CuadroTelemetria cuadroConsolidado = cuadro with
            {
                TiemposSectoresMs = sectores,
                EsMejorVueltaCerrada = esRecord
            };
            EventoVueltaCompletadaDto eventoVuelta = MapeadorTelemetria.MapearAEventoVueltaDto(
                cuadroConsolidado, vueltaId, muestrasCopia.Count);
            await _difusor.EmitirEventoVueltaAsync(eventoVuelta, token);
        }

        _logger.LogInformation("Vuelta #{Vuelta} (Id={VueltaId}) guardada y notificada exitosamente ({TiempoTexto}, {CantidadMuestras} muestras)",
            nuevaVuelta.NumeroVuelta, nuevaVuelta.Id, nuevaVuelta.TiempoVueltaTexto, muestrasCopia.Count);
    }

    private async Task FinalizarSesionActivaAsync(CancellationToken token)
    {
        if (!_sesionActivaId.HasValue)
        {
            return;
        }

        try
        {
            using var alcance = _factoriaAlcances.CreateScope();
            var repositorioSesiones = alcance.ServiceProvider.GetRequiredService<IRepositorioSesionesLocal>();
            await repositorioSesiones.FinalizarSesionAsync(_sesionActivaId.Value, token);
            _logger.LogInformation("Sesión de telemetría finalizada: {SesionId}", _sesionActivaId.Value);
            _sesionActivaId = null;
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al intentar finalizar la sesión activa durante el cierre.");
        }
    }
}
