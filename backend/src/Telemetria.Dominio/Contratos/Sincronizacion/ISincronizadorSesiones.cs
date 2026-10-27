namespace Telemetria.Dominio.Contratos.Sincronizacion;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Contrato para el componente de sincronización asíncrona (Store-and-Forward)
/// desde la base de datos local SQLite hacia el servidor central PostgreSQL.
/// </summary>
public interface ISincronizadorSesiones
{
    /// <summary>
    /// Sincroniza de forma transaccional e idempotente las sesiones reales finalizadas
    /// que se encuentren pendientes de transmisión.
    /// </summary>
    /// <param name="tokenCancelacion">Token de cancelación de la operación.</param>
    Task SincronizarSesionesPendientesAsync(CancellationToken tokenCancelacion = default);
}
