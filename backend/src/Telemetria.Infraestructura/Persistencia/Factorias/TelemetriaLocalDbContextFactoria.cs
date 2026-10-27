namespace Telemetria.Infraestructura.Persistencia.Factorias;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Telemetria.Infraestructura.Persistencia.Contexto;

/// <summary>
/// Factoría de diseño para Entity Framework Core CLI y herramientas de migración.
/// </summary>
public sealed class TelemetriaLocalDbContextFactoria : IDesignTimeDbContextFactory<TelemetriaLocalDbContext>
{
    public TelemetriaLocalDbContext CreateDbContext(string[] args)
    {
        var constructorOpciones = new DbContextOptionsBuilder<TelemetriaLocalDbContext>();
        constructorOpciones.UseSqlite("Data Source=telemetria_diseno.db");
        return new TelemetriaLocalDbContext(constructorOpciones.Options);
    }
}
