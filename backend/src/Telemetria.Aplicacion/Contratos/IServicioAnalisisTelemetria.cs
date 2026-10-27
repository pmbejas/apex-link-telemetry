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
}
