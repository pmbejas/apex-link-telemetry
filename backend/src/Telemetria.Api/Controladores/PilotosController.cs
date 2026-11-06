namespace Telemetria.Api.Controladores;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telemetria.Api.Autorizacion;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Dtos.Requests;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Aplicacion.Servicios;
using Telemetria.Dominio.Contratos.Repositorios;
using Telemetria.Dominio.Modelos;

/// <summary>
/// Controlador REST para consultar perfiles de piloto, evaluar tandas algorítmicamente y actualizar Apex Ratings.
/// </summary>
[ApiController]
[Route("api/pilotos")]
[Produces("application/json")]
public sealed class PilotosController : ControllerBase
{
    private readonly IRepositorioPerfilesPiloto _repositorioPerfiles;
    private readonly IMotorEvaluacionPiloto _motorEvaluacion;
    private readonly IRepositorioVueltasLocal _repositorioVueltas;
    private readonly IRepositorioSesionesLocal _repositorioSesiones;

    public PilotosController(
        IRepositorioPerfilesPiloto repositorioPerfiles,
        IMotorEvaluacionPiloto motorEvaluacion,
        IRepositorioVueltasLocal repositorioVueltas,
        IRepositorioSesionesLocal repositorioSesiones)
    {
        _repositorioPerfiles = repositorioPerfiles;
        _motorEvaluacion = motorEvaluacion;
        _repositorioVueltas = repositorioVueltas;
        _repositorioSesiones = repositorioSesiones;
    }

