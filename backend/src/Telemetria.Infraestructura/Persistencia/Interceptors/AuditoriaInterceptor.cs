namespace Telemetria.Infraestructura.Persistencia.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Telemetria.Dominio.Modelos;

/// <summary>
/// Interceptor de SaveChanges de EF Core que actualiza de manera transparente y centralizada
/// las marcas temporales de auditoría (CreatedAt y UpdatedAt) en horario UTC para toda EntidadBase.
/// </summary>
public sealed class AuditoriaInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData datosEvento,
        InterceptionResult<int> resultado)
    {
        ActualizarMarcasTemporales(datosEvento.Context);
        return base.SavingChanges(datosEvento, resultado);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData datosEvento,
        InterceptionResult<int> resultado,
        CancellationToken tokenCancelacion = default)
    {
        ActualizarMarcasTemporales(datosEvento.Context);
        return base.SavingChangesAsync(datosEvento, resultado, tokenCancelacion);
    }

    private static void ActualizarMarcasTemporales(DbContext? contexto)
    {
        if (contexto == null)
        {
            return;
        }

        DateTimeOffset ahoraUtc = DateTimeOffset.UtcNow;

        foreach (var entrada in contexto.ChangeTracker.Entries<EntidadBase>())
        {
            if (entrada.State == EntityState.Added)
            {
                entrada.Entity.CreatedAt = ahoraUtc;
                entrada.Entity.UpdatedAt = null;
            }
            else if (entrada.State == EntityState.Modified)
            {
                entrada.Entity.UpdatedAt = ahoraUtc;
            }
        }
    }
}
