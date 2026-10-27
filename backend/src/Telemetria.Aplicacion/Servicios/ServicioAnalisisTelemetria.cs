namespace Telemetria.Aplicacion.Servicios;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Dominio.Contratos.Repositorios;
using Telemetria.Dominio.Modelos;

/// <summary>
/// Servicio de análisis post-sesión que implementa consultas paginadas, proyecciones a DTO
/// y algoritmos de interpolación espacial de telemetría e integración trapezoidal de tiempos.
/// </summary>
public sealed class ServicioAnalisisTelemetria : IServicioAnalisisTelemetria
{
    private const int PAGINA_MINIMA = 1;
    private const int TAMANIO_PAGINA_MINIMO = 1;
    private const int TAMANIO_PAGINA_MAXIMO = 100;
    private const double VELOCIDAD_MINIMA_CALCULO_MS = 5.0; // 18 km/h mínimo para evitar singularidades
    private const double FACTOR_CONVERSION_KMH_A_MS = 3.6;
    private const float DISTANCIA_MINIMA_INTERPOLACION_METROS = 0.001f;
    private const double MILISEGUNDOS_POR_SEGUNDO = 1000.0;

    private readonly IRepositorioSesionesLocal _repositorioSesiones;
    private readonly IRepositorioVueltasLocal _repositorioVueltas;

    public ServicioAnalisisTelemetria(
        IRepositorioSesionesLocal repositorioSesiones,
        IRepositorioVueltasLocal repositorioVueltas)
    {
        _repositorioSesiones = repositorioSesiones;
        _repositorioVueltas = repositorioVueltas;
    }

    public async Task<PaginacionRespuesta<SesionResumenDto>> ObtenerSesionesPaginadasAsync(
        int numeroPagina,
        int tamanioPagina,
        CancellationToken tokenCancelacion = default)
    {
        int paginaValidada = Math.Max(PAGINA_MINIMA, numeroPagina);
        int tamanioValidado = Math.Clamp(tamanioPagina, TAMANIO_PAGINA_MINIMO, TAMANIO_PAGINA_MAXIMO);

        var sesiones = await _repositorioSesiones.ObtenerHistorialSesionesAsync(paginaValidada, tamanioValidado, tokenCancelacion);
        int totalRegistros = await _repositorioSesiones.ContarTotalSesionesAsync(tokenCancelacion);

        var elementosDto = sesiones.Select(sesion => new SesionResumenDto
        {
            Id = sesion.Id,
            Circuito = sesion.Circuito,
            ConfiguracionCircuito = sesion.ConfiguracionCircuito,
            ModeloVehiculo = sesion.ModeloVehiculo,
            TipoSesion = sesion.TipoSesion,
            EsSimulacionMock = sesion.EsSimulacionMock,
            FechaInicio = sesion.FechaInicio,
            FechaFin = sesion.FechaFin,
            TotalVueltas = sesion.TotalVueltas,
            MejorTiempoMs = sesion.MejorTiempoMs
        }).ToList();

        return new PaginacionRespuesta<SesionResumenDto>
        {
            Elementos = elementosDto,
            PaginaActual = paginaValidada,
            TamanioPagina = tamanioValidado,
            TotalRegistros = totalRegistros
        };
    }

    public async Task<List<VueltaDto>> ObtenerVueltasPorSesionAsync(
        Guid sesionId,
        CancellationToken tokenCancelacion = default)
    {
        var vueltas = await _repositorioVueltas.ObtenerVueltasPorSesionAsync(sesionId, tokenCancelacion);

        return vueltas.Select(vuelta => new VueltaDto
        {
            Id = vuelta.Id,
            NumeroVuelta = vuelta.NumeroVuelta,
            TiempoVueltaMs = vuelta.TiempoVueltaMs,
            TiempoVueltaTexto = vuelta.TiempoVueltaTexto,
            Sector1Ms = vuelta.Sector1Ms,
            Sector2Ms = vuelta.Sector2Ms,
            Sector3Ms = vuelta.Sector3Ms,
            EsValida = vuelta.EsValida,
            EsMejorVuelta = vuelta.EsMejorVuelta
        }).ToList();
    }

