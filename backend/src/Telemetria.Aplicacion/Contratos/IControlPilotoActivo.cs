namespace Telemetria.Aplicacion.Contratos;

using System;
using System.Threading;
using System.Threading.Tasks;
using Telemetria.Aplicacion.Dtos.Responses;

/// <summary>
/// Contrato para gestionar y declarar el piloto actualmente sentado en la cabina del simulador (FAPPE / Torneo).
/// Al conmutar de piloto activo, las sesiones previas se finalizan y se crea una nueva asociada al nuevo usuario.
/// </summary>
public interface IControlPilotoActivo
{
    Guid? PilotoActivoId { get; }
    string PilotoActivoNombre { get; }
    string PilotoActivoAlias { get; }
    Task FijarPilotoActivoAsync(Guid usuarioId, string nombre, string alias, CancellationToken token = default);
    SesionFinalizadaTorneoDto? ObtenerSesionFinalizadaPendiente() => null;
    void ConfirmarSesionFinalizadaPendiente(Guid sesionId) { }
    Task<SesionFinalizadaTorneoDto?> ForzarCierreSesionActivaAsync(CancellationToken token = default) =>
        Task.FromResult<SesionFinalizadaTorneoDto?>(null);
}
