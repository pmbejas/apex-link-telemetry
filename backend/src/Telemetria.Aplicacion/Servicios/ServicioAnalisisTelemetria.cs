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
        Guid? usuarioId = null,
        CancellationToken tokenCancelacion = default)
    {
        int paginaValidada = Math.Max(PAGINA_MINIMA, numeroPagina);
        int tamanioValidado = Math.Clamp(tamanioPagina, TAMANIO_PAGINA_MINIMO, TAMANIO_PAGINA_MAXIMO);

        if (usuarioId.HasValue && usuarioId.Value != Guid.Empty && !usuarioId.Value.ToString().StartsWith("00000000"))
        {
            await _repositorioSesiones.AsignarUsuarioASesionesSinPilotoAsync(usuarioId.Value, tokenCancelacion);
        }

        var sesiones = await _repositorioSesiones.ObtenerHistorialSesionesAsync(paginaValidada, tamanioValidado, usuarioId, tokenCancelacion);
        int totalRegistros = await _repositorioSesiones.ContarTotalSesionesAsync(usuarioId, tokenCancelacion);

        var elementosDto = sesiones.Select(sesion => new SesionResumenDto
        {
            Id = sesion.Id,
            Circuito = sesion.Circuito,
            ConfiguracionCircuito = sesion.ConfiguracionCircuito,
            ModeloVehiculo = FormatearNombreVehiculo(sesion.ModeloVehiculo),
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

    public async Task<bool> EliminarSesionAsync(
        Guid sesionId,
        CancellationToken tokenCancelacion = default)
    {
        return await _repositorioSesiones.EliminarSesionAsync(sesionId, tokenCancelacion);
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

    public async Task<List<LeaderboardItemDto>> ObtenerLeaderboardAsync(
        string? circuito,
        int limite = 10,
        CancellationToken tokenCancelacion = default)
    {
        string circuitoFiltro = string.IsNullOrWhiteSpace(circuito) ? "monza" : circuito.Trim();
        var vueltas = await _repositorioVueltas.ObtenerMejoresVueltasPorCircuitoAsync(circuitoFiltro, limite, tokenCancelacion);

        var vueltasReales = vueltas
            .Where(v => v.Sesion == null || !v.Sesion.EsSimulacionMock)
            .ToList();

        var baseReferencia = ObtenerLeaderboardBase(circuitoFiltro);

        if (vueltasReales.Count == 0)
        {
            return new List<LeaderboardItemDto>();
        }

        var guidPilotoDefault = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var mejoresPorPiloto = vueltasReales
            .GroupBy(v => v.Sesion?.UsuarioId ?? guidPilotoDefault)
            .Select(grupo =>
            {
                var mejorVuelta = grupo.OrderBy(v => v.TiempoVueltaMs).First();
                int totalVueltasPiloto = grupo
                    .Where(v => v.Sesion != null)
                    .Select(v => v.Sesion!)
                    .DistinctBy(s => s.Id)
                    .Sum(s => Math.Max(1, s.TotalVueltas));
                if (totalVueltasPiloto == 0) totalVueltasPiloto = grupo.Count();
                return (UsuarioId: grupo.Key, MejorVuelta: mejorVuelta, TotalVueltas: totalVueltasPiloto);
            })
            .ToList();

        var itemsCombinados = new List<LeaderboardItemDto>();
        var aliasesReales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < mejoresPorPiloto.Count; i++)
        {
            var entrada = mejoresPorPiloto[i];
            var (nombrePiloto, aliasPiloto) = ObtenerNombrePiloto(entrada.UsuarioId, i);
            aliasesReales.Add(aliasPiloto);

            itemsCombinados.Add(new LeaderboardItemDto
            {
                Posicion = 0,
                Piloto = nombrePiloto,
                Alias = aliasPiloto,
                Tiempo = entrada.MejorVuelta.TiempoVueltaTexto,
                TiempoMs = entrada.MejorVuelta.TiempoVueltaMs,
                Delta = "Ref",
                Auto = FormatearNombreVehiculo(entrada.MejorVuelta.Sesion?.ModeloVehiculo),
                Circuito = entrada.MejorVuelta.Sesion?.Circuito ?? circuitoFiltro,
                Vueltas = entrada.TotalVueltas,
                EsRecord = false
            });
        }

        var ordenados = itemsCombinados
            .OrderBy(item => item.TiempoMs > 0 ? item.TiempoMs : int.MaxValue)
            .Take(Math.Max(1, limite))
            .ToList();

        int mejorTiempoGlobal = ordenados.Count > 0 ? ordenados[0].TiempoMs : 0;

        return ordenados.Select((item, idx) => new LeaderboardItemDto
        {
            Posicion = idx + 1,
            Piloto = item.Piloto,
            Alias = item.Alias,
            Tiempo = item.Tiempo,
            TiempoMs = item.TiempoMs,
            Delta = idx == 0 ? "Ref" : $"+{((item.TiempoMs - mejorTiempoGlobal) / 1000.0):F3} s",
            Auto = item.Auto,
            Circuito = item.Circuito,
            Vueltas = item.Vueltas,
            EsRecord = idx == 0
        }).ToList();
    }

    private static (string Piloto, string Alias) ObtenerNombrePiloto(Guid? usuarioId, int indice)
    {
        if (usuarioId.HasValue)
        {
            string idStr = usuarioId.Value.ToString().ToLowerInvariant();
            if (idStr.StartsWith("11111111") || idStr.StartsWith("a1a1a1a1") || idStr.Contains("garlati"))
            {
                return ("Alejo Garlati", "AG_PRO");
            }
            if (idStr.StartsWith("22222222") || idStr.StartsWith("b2b2b2b2") || idStr.Contains("salas"))
            {
                return ("Alejo Salas", "AS_GT3");
            }
            if (idStr.StartsWith("00000000") || idStr.StartsWith("c3c3c3c3") || idStr.Contains("admin"))
            {
                return ("Administrador", "ADMIN");
            }
            if (idStr.StartsWith("a0eebc99"))
            {
                return ("Piloto Invitado (FAPPE)", "PILOTO_01");
            }
        }
        return (indice == 0 ? "Alejo Garlati" : $"Piloto #{indice + 1}", indice == 0 ? "AG_PRO" : $"PILOTO_{indice + 1:D2}");
    }

    public static string FormatearNombreVehiculo(string? modelo)
    {
        if (string.IsNullOrWhiteSpace(modelo)) return "Ferrari SF70H";
        return modelo.ToLowerInvariant() switch
        {
            "rss_formula_rss_supreme_25" => "Formula RSS Supreme 2025",
            "ks_ferrari_sf70h" => "Ferrari SF70H",
            "ks_porsche_911_gt3_r_2016" => "Porsche 911 GT3 R",
            "bmw_m2_cs_racing" => "BMW M2 CS Racing",
            "tatuusfa01" => "Tatuus FA01",
            _ => modelo.Replace("_", " ").Trim()
        };
    }

    private static List<LeaderboardItemDto> ObtenerLeaderboardBase(string circuito)
    {
        return new List<LeaderboardItemDto>();
    }
}