    public async Task<List<MuestraTelemetriaDto>> ObtenerTelemetriaDeVueltaAsync(
        Guid vueltaId,
        CancellationToken tokenCancelacion = default)
    {
        var muestras = await _repositorioVueltas.ObtenerTelemetriaVueltaAsync(vueltaId, tokenCancelacion);

        return muestras.Select(muestra => new MuestraTelemetriaDto
        {
            DistanciaPistaMetros = (float)Math.Round(muestra.DistanciaPistaMetros, 2),
            VelocidadKmh = (float)Math.Round(muestra.VelocidadKmh, 1),
            Acelerador = muestra.Acelerador,
            Freno = muestra.Freno,
            Marcha = muestra.Marcha,
            Rpms = muestra.Rpms,
            FuerzaGLat = (float)Math.Round(muestra.FuerzaGLat, 2),
            FuerzaGLon = (float)Math.Round(muestra.FuerzaGLon, 2),
            PosicionX = (float)Math.Round(muestra.PosicionX, 2),
            PosicionY = (float)Math.Round(muestra.PosicionY, 2),
            PosicionZ = (float)Math.Round(muestra.PosicionZ, 2)
        }).ToList();
    }

    public async Task<ComparativaVueltasDto?> CompararVueltasPorDistanciaAsync(
        Guid vueltaBaseId,
        Guid vueltaComparadaId,
        CancellationToken tokenCancelacion = default)
    {
        var telemetriaBase = await _repositorioVueltas.ObtenerTelemetriaVueltaAsync(vueltaBaseId, tokenCancelacion);
        var telemetriaComparada = await _repositorioVueltas.ObtenerTelemetriaVueltaAsync(vueltaComparadaId, tokenCancelacion);

        if (telemetriaBase.Count == 0 || telemetriaComparada.Count == 0)
        {
            return null;
        }

        var puntosComparados = ConstruirSerieInterpoladaPorDistancia(telemetriaBase, telemetriaComparada);

        return new ComparativaVueltasDto
        {
            VueltaBaseId = vueltaBaseId,
            VueltaComparadaId = vueltaComparadaId,
            TotalMuestrasBase = telemetriaBase.Count,
            TotalMuestrasComparada = telemetriaComparada.Count,
            PuntosComparados = puntosComparados
        };
    }

    private static List<PuntoComparativaTelemetriaDto> ConstruirSerieInterpoladaPorDistancia(
        List<MuestraTelemetria> muestrasBase,
        List<MuestraTelemetria> muestrasComparada)
    {
        var resultado = new List<PuntoComparativaTelemetriaDto>(muestrasBase.Count);
        int indiceComparada = 0;
        double deltaTiempoAcumuladoSegundos = 0.0;
        float distanciaAnterior = muestrasBase[0].DistanciaPistaMetros;
        double velocidadBaseAnteriorMs = Math.Max(VELOCIDAD_MINIMA_CALCULO_MS, muestrasBase[0].VelocidadKmh / FACTOR_CONVERSION_KMH_A_MS);
        double velocidadComparadaAnteriorMs = Math.Max(VELOCIDAD_MINIMA_CALCULO_MS, muestrasComparada[0].VelocidadKmh / FACTOR_CONVERSION_KMH_A_MS);

        foreach (var muestraBase in muestrasBase)
        {
            while (indiceComparada + 1 < muestrasComparada.Count &&
                   muestrasComparada[indiceComparada + 1].DistanciaPistaMetros <= muestraBase.DistanciaPistaMetros)
            {
                indiceComparada++;
            }

            var (velocidadComparada, aceleradorComparado, frenoComparado) = InterpolarMuestraEnDistancia(
                muestrasComparada, indiceComparada, muestraBase.DistanciaPistaMetros);

            float tramoMetros = Math.Max(0.0f, muestraBase.DistanciaPistaMetros - distanciaAnterior);
            distanciaAnterior = muestraBase.DistanciaPistaMetros;

            double velocidadBaseActualMs = Math.Max(VELOCIDAD_MINIMA_CALCULO_MS, muestraBase.VelocidadKmh / FACTOR_CONVERSION_KMH_A_MS);
            double velocidadComparadaActualMs = Math.Max(VELOCIDAD_MINIMA_CALCULO_MS, velocidadComparada / FACTOR_CONVERSION_KMH_A_MS);

            double velocidadMediaBaseMs = 0.5 * (velocidadBaseAnteriorMs + velocidadBaseActualMs);
            double velocidadMediaComparadaMs = 0.5 * (velocidadComparadaAnteriorMs + velocidadComparadaActualMs);
            deltaTiempoAcumuladoSegundos += (tramoMetros / velocidadMediaComparadaMs) - (tramoMetros / velocidadMediaBaseMs);

            velocidadBaseAnteriorMs = velocidadBaseActualMs;
            velocidadComparadaAnteriorMs = velocidadComparadaActualMs;

            resultado.Add(CrearPuntoComparativa(
                muestraBase, velocidadComparada, aceleradorComparado, frenoComparado, deltaTiempoAcumuladoSegundos));
        }

        return resultado;
    }

