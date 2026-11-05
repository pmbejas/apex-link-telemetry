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
}
