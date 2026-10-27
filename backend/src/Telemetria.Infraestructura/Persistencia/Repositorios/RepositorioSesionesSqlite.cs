namespace Telemetria.Infraestructura.Persistencia.Repositorios;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telemetria.Dominio.Contratos.Repositorios;
using Telemetria.Dominio.Modelos;
using Telemetria.Infraestructura.Persistencia.Contexto;

/// <summary>
/// Repositorio local de sesiones implementado con Entity Framework Core sobre SQLite.
/// </summary>
public sealed class RepositorioSesionesSqlite : IRepositorioSesionesLocal
{
    private const int TAMANIO_MAXIMO_PAGINA = 100;
    private readonly TelemetriaLocalDbContext _contexto;
    private readonly ILogger<RepositorioSesionesSqlite> _logger;

    public RepositorioSesionesSqlite(
        TelemetriaLocalDbContext contexto,
        ILogger<RepositorioSesionesSqlite> logger)
    {
        _contexto = contexto;
        _logger = logger;
    }

    public async Task<Guid> IniciarSesionAsync(
        string circuito,
        string configuracionCircuito,
        string modeloVehiculo,
        int tipoSesion,
        bool esSimulacionMock,
        CancellationToken tokenCancelacion = default)
    {
        try
        {
            var nuevaSesion = new Sesion
            {
                Id = Guid.NewGuid(),
                Circuito = circuito,
                ConfiguracionCircuito = configuracionCircuito,
                ModeloVehiculo = modeloVehiculo,
                TipoSesion = tipoSesion,
                EsSimulacionMock = esSimulacionMock,
                FechaInicio = DateTimeOffset.UtcNow,
                TotalVueltas = 0,
                MejorTiempoMs = 0,
                SincronizadoConServidorCentral = false
            };

            await _contexto.Sesiones.AddAsync(nuevaSesion, tokenCancelacion);
            await _contexto.SaveChangesAsync(tokenCancelacion);
            return nuevaSesion.Id;
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al iniciar la sesión local en SQLite para el circuito {Circuito}.", circuito);
            throw;
        }
    }

    public async Task FinalizarSesionAsync(Guid sesionId, CancellationToken tokenCancelacion = default)
    {
        try
        {
            var sesion = await _contexto.Sesiones
                .Include(entidad => entidad.Vueltas)
                .FirstOrDefaultAsync(entidad => entidad.Id == sesionId, tokenCancelacion);

            if (sesion == null)
            {
                return;
            }

            sesion.FechaFin = DateTimeOffset.UtcNow;
            sesion.TotalVueltas = sesion.Vueltas.Count;

            var vueltasValidas = sesion.Vueltas
                .Where(vuelta => vuelta.EsValida && vuelta.TiempoVueltaMs > 0)
                .ToList();

            sesion.MejorTiempoMs = vueltasValidas.Count > 0
                ? vueltasValidas.Min(vuelta => vuelta.TiempoVueltaMs)
                : 0;

            await _contexto.SaveChangesAsync(tokenCancelacion);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al finalizar la sesión {SesionId} en SQLite.", sesionId);
            throw;
        }
    }

    public async Task<List<Sesion>> ObtenerHistorialSesionesAsync(
        int pagina,
        int tamanioPagina,
        CancellationToken tokenCancelacion = default)
    {
        try
        {
            int paginaSanitizada = Math.Max(1, pagina);
            int tamanioSanitizado = Math.Clamp(tamanioPagina, 1, TAMANIO_MAXIMO_PAGINA);
            int registrosASaltar = (paginaSanitizada - 1) * tamanioSanitizado;

            return await _contexto.Sesiones
                .AsNoTracking()
                .OrderByDescending(sesion => sesion.FechaInicio)
                .Skip(registrosASaltar)
                .Take(tamanioSanitizado)
                .ToListAsync(tokenCancelacion);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al consultar el historial paginado de sesiones en SQLite (Página {Pagina}).", pagina);
            throw;
        }
    }

    public async Task<int> ContarTotalSesionesAsync(CancellationToken tokenCancelacion = default)
    {
        try
        {
            return await _contexto.Sesiones.CountAsync(tokenCancelacion);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al contabilizar el total de sesiones en SQLite.");
            throw;
        }
    }

    public async Task<List<Sesion>> ObtenerSesionesPendientesSincronizacionAsync(CancellationToken tokenCancelacion = default)
    {
        try
        {
            return await _contexto.Sesiones
                .AsNoTracking()
                .Include(sesion => sesion.Vueltas)
                .Where(sesion => !sesion.SincronizadoConServidorCentral && !sesion.EsSimulacionMock && sesion.FechaFin != null)
                .OrderBy(sesion => sesion.FechaInicio)
                .ToListAsync(tokenCancelacion);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al obtener las sesiones pendientes de sincronización en SQLite.");
            throw;
        }
    }

    public async Task MarcarComoSincronizadaAsync(Guid sesionId, CancellationToken tokenCancelacion = default)
    {
        try
        {
            var sesion = await _contexto.Sesiones.FirstOrDefaultAsync(entidad => entidad.Id == sesionId, tokenCancelacion);
            if (sesion == null)
            {
                return;
            }

            sesion.SincronizadoConServidorCentral = true;
            await _contexto.SaveChangesAsync(tokenCancelacion);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al marcar como sincronizada la sesión {SesionId} en SQLite.", sesionId);
            throw;
        }
    }
}
