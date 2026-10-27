namespace Telemetria.Api.Controladores;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Telemetria.Api.Autorizacion;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Dtos.Requests;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Dominio.Contratos.Hardware;

/// <summary>
/// Controlador REST para consultar el estado del servicio y conmutar en caliente entre
/// el lector físico de Assetto Corsa y el generador determinístico mock.
/// </summary>
[ApiController]
[Route("api/estado")]
[Produces("application/json")]
public sealed class EstadoController : ControllerBase
{
    private readonly ILectorTelemetria _lectorTelemetria;
    private readonly IControlModoTelemetria _controlModoTelemetria;
    private readonly ILogger<EstadoController> _logger;

    public EstadoController(
        ILectorTelemetria lectorTelemetria,
        IControlModoTelemetria controlModoTelemetria,
        ILogger<EstadoController> logger)
    {
        _lectorTelemetria = lectorTelemetria;
        _controlModoTelemetria = controlModoTelemetria;
        _logger = logger;
    }

    [HttpGet]
    [HasPermission("estado:leer")]
    [ProducesResponseType(typeof(RespuestaApi<EstadoServicioDto>), StatusCodes.Status200OK)]
    public ActionResult<RespuestaApi<EstadoServicioDto>> ObtenerEstado()
    {
        bool estaConectado = _lectorTelemetria.EstaConectado();
        var metadatos = estaConectado ? _lectorTelemetria.ObtenerMetadatosEstaticos() : null;

        var estadoDto = new EstadoServicioDto
        {
            EstaConectado = estaConectado,
            ModoMock = _controlModoTelemetria.EstaEnModoMock,
            Circuito = metadatos?.NombrePista ?? "Desconocido / Esperando simulador",
            ModeloVehiculo = metadatos?.NombreAuto ?? "Desconocido",
            LongitudPistaMetros = metadatos?.LongitudPistaMetros ?? 0f,
            AutosActivos = estaConectado ? _lectorTelemetria.ObtenerUltimoCuadroConocido().CantidadAutosActivos : 0
        };

        return Ok(RespuestaApi<EstadoServicioDto>.ConstruirExito(estadoDto));
    }

    [HttpPost("/api/mock/alternar")]
    [HasPermission("simulador:alternar_mock")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RespuestaApi<EstadoMockDto>), StatusCodes.Status200OK)]
    public ActionResult<RespuestaApi<EstadoMockDto>> AlternarModoMock([FromBody] AlternarModoMockRequest? peticion)
    {
        bool nuevoModo = peticion?.UsarMock ?? !_controlModoTelemetria.EstaEnModoMock;
        _controlModoTelemetria.ConmutarModoMock(nuevoModo);

        _logger.LogInformation("Conmutación en caliente ejecutada vía API: ModoMock={NuevoModo}", nuevoModo);

        var resultado = new EstadoMockDto
        {
            ModoMockActivo = _controlModoTelemetria.EstaEnModoMock,
            Mensaje = _controlModoTelemetria.EstaEnModoMock
                ? "Generador sintético determinístico activado (Monza - ks_ferrari_sf70h)."
                : "Lector de memoria compartida física Assetto Corsa activado."
        };

        return Ok(RespuestaApi<EstadoMockDto>.ConstruirExito(resultado));
    }
}
