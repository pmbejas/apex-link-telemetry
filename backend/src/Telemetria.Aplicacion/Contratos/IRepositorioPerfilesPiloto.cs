namespace Telemetria.Aplicacion.Contratos;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telemetria.Aplicacion.Dtos.Responses;

/// <summary>
/// Contrato de persistencia para el perfil del piloto, evaluaciones y rating.
/// </summary>
public interface IRepositorioPerfilesPiloto
{
    Task<PerfilPilotoDetalleDto?> ObtenerPerfilCompletoAsync(
        Guid usuarioId,
        CancellationToken token = default);

    Task<Dictionary<Guid, (string Nombre, string Alias, DateTimeOffset? FechaRegistro)>> ObtenerMapaUsuariosRegistradosAsync(
        CancellationToken token = default) => Task.FromResult(new Dictionary<Guid, (string Nombre, string Alias, DateTimeOffset? FechaRegistro)>());

    Task GuardarEvaluacionAsync(
        Guid usuarioId,
        EvaluacionRendimientoDto evaluacion,
        CancellationToken token = default);

    Task ActualizarRatingAsync(
        Guid usuarioId,
        int paceRating,
        string safetyRating,
        float puntajeGeneral,
        int vueltasSumar = 0,
        float kmSumar = 0f,
        CancellationToken token = default);
}
