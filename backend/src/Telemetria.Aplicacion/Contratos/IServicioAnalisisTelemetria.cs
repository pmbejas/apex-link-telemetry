namespace Telemetria.Aplicacion.Contratos;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Dtos.Responses;

/// <summary>
/// Contrato para las operaciones de consulta analítica, paginación e interpolación métrica de telemetría post-sesión.
/// </summary>
public interface IServicioAnalisisTelemetria
{
    Task<PaginacionRespuesta<SesionResumenDto>> ObtenerSesionesPaginadasAsync(
        int numeroPagina,
        int tamanioPagina,
        Guid? usuarioId = null,
        CancellationToken tokenCancelacion = default);

    Task<bool> EliminarSesionAsync(
        Guid sesionId,
        CancellationToken tokenCancelacion = default);

    Task<List<VueltaDto>> ObtenerVueltasPorSesionAsync(
        Guid sesionId,
        CancellationToken tokenCancelacion = default);

    Task<List<MuestraTelemetriaDto>> ObtenerTelemetriaDeVueltaAsync(
        Guid vueltaId,
        CancellationToken tokenCancelacion = default);

    Task<ComparativaVueltasDto?> CompararVueltasPorDistanciaAsync(
        Guid vueltaBaseId,
        Guid vueltaComparadaId,
        CancellationToken tokenCancelacion = default);

    Task<List<LeaderboardItemDto>> ObtenerLeaderboardAsync(
        string? circuito,
        int limite = 10,
        CancellationToken tokenCancelacion = default);

    Task<bool> AtribuirSesionParticipanteTorneoAsync(
        Guid sesionId,
        bool esParticipanteTorneo,
        string? nombreParticipante,
        CancellationToken tokenCancelacion = default) => Task.FromResult(false);
}
