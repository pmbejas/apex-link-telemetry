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

    public TelemetriaController(IServicioAnalisisTelemetria servicioAnalisis)
    {
        _servicioAnalisis = servicioAnalisis;
    }

    [HttpGet("sesiones")]
    [HasPermission("sesion:listar")]
    [ProducesResponseType(typeof(RespuestaApi<PaginacionRespuesta<SesionResumenDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<RespuestaApi<PaginacionRespuesta<SesionResumenDto>>>> ObtenerSesiones(
        [FromQuery] int numeroPagina = 1,
        [FromQuery] int tamanioPagina = 10,
        CancellationToken tokenCancelacion = default)
    {
        var paginacion = await _servicioAnalisis.ObtenerSesionesPaginadasAsync(
            numeroPagina, tamanioPagina, tokenCancelacion);

        return Ok(RespuestaApi<PaginacionRespuesta<SesionResumenDto>>.ConstruirExito(paginacion));
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
}
