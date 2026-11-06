namespace Telemetria.Api.Controladores;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Dtos.Requests;
using Telemetria.Aplicacion.Dtos.Responses;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAutenticadorUsuarios _autenticador;

    public AuthController(IAutenticadorUsuarios autenticador)
    {
        _autenticador = autenticador;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespuestaApi<LoginRespuestaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest solicitud,
        CancellationToken tokenCancelacion)
    {
        if (solicitud == null ||
            string.IsNullOrWhiteSpace(solicitud.Email) ||
            string.IsNullOrWhiteSpace(solicitud.Password))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Datos de acceso incompletos",
                Detail = "Debe proporcionar correo electrónico y contraseña válidos."
            });
        }

        var resultado = await _autenticador.AutenticarAsync(solicitud, tokenCancelacion);
        if (resultado == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Credenciales inválidas",
                Detail = "El correo electrónico o la contraseña ingresados no corresponden a un piloto activo."
            });
        }

        return Ok(RespuestaApi<LoginRespuestaDto>.ConstruirExito(
            resultado,
            "Autenticación exitosa en servidor de boxes."));
    }

    [HttpPost("registro")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespuestaApi<LoginRespuestaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registro(
        [FromBody] RegistroUsuarioRequest solicitud,
        CancellationToken tokenCancelacion)
    {
        if (solicitud == null ||
            string.IsNullOrWhiteSpace(solicitud.Nombre) ||
            string.IsNullOrWhiteSpace(solicitud.Alias) ||
            string.IsNullOrWhiteSpace(solicitud.Email) ||
            string.IsNullOrWhiteSpace(solicitud.Password))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Datos de registro incompletos",
                Detail = "Debés completar nombre completo, alias, correo electrónico y contraseña."
            });
        }

        var (resultado, error) = await _autenticador.RegistrarAsync(solicitud, tokenCancelacion);
        if (resultado == null)
        {
            bool esConflicto = error != null && (error.Contains("existe") || error.Contains("en uso"));
            int statusCode = esConflicto ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;

            return StatusCode(statusCode, new ProblemDetails
            {
                Status = statusCode,
                Title = esConflicto ? "Piloto ya registrado" : "No se pudo completar el registro",
                Detail = error ?? "Verificá los datos ingresados e intentá nuevamente."
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            RespuestaApi<LoginRespuestaDto>.ConstruirExito(
                resultado,
                "Piloto registrado y autenticado exitosamente en boxes."));
    }

    [HttpPost("recuperar-solicitud")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespuestaApi<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SolicitarRecupero(
        [FromBody] SolicitarRecuperoRequest solicitud,
        CancellationToken tokenCancelacion)
    {
        if (solicitud == null || string.IsNullOrWhiteSpace(solicitud.Email))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Correo requerido",
                Detail = "Ingresá el correo electrónico asociado a tu licencia de piloto."
            });
        }

        var (exitoso, mensaje) = await _autenticador.SolicitarRecuperacionPasswordAsync(
            solicitud.Email,
            tokenCancelacion);

        if (!exitoso)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "No se pudo procesar la recuperación",
                Detail = mensaje
            });
        }

        return Ok(RespuestaApi<bool>.ConstruirExito(true, mensaje));
    }

    [HttpPost("recuperar-confirmar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespuestaApi<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmarRecupero(
        [FromBody] ConfirmarRecuperoRequest solicitud,
        CancellationToken tokenCancelacion)
    {
        if (solicitud == null ||
            string.IsNullOrWhiteSpace(solicitud.Email) ||
            string.IsNullOrWhiteSpace(solicitud.Codigo) ||
            string.IsNullOrWhiteSpace(solicitud.NuevaPassword))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Datos incompletos",
                Detail = "Debés proporcionar correo, código de 6 dígitos y la nueva contraseña."
            });
        }

        var (exitoso, mensaje) = await _autenticador.ConfirmarRecuperacionPasswordAsync(
            solicitud.Email,
            solicitud.Codigo,
            solicitud.NuevaPassword,
            tokenCancelacion);

        if (!exitoso)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Error de verificación",
                Detail = mensaje
            });
        }

        return Ok(RespuestaApi<bool>.ConstruirExito(true, mensaje));
    }
}