    private static (float velocidadKmh, float acelerador, float freno) InterpolarMuestraEnDistancia(
        List<MuestraTelemetria> muestras,
        int indiceInferior,
        float distanciaObjetivo)
    {
        var muestraInferior = muestras[indiceInferior];
        if (distanciaObjetivo <= muestraInferior.DistanciaPistaMetros || indiceInferior + 1 >= muestras.Count)
        {
            return (muestraInferior.VelocidadKmh, muestraInferior.Acelerador, muestraInferior.Freno);
        }

        var muestraSuperior = muestras[indiceInferior + 1];
        float rangoDistancia = muestraSuperior.DistanciaPistaMetros - muestraInferior.DistanciaPistaMetros;
        if (rangoDistancia <= DISTANCIA_MINIMA_INTERPOLACION_METROS)
        {
            return (muestraInferior.VelocidadKmh, muestraInferior.Acelerador, muestraInferior.Freno);
        }

        float factor = Math.Clamp((distanciaObjetivo - muestraInferior.DistanciaPistaMetros) / rangoDistancia, 0.0f, 1.0f);
        float velocidad = muestraInferior.VelocidadKmh + (muestraSuperior.VelocidadKmh - muestraInferior.VelocidadKmh) * factor;
        float acelerador = muestraInferior.Acelerador + (muestraSuperior.Acelerador - muestraInferior.Acelerador) * factor;
        float freno = muestraInferior.Freno + (muestraSuperior.Freno - muestraInferior.Freno) * factor;

        return (velocidad, acelerador, freno);
    }

    private static PuntoComparativaTelemetriaDto CrearPuntoComparativa(
        MuestraTelemetria muestraBase,
        float velocidadComparada,
        float aceleradorComparado,
        float frenoComparado,
        double deltaTiempoSegundos)
    {
        return new PuntoComparativaTelemetriaDto
        {
            DistanciaPistaMetros = (float)Math.Round(muestraBase.DistanciaPistaMetros, 2),
            VelocidadBaseKmh = (float)Math.Round(muestraBase.VelocidadKmh, 1),
            VelocidadComparadaKmh = (float)Math.Round(velocidadComparada, 1),
            DeltaVelocidadKmh = (float)Math.Round(velocidadComparada - muestraBase.VelocidadKmh, 1),
            AceleradorBase = muestraBase.Acelerador,
            AceleradorComparada = (float)Math.Round(aceleradorComparado, 3),
            FrenoBase = muestraBase.Freno,
            FrenoComparada = (float)Math.Round(frenoComparado, 3),
            DeltaTiempoAcumuladoMs = (int)Math.Round(deltaTiempoSegundos * MILISEGUNDOS_POR_SEGUNDO)
        };
    }
}
