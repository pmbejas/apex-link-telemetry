namespace Telemetria.Dominio.Contratos.Repositorios;

using Telemetria.Dominio.Modelos;

/// <summary>
/// Contrato para las operaciones de persistencia y consulta local de sesiones en SQLite.
/// </summary>
public interface IRepositorioSesionesLocal
{
    Task<Guid> IniciarSesionAsync(
        string circuito,
        string configuracionCircuito,
        string modeloVehiculo,
        int tipoSesion,
        bool esSimulacionMock,
        CancellationToken tokenCancelacion = default);

    Task FinalizarSesionAsync(Guid sesionId, CancellationToken tokenCancelacion = default);

    Task<List<Sesion>> ObtenerHistorialSesionesAsync(int pagina, int tamanioPagina, CancellationToken tokenCancelacion = default);

    Task<int> ContarTotalSesionesAsync(CancellationToken tokenCancelacion = default);

    Task<List<Sesion>> ObtenerSesionesPendientesSincronizacionAsync(CancellationToken tokenCancelacion = default);

    Task MarcarComoSincronizadaAsync(Guid sesionId, CancellationToken tokenCancelacion = default);
}
