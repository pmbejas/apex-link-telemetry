namespace Telemetria.Api.Controladores;

using System;
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

        return Ok(RespuestaApi<PerfilPilotoDetalleDto>.ConstruirExito(perfil));
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
