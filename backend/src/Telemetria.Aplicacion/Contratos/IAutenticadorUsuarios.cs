namespace Telemetria.Aplicacion.Contratos;

using System.Threading;
using System.Threading.Tasks;
using Telemetria.Aplicacion.Dtos.Requests;
using Telemetria.Aplicacion.Dtos.Responses;

public interface IAutenticadorUsuarios
{
    Task<LoginRespuestaDto?> AutenticarAsync(
        LoginRequest solicitud,
        CancellationToken token = default);

    Task<(LoginRespuestaDto? Resultado, string? Error)> RegistrarAsync(
        RegistroUsuarioRequest solicitud,
        CancellationToken token = default);

    Task<(bool Exitoso, string Mensaje)> SolicitarRecuperacionPasswordAsync(
        string email,
        CancellationToken token = default);

    Task<(bool Exitoso, string Mensaje)> ConfirmarRecuperacionPasswordAsync(
        string email,
        string codigo,
        string nuevaPassword,
        CancellationToken token = default);
}
