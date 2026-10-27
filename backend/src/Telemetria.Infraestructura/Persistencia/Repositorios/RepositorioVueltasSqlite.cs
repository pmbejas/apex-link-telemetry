namespace Telemetria.Infraestructura.Persistencia.Repositorios;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telemetria.Dominio.Contratos.Repositorios;
using Telemetria.Dominio.Modelos;
using Telemetria.Infraestructura.Persistencia.Contexto;

/// <summary>
/// Repositorio local de vueltas y muestras de telemetría implementado con Entity Framework Core sobre SQLite.
/// Gestiona la inserción masiva en transacciones atómicas y el recálculo consistente de récords.
/// </summary>
public sealed class RepositorioVueltasSqlite : IRepositorioVueltasLocal
{
    private readonly TelemetriaLocalDbContext _contexto;
    private readonly ILogger<RepositorioVueltasSqlite> _logger;

    public RepositorioVueltasSqlite(
        TelemetriaLocalDbContext contexto,
        ILogger<RepositorioVueltasSqlite> logger)
    {
        _contexto = contexto;
        _logger = logger;
    }

    public async Task GuardarVueltaConTelemetriaAsync(
        Vuelta vuelta,
        List<MuestraTelemetria> muestras,
        CancellationToken tokenCancelacion = default)
    {
        using var transaccion = await _contexto.Database.BeginTransactionAsync(tokenCancelacion);
        try
        {
            if (vuelta.EsMejorVuelta)
            {
                await DesmarcarMejorVueltaPreviaAsync(vuelta.SesionId, tokenCancelacion);
            }

            await _contexto.Vueltas.AddAsync(vuelta, tokenCancelacion);
            await ActualizarConsolidadoSesionAsync(vuelta, tokenCancelacion);
            await _contexto.SaveChangesAsync(tokenCancelacion);

            await VincularYGuardarMuestrasAsync(vuelta.Id, muestras, tokenCancelacion);
            await transaccion.CommitAsync(tokenCancelacion);

            _logger.LogInformation(
                "Vuelta #{NumeroVuelta} guardada exitosamente en SQLite. Muestras persistidas: {TotalMuestras}",
                vuelta.NumeroVuelta,
                muestras.Count);
        }
        catch (Exception excepcion)
        {
            await transaccion.RollbackAsync(tokenCancelacion);
            _logger.LogError(excepcion, "Error crítico al guardar en lote la vuelta #{NumeroVuelta} en SQLite.", vuelta.NumeroVuelta);
            throw;
        }
    }

    private async Task DesmarcarMejorVueltaPreviaAsync(Guid sesionId, CancellationToken tokenCancelacion)
    {
        var mejoresPrevias = await _contexto.Vueltas
            .Where(vuelta => vuelta.SesionId == sesionId && vuelta.EsMejorVuelta)
            .ToListAsync(tokenCancelacion);

        foreach (var vueltaPrevia in mejoresPrevias)
        {
            vueltaPrevia.EsMejorVuelta = false;
        }
    }

    private async Task ActualizarConsolidadoSesionAsync(Vuelta vuelta, CancellationToken tokenCancelacion)
    {
        var sesion = await _contexto.Sesiones.FirstOrDefaultAsync(entidad => entidad.Id == vuelta.SesionId, tokenCancelacion);
        if (sesion == null)
        {
            return;
        }

        sesion.TotalVueltas++;
        bool mejoraTiempoSesion = vuelta.EsValida &&
                                  vuelta.TiempoVueltaMs > 0 &&
                                  (sesion.MejorTiempoMs == 0 || vuelta.TiempoVueltaMs < sesion.MejorTiempoMs);

        if (mejoraTiempoSesion)
        {
            sesion.MejorTiempoMs = vuelta.TiempoVueltaMs;
        }
    }

    private async Task VincularYGuardarMuestrasAsync(
        Guid vueltaId,
        List<MuestraTelemetria> muestras,
        CancellationToken tokenCancelacion)
    {
        if (muestras.Count == 0)
        {
            return;
        }

        bool estadoPrevioDeteccion = _contexto.ChangeTracker.AutoDetectChangesEnabled;
        try
        {
            _contexto.ChangeTracker.AutoDetectChangesEnabled = false;

            foreach (var muestra in muestras)
            {
                muestra.VueltaId = vueltaId;
            }

            await _contexto.MuestrasTelemetria.AddRangeAsync(muestras, tokenCancelacion);
            await _contexto.SaveChangesAsync(tokenCancelacion);
        }
        finally
        {
            _contexto.ChangeTracker.AutoDetectChangesEnabled = estadoPrevioDeteccion;
        }
    }

    public async Task<List<Vuelta>> ObtenerVueltasPorSesionAsync(Guid sesionId, CancellationToken tokenCancelacion = default)
    {
        try
        {
            return await _contexto.Vueltas
                .AsNoTracking()
                .Where(vuelta => vuelta.SesionId == sesionId)
                .OrderBy(vuelta => vuelta.NumeroVuelta)
                .ToListAsync(tokenCancelacion);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al consultar las vueltas de la sesión {SesionId} en SQLite.", sesionId);
            throw;
        }
    }

    public async Task<List<MuestraTelemetria>> ObtenerTelemetriaVueltaAsync(Guid vueltaId, CancellationToken tokenCancelacion = default)
    {
        try
        {
            return await _contexto.MuestrasTelemetria
                .AsNoTracking()
                .Where(muestra => muestra.VueltaId == vueltaId)
                .OrderBy(muestra => muestra.DistanciaPistaMetros)
                .ToListAsync(tokenCancelacion);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al consultar las muestras de telemetría de la vuelta {VueltaId} en SQLite.", vueltaId);
            throw;
        }
    }

    public async Task<Vuelta?> ObtenerMejorVueltaAsync(string circuito, CancellationToken tokenCancelacion = default)
    {
        try
        {
            string circuitoNormalizado = circuito.Trim().ToLowerInvariant();
            return await _contexto.Vueltas
                .AsNoTracking()
                .Include(vuelta => vuelta.Sesion)
                .Where(vuelta => vuelta.Sesion != null &&
                                 vuelta.Sesion.Circuito.ToLower() == circuitoNormalizado &&
                                 vuelta.EsValida &&
                                 vuelta.TiempoVueltaMs > 0)
                .OrderBy(vuelta => vuelta.TiempoVueltaMs)
                .FirstOrDefaultAsync(tokenCancelacion);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error al obtener la mejor vuelta histórica para el circuito {Circuito}.", circuito);
            throw;
        }
    }
}
