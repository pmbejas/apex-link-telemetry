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
        Guid? usuarioId = null,
        CancellationToken tokenCancelacion = default);

    Task FinalizarSesionAsync(Guid sesionId, CancellationToken tokenCancelacion = default);

    Task<List<Sesion>> ObtenerHistorialSesionesAsync(
        int pagina,
        int tamanioPagina,
        Guid? usuarioId = null,
        CancellationToken tokenCancelacion = default);

    Task<int> ContarTotalSesionesAsync(
        Guid? usuarioId = null,
        CancellationToken tokenCancelacion = default);

    Task<bool> EliminarSesionAsync(Guid sesionId, CancellationToken tokenCancelacion = default);

    Task AsignarUsuarioASesionesSinPilotoAsync(Guid usuarioId, CancellationToken tokenCancelacion = default);

    Task CerrarSesionesAbiertasAsync(CancellationToken tokenCancelacion = default);

    Task<List<Sesion>> ObtenerSesionesPendientesSincronizacionAsync(CancellationToken tokenCancelacion = default);

    Task MarcarComoSincronizadaAsync(Guid sesionId, CancellationToken tokenCancelacion = default);

    Task<Sesion?> ObtenerSesionConVueltasPorIdAsync(Guid sesionId, CancellationToken tokenCancelacion = default) =>
        Task.FromResult<Sesion?>(null);

    Task<Sesion?> ReasignarUsuarioSesionAsync(Guid sesionId, Guid nuevoUsuarioId, CancellationToken tokenCancelacion = default) =>
        Task.FromResult<Sesion?>(null);
}