    [HttpGet("{id:guid}/perfil")]
    [HasPermission("perfil:leer")]
    [ProducesResponseType(typeof(RespuestaApi<PerfilPilotoDetalleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi<PerfilPilotoDetalleDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RespuestaApi<PerfilPilotoDetalleDto>>> ObtenerPerfil(
        Guid id,
        CancellationToken tokenCancelacion = default)
    {
        var perfil = await _repositorioPerfiles.ObtenerPerfilCompletoAsync(id, tokenCancelacion);
        if (perfil is null)
        {
            return NotFound(RespuestaApi<PerfilPilotoDetalleDto>.ConstruirFallo(
                "No se encontró el perfil de piloto especificado."));
        }

        var perfilEnriquecido = await EnriquecerPerfilConTelemetriaLocalAsync(perfil, id, tokenCancelacion);
        return Ok(RespuestaApi<PerfilPilotoDetalleDto>.ConstruirExito(perfilEnriquecido));
    }

    private async Task<PerfilPilotoDetalleDto> EnriquecerPerfilConTelemetriaLocalAsync(
        PerfilPilotoDetalleDto perfilBase,
        Guid usuarioId,
        CancellationToken token)
    {
        var sesiones = await _repositorioSesiones.ObtenerHistorialSesionesAsync(1, 100, usuarioId, token);
        bool esAdmin = usuarioId.ToString().StartsWith("00000000");

        var sesionesValidas = sesiones
            .Where(s => !s.EsSimulacionMock && s.TotalVueltas > 0)
            .ToList();

        if (sesionesValidas.Count == 0 && esAdmin)
        {
            sesionesValidas = sesiones.Where(s => s.TotalVueltas > 0).ToList();
        }

        if (sesionesValidas.Count == 0)
        {
            return perfilBase;
        }

        var todasLasVueltas = new List<Vuelta>();
        foreach (var sesion in sesionesValidas)
        {
            var vueltasSesion = await _repositorioVueltas.ObtenerVueltasPorSesionAsync(sesion.Id, token);
            foreach (var v in vueltasSesion)
            {
                v.Sesion = sesion;
                todasLasVueltas.Add(v);
            }
        }

        if (todasLasVueltas.Count == 0)
        {
            return perfilBase;
        }

        int totalVueltas = todasLasVueltas.Count;
        int vueltasValidasCount = todasLasVueltas.Count(v => v.EsValida);
        float porcentajeValidas = (float)Math.Round((vueltasValidasCount * 100.0) / totalVueltas, 1);
        float kmTotales = (float)Math.Round(totalVueltas * 5.793f, 1);

        string? modeloMasUsado = sesionesValidas
            .Where(s => !string.IsNullOrWhiteSpace(s.ModeloVehiculo))
            .GroupBy(s => s.ModeloVehiculo, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Sum(s => Math.Max(1, s.TotalVueltas)))
            .Select(g => g.Key)
            .FirstOrDefault();

        string vehiculoPrincipal = !string.IsNullOrWhiteSpace(modeloMasUsado)
            ? ServicioAnalisisTelemetria.FormatearNombreVehiculo(modeloMasUsado)
            : perfilBase.VehiculoPrincipal;

        var mejorVueltaGlobal = todasLasVueltas
            .Where(v => v.EsValida && v.TiempoVueltaMs > 0)
            .OrderBy(v => v.TiempoVueltaMs)
            .FirstOrDefault()
            ?? todasLasVueltas.OrderBy(v => v.TiempoVueltaMs > 0 ? v.TiempoVueltaMs : int.MaxValue).First();

        var muestrasReferencia = await _repositorioVueltas.ObtenerTelemetriaVueltaAsync(mejorVueltaGlobal.Id, token);
        float velocidadMaxima = muestrasReferencia.Count > 0
            ? (float)Math.Round(muestrasReferencia.Max(m => m.VelocidadKmh), 1)
            : perfilBase.VelocidadMaximaKmh;

        var sesionRef = mejorVueltaGlobal.Sesion ?? sesionesValidas[0];
        var evaluacionReal = _motorEvaluacion.EvaluarSesion(sesionRef, todasLasVueltas, muestrasReferencia, usuarioId);

        float puntajeGeneral = (float)Math.Round(
            (evaluacionReal.Frenada + evaluacionReal.TraccionSalida + evaluacionReal.VelocidadApice + evaluacionReal.Consistencia + evaluacionReal.RespetoLimites) / 5.0f,
            1);

        int paceCalculado = Math.Clamp(1200 + (evaluacionReal.Velocidad * 9) + (evaluacionReal.Consistencia * 8), 1200, 3000);
        string safetyCalculada = evaluacionReal.RespetoLimites >= 93 ? "S" : (evaluacionReal.RespetoLimites >= 82 ? "A" : "B");

        var recordsReales = todasLasVueltas
            .Where(v => v.EsValida && v.TiempoVueltaMs > 0 && v.Sesion != null)
            .GroupBy(v => NormalizarClaveCircuito(v.Sesion!.Circuito))
            .Select(g =>
            {
                var mejor = g.OrderBy(v => v.TiempoVueltaMs).First();
                var (nombreCircuito, pais) = FormatearCircuitoYPais(g.Key, mejor.Sesion!.Circuito);
                return new RecordCircuitoPilotoDto
                {
                    Id = g.Key,
                    Circuito = nombreCircuito,
                    Pais = pais,
                    Tiempo = mejor.TiempoVueltaTexto,
                    EsRecordSesion = true
                };
            })
            .ToList();

        string fechaReciente = sesionesValidas[0].FechaInicio.ToString("yyyy-MM-dd");
        bool tieneMonzaRapido = todasLasVueltas.Any(v =>
            v.EsValida && v.TiempoVueltaMs > 0 && v.TiempoVueltaMs < 95000 &&
            (v.Sesion?.Circuito.Contains("monza", StringComparison.OrdinalIgnoreCase) ?? false));

        var logrosReales = new List<LogroPilotoDto>
        {
            new()
            {
                Id = "bautismo-pista",
                Titulo = "Bautismo de Pista",
                Descripcion = "Primera tanda de telemetría completada y registrada en base de datos.",
                Icono = "zap",
                Desbloqueado = totalVueltas >= 1,
                FechaDesbloqueo = totalVueltas >= 1 ? fechaReciente : null
            },
            new()
            {
                Id = "freno-precision",
                Titulo = "Freno de Precisión",
                Descripcion = "Modulación de Trail-Braking superior a 85 puntos en telemetría.",
                Icono = "disc",
                Desbloqueado = evaluacionReal.TrailBraking >= 85,
                FechaDesbloqueo = evaluacionReal.TrailBraking >= 85 ? fechaReciente : null
            },
            new()
            {
                Id = "ritmo-quirurgico",
                Titulo = "Ritmo Quirúrgico",
                Descripcion = "Consistencia de ritmo superior a 88 puntos con al menos 5 vueltas válidas.",
                Icono = "crosshair",
                Desbloqueado = vueltasValidasCount >= 5 && evaluacionReal.Consistencia >= 88,
                FechaDesbloqueo = (vueltasValidasCount >= 5 && evaluacionReal.Consistencia >= 88) ? fechaReciente : null
            },
            new()
            {
                Id = "cero-incidentes",
                Titulo = "Cero Incidentes",
                Descripcion = "Más del 90% de vueltas válidas respetando los límites de pista.",
                Icono = "shield-check",
                Desbloqueado = totalVueltas >= 3 && porcentajeValidas >= 90.0f,
                FechaDesbloqueo = (totalVueltas >= 3 && porcentajeValidas >= 90.0f) ? fechaReciente : null
            },
            new()
            {
                Id = "monza-record",
                Titulo = "Templo de la Velocidad",
                Descripcion = "Tiempo de referencia válido registrado en el Autodromo Nazionale Monza.",
                Icono = "timer",
                Desbloqueado = tieneMonzaRapido || recordsReales.Any(r => r.Id == "monza"),
                FechaDesbloqueo = (tieneMonzaRapido || recordsReales.Any(r => r.Id == "monza")) ? fechaReciente : null
            },
            new()
            {
                Id = "telemetria-pro",
                Titulo = "Ingeniero en Cabina",
                Descripcion = "Más de 15 vueltas acumuladas con adquisición continua a 60 Hz.",
                Icono = "gauge",
                Desbloqueado = totalVueltas >= 15,
                FechaDesbloqueo = totalVueltas >= 15 ? fechaReciente : null
            }
        };

        return perfilBase with
        {
            VehiculoPrincipal = vehiculoPrincipal,
            PaceRating = paceCalculado,
            SafetyRating = safetyCalculada,
            PuntajeGeneral = puntajeGeneral,
            VueltasTotales = totalVueltas,
            KilometrosTotales = kmTotales,
            PorcentajeVueltasValidas = porcentajeValidas,
            VelocidadMaximaKmh = velocidadMaxima,
            UltimaEvaluacion = evaluacionReal,
            Records = recordsReales.Count > 0 ? recordsReales : perfilBase.Records,
            Logros = logrosReales
        };
    }

    private static string NormalizarClaveCircuito(string? circuito)
    {
        string c = (circuito ?? "monza").ToLowerInvariant();
        if (c.Contains("monza")) return "monza";
        if (c.Contains("spa")) return "spa";
        if (c.Contains("redbull") || c.Contains("spielberg")) return "redbullring";
        if (c.Contains("imola")) return "imola";
        return c.Trim();
    }

    private static (string Nombre, string Pais) FormatearCircuitoYPais(string clave, string original)
    {
        return clave switch
        {
            "monza" => ("Autodromo Nazionale Monza", "ITA"),
            "spa" => ("Circuit de Spa-Francorchamps", "BEL"),
            "redbullring" => ("Red Bull Ring (Spielberg)", "AUT"),
            "imola" => ("Autodromo Enzo e Dino Ferrari (Imola)", "ITA"),
            _ => (original, "INT")
        };
    }

    [HttpPost("{id:guid}/evaluacion")]
    [HasPermission("evaluacion:crear")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RespuestaApi<EvaluacionRendimientoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi<EvaluacionRendimientoDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RespuestaApi<EvaluacionRendimientoDto>>> EvaluarSesion(
        Guid id,
        [FromBody] GenerarEvaluacionRequest? peticion,
        CancellationToken tokenCancelacion = default)
    {
        if (peticion is null || peticion.SesionId == Guid.Empty)
        {
            return BadRequest(RespuestaApi<EvaluacionRendimientoDto>.ConstruirFallo(
                "El identificador de sesión ('sesionId') es obligatorio."));
        }

        var vueltas = await _repositorioVueltas.ObtenerVueltasPorSesionAsync(peticion.SesionId, tokenCancelacion);
        var mejorVuelta = vueltas.FirstOrDefault(v => v.EsMejorVuelta) ?? vueltas.FirstOrDefault();

        var muestras = mejorVuelta is not null
            ? await _repositorioVueltas.ObtenerTelemetriaVueltaAsync(mejorVuelta.Id, tokenCancelacion)
            : new System.Collections.Generic.List<MuestraTelemetria>();

        var sesionDummy = new Sesion
        {
            Id = peticion.SesionId,
            Circuito = mejorVuelta?.Sesion?.Circuito ?? "Autodromo Nazionale Monza",
            ModeloVehiculo = mejorVuelta?.Sesion?.ModeloVehiculo ?? "Ferrari SF70H",
            TotalVueltas = vueltas.Count,
            MejorTiempoMs = mejorVuelta?.TiempoVueltaMs ?? 81240
        };

        var evaluacion = _motorEvaluacion.EvaluarSesion(sesionDummy, vueltas, muestras, id);

        // Persistir en PostgreSQL
        await _repositorioPerfiles.GuardarEvaluacionAsync(id, evaluacion, tokenCancelacion);

        // Actualizar rating del perfil proporcionalmente
        int paceCalculado = Math.Clamp(1200 + (evaluacion.Velocidad * 10) + (evaluacion.Consistencia * 8), 1000, 3000);
        string safetyCalculada = evaluacion.RespetoLimites >= 95 ? "S" : (evaluacion.RespetoLimites >= 85 ? "A" : "B");
        float puntajeGral = (float)Math.Round((evaluacion.Velocidad + evaluacion.Consistencia + evaluacion.Frenada + evaluacion.Control + evaluacion.RespetoLimites) / 5.0f, 1);

        float kmNuevos = (float)Math.Round(vueltas.Count * 5.793f, 2);
        await _repositorioPerfiles.ActualizarRatingAsync(id, paceCalculado, safetyCalculada, puntajeGral, vueltas.Count, kmNuevos, tokenCancelacion);

        return Ok(RespuestaApi<EvaluacionRendimientoDto>.ConstruirExito(evaluacion));
    }

    [HttpPut("{id:guid}/rating")]
    [HasPermission("perfil:actualizar")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RespuestaApi<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi<string>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RespuestaApi<string>>> ActualizarRating(
        Guid id,
        [FromBody] ActualizarRatingRequest? peticion,
        CancellationToken tokenCancelacion = default)
    {
        if (peticion is null)
        {
            return BadRequest(RespuestaApi<string>.ConstruirFallo("Cuerpo de petición inválido."));
        }

        await _repositorioPerfiles.ActualizarRatingAsync(
            id,
            peticion.PaceRating,
            peticion.SafetyRating,
            peticion.PuntajeGeneral,
            peticion.VueltasSumar,
            peticion.KmSumar,
            tokenCancelacion);

        return Ok(RespuestaApi<string>.ConstruirExito("Rating actualizado exitosamente."));
    }
}
