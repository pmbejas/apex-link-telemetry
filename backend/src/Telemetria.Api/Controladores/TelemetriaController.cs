namespace Telemetria.Api.Controladores;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telemetria.Api.Autorizacion;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Dtos.Requests;
using Telemetria.Aplicacion.Dtos.Responses;

/// <summary>
/// Controlador REST para las consultas analíticas de sesiones, vueltas y telemetría espacial post-sesión.
/// Protegido con permisos granulares RBAC.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public sealed class TelemetriaController : ControllerBase
{
    private readonly IServicioAnalisisTelemetria _servicioAnalisis;
    private readonly IControlPilotoActivo _controlPilotoActivo;

    public TelemetriaController(
        IServicioAnalisisTelemetria servicioAnalisis,
        IControlPilotoActivo controlPilotoActivo)
    {
        _servicioAnalisis = servicioAnalisis;
        _controlPilotoActivo = controlPilotoActivo;
    }

    [HttpGet("sesiones")]
    [HasPermission("sesion:listar")]
    [ProducesResponseType(typeof(RespuestaApi<PaginacionRespuesta<SesionResumenDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<RespuestaApi<PaginacionRespuesta<SesionResumenDto>>>> ObtenerSesiones(
        [FromQuery] int numeroPagina = 1,
        [FromQuery] int tamanioPagina = 10,
        [FromQuery] Guid? usuarioId = null,
        CancellationToken tokenCancelacion = default)
    {
        var paginacion = await _servicioAnalisis.ObtenerSesionesPaginadasAsync(
            numeroPagina, tamanioPagina, usuarioId, tokenCancelacion);

        return Ok(RespuestaApi<PaginacionRespuesta<SesionResumenDto>>.ConstruirExito(paginacion));
    }

    [HttpDelete("sesiones/{id:guid}")]
    [HasPermission("sesion:listar")]
    [ProducesResponseType(typeof(RespuestaApi<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RespuestaApi<bool>>> EliminarSesion(
        Guid id,
        CancellationToken tokenCancelacion = default)
    {
        bool eliminada = await _servicioAnalisis.EliminarSesionAsync(id, tokenCancelacion);
        if (!eliminada)
        {
            return NotFound(RespuestaApi<bool>.ConstruirFallo(
                "La tanda especificada no existe o no pudo ser eliminada."));
        }

        return Ok(RespuestaApi<bool>.ConstruirExito(true));
    }

    [HttpGet("sesiones/{id:guid}/vueltas")]
    [HasPermission("vuelta:listar")]
    [ProducesResponseType(typeof(RespuestaApi<List<VueltaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi<List<VueltaDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RespuestaApi<List<VueltaDto>>>> ObtenerVueltasPorSesion(
        Guid id,
        CancellationToken tokenCancelacion = default)
    {
        var vueltasDto = await _servicioAnalisis.ObtenerVueltasPorSesionAsync(id, tokenCancelacion);
        if (vueltasDto.Count == 0)
        {
            return NotFound(RespuestaApi<List<VueltaDto>>.ConstruirFallo(
                "No se encontraron vueltas para la sesión especificada."));
        }

        return Ok(RespuestaApi<List<VueltaDto>>.ConstruirExito(vueltasDto));
    }

    [HttpGet("vueltas/{id:guid}/telemetria")]
    [HasPermission("telemetria:leer")]
    [ProducesResponseType(typeof(RespuestaApi<List<MuestraTelemetriaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi<List<MuestraTelemetriaDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RespuestaApi<List<MuestraTelemetriaDto>>>> ObtenerTelemetriaDeVuelta(
        Guid id,
        CancellationToken tokenCancelacion = default)
    {
        var muestrasDto = await _servicioAnalisis.ObtenerTelemetriaDeVueltaAsync(id, tokenCancelacion);
        if (muestrasDto.Count == 0)
        {
            return NotFound(RespuestaApi<List<MuestraTelemetriaDto>>.ConstruirFallo(
                "No se hallaron registros de telemetría para la vuelta indicada."));
        }

        return Ok(RespuestaApi<List<MuestraTelemetriaDto>>.ConstruirExito(muestrasDto));
    }

    [HttpGet("vueltas/comparativa")]
    [HasPermission("telemetria:comparar")]
    [ProducesResponseType(typeof(RespuestaApi<ComparativaVueltasDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi<ComparativaVueltasDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaApi<ComparativaVueltasDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RespuestaApi<ComparativaVueltasDto>>> CompararVueltas(
        [FromQuery] Guid vueltaBaseId,
        [FromQuery] Guid vueltaComparadaId,
        CancellationToken tokenCancelacion = default)
    {
        if (vueltaBaseId == Guid.Empty || vueltaComparadaId == Guid.Empty)
        {
            return BadRequest(RespuestaApi<ComparativaVueltasDto>.ConstruirFallo(
                "Los identificadores de vuelta base y vuelta comparada son obligatorios."));
        }

        var resultadoComparativa = await _servicioAnalisis.CompararVueltasPorDistanciaAsync(
            vueltaBaseId, vueltaComparadaId, tokenCancelacion);

        if (resultadoComparativa is null)
        {
            return NotFound(RespuestaApi<ComparativaVueltasDto>.ConstruirFallo(
                "Una o ambas vueltas especificadas no contienen muestras de telemetría."));
        }

        return Ok(RespuestaApi<ComparativaVueltasDto>.ConstruirExito(resultadoComparativa));
    }

    [HttpGet("leaderboard")]
    [HasPermission("sesion:listar")]
    [ProducesResponseType(typeof(RespuestaApi<List<LeaderboardItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<RespuestaApi<List<LeaderboardItemDto>>>> ObtenerLeaderboard(
        [FromQuery] string? circuito = "monza",
        [FromQuery] int limite = 10,
        CancellationToken tokenCancelacion = default)
    {
        var ranking = await _servicioAnalisis.ObtenerLeaderboardAsync(circuito, limite, tokenCancelacion);
        return Ok(RespuestaApi<List<LeaderboardItemDto>>.ConstruirExito(ranking));
    }

    [HttpGet("telemetria/piloto-activo")]
    [HasPermission("sesion:listar")]
    [ProducesResponseType(typeof(RespuestaApi<object>), StatusCodes.Status200OK)]
    public ActionResult<RespuestaApi<object>> ObtenerPilotoActivo()
    {
        var datos = new
        {
            usuarioId = _controlPilotoActivo.PilotoActivoId,
            nombre = _controlPilotoActivo.PilotoActivoNombre,
            alias = _controlPilotoActivo.PilotoActivoAlias
        };
        return Ok(RespuestaApi<object>.ConstruirExito(datos));
    }

    [HttpPost("telemetria/piloto-activo")]
    [HasPermission("sesion:listar")]
    [ProducesResponseType(typeof(RespuestaApi<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<RespuestaApi<bool>>> FijarPilotoActivo(
        [FromBody] DeclararPilotoActivoRequest request,
        CancellationToken tokenCancelacion = default)
    {
        await _controlPilotoActivo.FijarPilotoActivoAsync(
            request.UsuarioId,
            request.Nombre,
            request.Alias,
            tokenCancelacion);

        return Ok(RespuestaApi<bool>.ConstruirExito(true));
    }
}
