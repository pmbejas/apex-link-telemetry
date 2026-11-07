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
        Guid? usuarioId = null,
        CancellationToken tokenCancelacion = default)
    {
        try
        {
            var nuevaSesion = new Sesion
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
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
        Guid? usuarioId = null,
        CancellationToken tokenCancelacion = default)
    {
        try
        {
            int paginaSanitizada = Math.Max(1, pagina);
            int tamanioSanitizado = Math.Clamp(tamanioPagina, 1, TAMANIO_MAXIMO_PAGINA);
            int registrosASaltar = (paginaSanitizada - 1) * tamanioSanitizado;

            var query = _contexto.Sesiones.AsNoTracking();
            if (usuarioId.HasValue && usuarioId.Value != Guid.Empty)
            {
                query = query.Where(sesion => sesion.UsuarioId == usuarioId.Value);
            }

            return await query
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

    public async Task<int> ContarTotalSesionesAsync(
        Guid? usuarioId = null,
        CancellationToken tokenCancelacion = default)
    {
        try
        {
            var query = _contexto.Sesiones.AsNoTracking();
            if (usuarioId.HasValue && usuarioId.Value != Guid.Empty)
            {
                query = query.Where(sesion => sesion.UsuarioId == usuarioId.Value);
            }

            return await query.CountAsync(tokenCancelacion);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al contabilizar el total de sesiones en SQLite.");
            throw;
        }
    }

    public async Task<bool> EliminarSesionAsync(Guid sesionId, CancellationToken tokenCancelacion = default)
    {
        try
        {
            var sesion = await _contexto.Sesiones
                .Include(s => s.Vueltas)
                .FirstOrDefaultAsync(s => s.Id == sesionId, tokenCancelacion);

            if (sesion == null)
            {
                return false;
            }

            // Eliminar muestras asociadas si no se propagaron por cascada en SQLite
            var vueltasIds = sesion.Vueltas.Select(v => v.Id).ToList();
            if (vueltasIds.Count > 0)
            {
                var muestras = await _contexto.MuestrasTelemetria
                    .Where(m => vueltasIds.Contains(m.VueltaId))
                    .ToListAsync(tokenCancelacion);

                if (muestras.Count > 0)
                {
                    _contexto.MuestrasTelemetria.RemoveRange(muestras);
                }

                _contexto.Vueltas.RemoveRange(sesion.Vueltas);
            }

            _contexto.Sesiones.Remove(sesion);
            await _contexto.SaveChangesAsync(tokenCancelacion);

            _logger.LogInformation("Sesión {SesionId} eliminada exitosamente de SQLite.", sesionId);
            return true;
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al eliminar la sesión {SesionId} en SQLite.", sesionId);
            throw;
        }
    }

    public async Task AsignarUsuarioASesionesSinPilotoAsync(Guid usuarioId, CancellationToken tokenCancelacion = default)
    {
        if (usuarioId == Guid.Empty)
        {
            return;
        }

        try
        {
            // Adoptar sesiones recientes iniciadas sin piloto activo:
            // - Sesiones en curso en el simulador activo (FechaFin == null)
            // - Sesiones recién finalizadas con vueltas completadas (TotalVueltas > 0) en las últimas 2 horas
            // Jamás adoptar sesiones históricas antiguas ni sesiones vacías cerradas.
            var umbralReciente = DateTimeOffset.UtcNow.AddHours(-2);
            var candidatas = await _contexto.Sesiones
                .Where(s => s.UsuarioId == null && !s.EsSimulacionMock)
                .ToListAsync(tokenCancelacion);

            var sesionesHuerfanas = candidatas
                .Where(s => s.FechaInicio >= umbralReciente && (s.FechaFin == null || s.TotalVueltas > 0))
                .ToList();

            if (sesionesHuerfanas.Count == 0)
            {
                return;
            }

            foreach (var sesion in sesionesHuerfanas)
            {
                sesion.UsuarioId = usuarioId;
            }

            await _contexto.SaveChangesAsync(tokenCancelacion);
            _logger.LogInformation("Adoptadas {Cantidad} sesiones activas en pista sin piloto previo para el usuario {UsuarioId}.", sesionesHuerfanas.Count, usuarioId);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al adoptar sesiones sin piloto para el usuario {UsuarioId}.", usuarioId);
            throw;
        }
    }

    public async Task CerrarSesionesAbiertasAsync(CancellationToken tokenCancelacion = default)
    {
        try
        {
            var sesionesAbiertas = await _contexto.Sesiones
                .Include(entidad => entidad.Vueltas)
                .Where(entidad => entidad.FechaFin == null)
                .ToListAsync(tokenCancelacion);

            if (sesionesAbiertas.Count == 0)
            {
                return;
            }

            var ahora = DateTimeOffset.UtcNow;
            foreach (var sesion in sesionesAbiertas)
            {
                sesion.FechaFin = ahora;
                sesion.TotalVueltas = sesion.Vueltas.Count;

                var vueltasValidas = sesion.Vueltas
                    .Where(vuelta => vuelta.EsValida && vuelta.TiempoVueltaMs > 0)
                    .ToList();

                sesion.MejorTiempoMs = vueltasValidas.Count > 0
                    ? vueltasValidas.Min(vuelta => vuelta.TiempoVueltaMs)
                    : 0;
            }

            await _contexto.SaveChangesAsync(tokenCancelacion);
            _logger.LogInformation("Consolidadas y cerradas {Cantidad} sesiones que habían quedado abiertas en SQLite.", sesionesAbiertas.Count);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al consolidar sesiones abiertas en SQLite.");
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

    public async Task<Sesion?> ObtenerSesionConVueltasPorIdAsync(
        Guid sesionId,
        CancellationToken tokenCancelacion = default)
    {
        try
        {
            return await _contexto.Sesiones
                .AsNoTracking()
                .Include(sesion => sesion.Vueltas)
                .FirstOrDefaultAsync(sesion => sesion.Id == sesionId, tokenCancelacion);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al consultar la sesión {SesionId} con sus vueltas en SQLite.", sesionId);
            throw;
        }
    }

    public async Task<Sesion?> ReasignarUsuarioSesionAsync(
        Guid sesionId,
        Guid nuevoUsuarioId,
        CancellationToken tokenCancelacion = default)
    {
        try
        {
            var sesion = await _contexto.Sesiones
                .Include(entidad => entidad.Vueltas)
                .FirstOrDefaultAsync(entidad => entidad.Id == sesionId, tokenCancelacion);

            if (sesion == null)
            {
                return null;
            }

            sesion.UsuarioId = nuevoUsuarioId;
            sesion.EsSimulacionMock = false;
            await _contexto.SaveChangesAsync(tokenCancelacion);

            _logger.LogInformation("Sesión {SesionId} reasignada al participante del Torneo FAPPE {UsuarioId}.", sesionId, nuevoUsuarioId);
            return sesion;
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al reasignar usuario en la sesión {SesionId} en SQLite.", sesionId);
            throw;
        }
    }
}
