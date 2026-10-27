namespace Telemetria.Dominio.Contratos.Repositorios;

using Telemetria.Dominio.Modelos;

/// <summary>
/// Contrato para las operaciones de persistencia transaccional y consulta analítica de vueltas y muestras en SQLite.
/// </summary>
public interface IRepositorioVueltasLocal
{
    Task GuardarVueltaConTelemetriaAsync(
        Vuelta vuelta,
        List<MuestraTelemetria> muestras,
        CancellationToken tokenCancelacion = default);

    Task<List<Vuelta>> ObtenerVueltasPorSesionAsync(Guid sesionId, CancellationToken tokenCancelacion = default);

    Task<List<MuestraTelemetria>> ObtenerTelemetriaVueltaAsync(Guid vueltaId, CancellationToken tokenCancelacion = default);

    Task<Vuelta?> ObtenerMejorVueltaAsync(string circuito, CancellationToken tokenCancelacion = default);
}
