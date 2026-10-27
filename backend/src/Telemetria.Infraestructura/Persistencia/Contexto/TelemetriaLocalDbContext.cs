namespace Telemetria.Infraestructura.Persistencia.Contexto;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Telemetria.Dominio.Modelos;

/// <summary>
/// Contexto de Entity Framework Core para la base de datos local SQLite (Nivel 1 Edge).
/// Configura llaves foráneas, índices de alta velocidad para consulta de telemetría y conversión binaria de DateTimeOffset.
/// </summary>
public sealed class TelemetriaLocalDbContext : DbContext
{
    public TelemetriaLocalDbContext(DbContextOptions<TelemetriaLocalDbContext> opciones)
        : base(opciones)
    {
    }

    public DbSet<Sesion> Sesiones => Set<Sesion>();

    public DbSet<Vuelta> Vueltas => Set<Vuelta>();

    public DbSet<MuestraTelemetria> MuestrasTelemetria => Set<MuestraTelemetria>();

    protected override void ConfigureConventions(ModelConfigurationBuilder constructorConfiguracion)
    {
        // SQLite no soporta ORDER BY sobre DateTimeOffset almacenado como texto; se persiste como entero binario de 64 bits ordenable.
        constructorConfiguracion.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();

        constructorConfiguracion.Properties<DateTimeOffset?>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder constructorModelo)
    {
        base.OnModelCreating(constructorModelo);

        ConfigurarEntidadSesion(constructorModelo.Entity<Sesion>());
        ConfigurarEntidadVuelta(constructorModelo.Entity<Vuelta>());
        ConfigurarEntidadMuestraTelemetria(constructorModelo.Entity<MuestraTelemetria>());
    }

    private static void ConfigurarEntidadSesion(EntityTypeBuilder<Sesion> entidad)
    {
        entidad.ToTable("sesiones");
        entidad.HasKey(sesion => sesion.Id);
        entidad.Property(sesion => sesion.Id).HasColumnName("id");
        entidad.Property(sesion => sesion.UsuarioId).HasColumnName("usuario_id");
        entidad.Property(sesion => sesion.Circuito).HasColumnName("circuito").HasMaxLength(64).IsRequired();
        entidad.Property(sesion => sesion.ConfiguracionCircuito).HasColumnName("configuracion_circuito").HasMaxLength(64);
        entidad.Property(sesion => sesion.ModeloVehiculo).HasColumnName("modelo_vehiculo").HasMaxLength(64).IsRequired();
        entidad.Property(sesion => sesion.TipoSesion).HasColumnName("tipo_sesion");
        entidad.Property(sesion => sesion.EsSimulacionMock).HasColumnName("es_simulacion_mock");
        entidad.Property(sesion => sesion.FechaInicio).HasColumnName("fecha_inicio").IsRequired();
        entidad.Property(sesion => sesion.FechaFin).HasColumnName("fecha_fin");
        entidad.Property(sesion => sesion.TotalVueltas).HasColumnName("total_vueltas");
        entidad.Property(sesion => sesion.MejorTiempoMs).HasColumnName("mejor_tiempo_ms");
        entidad.Property(sesion => sesion.SincronizadoConServidorCentral).HasColumnName("sincronizado_servidor_central");
        entidad.Property(sesion => sesion.CreatedAt).HasColumnName("created_at").IsRequired();
        entidad.Property(sesion => sesion.UpdatedAt).HasColumnName("updated_at");

        entidad.HasIndex(sesion => sesion.FechaInicio);
        entidad.HasIndex(sesion => new { sesion.SincronizadoConServidorCentral, sesion.EsSimulacionMock, sesion.FechaFin });
    }

    private static void ConfigurarEntidadVuelta(EntityTypeBuilder<Vuelta> entidad)
    {
        entidad.ToTable("vueltas");
        entidad.HasKey(vuelta => vuelta.Id);
        entidad.Property(vuelta => vuelta.Id).HasColumnName("id");
        entidad.Property(vuelta => vuelta.SesionId).HasColumnName("sesion_id");
        entidad.Property(vuelta => vuelta.NumeroVuelta).HasColumnName("numero_vuelta");
        entidad.Property(vuelta => vuelta.TiempoVueltaMs).HasColumnName("tiempo_vuelta_ms");
        entidad.Property(vuelta => vuelta.TiempoVueltaTexto).HasColumnName("tiempo_vuelta_formateado").HasMaxLength(16).IsRequired();
        entidad.Property(vuelta => vuelta.Sector1Ms).HasColumnName("sector_1_ms");
        entidad.Property(vuelta => vuelta.Sector2Ms).HasColumnName("sector_2_ms");
        entidad.Property(vuelta => vuelta.Sector3Ms).HasColumnName("sector_3_ms");
        entidad.Property(vuelta => vuelta.EsValida).HasColumnName("es_valida");
        entidad.Property(vuelta => vuelta.EsMejorVuelta).HasColumnName("es_mejor_vuelta");
        entidad.Property(vuelta => vuelta.CreatedAt).HasColumnName("created_at").IsRequired();
        entidad.Property(vuelta => vuelta.UpdatedAt).HasColumnName("updated_at");

        entidad.HasIndex(vuelta => new { vuelta.SesionId, vuelta.NumeroVuelta }).IsUnique();

        entidad.HasOne(vuelta => vuelta.Sesion)
            .WithMany(sesion => sesion.Vueltas)
            .HasForeignKey(vuelta => vuelta.SesionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarEntidadMuestraTelemetria(EntityTypeBuilder<MuestraTelemetria> entidad)
    {
        entidad.ToTable("telemetria_vueltas");
        entidad.HasKey(muestra => muestra.Id);
        entidad.Property(muestra => muestra.Id).HasColumnName("id");
        entidad.Property(muestra => muestra.VueltaId).HasColumnName("vuelta_id");
        entidad.Property(muestra => muestra.MarcaTemporalUnixMs).HasColumnName("marca_temporal_unix_ms");
        entidad.Property(muestra => muestra.DistanciaPistaMetros).HasColumnName("distancia_pista_metros");
        entidad.Property(muestra => muestra.VelocidadKmh).HasColumnName("velocidad_kmh");
        entidad.Property(muestra => muestra.Acelerador).HasColumnName("acelerador");
        entidad.Property(muestra => muestra.Freno).HasColumnName("freno");
        entidad.Property(muestra => muestra.Marcha).HasColumnName("marcha");
        entidad.Property(muestra => muestra.Rpms).HasColumnName("rpms");
        entidad.Property(muestra => muestra.FuerzaGLat).HasColumnName("fuerza_g_lat");
        entidad.Property(muestra => muestra.FuerzaGLon).HasColumnName("fuerza_g_lon");
        entidad.Property(muestra => muestra.PosicionX).HasColumnName("pos_x");
        entidad.Property(muestra => muestra.PosicionY).HasColumnName("pos_y");
        entidad.Property(muestra => muestra.PosicionZ).HasColumnName("pos_z");
        entidad.Property(muestra => muestra.CreatedAt).HasColumnName("created_at").IsRequired();
        entidad.Property(muestra => muestra.UpdatedAt).HasColumnName("updated_at");

        entidad.HasIndex(muestra => new { muestra.VueltaId, muestra.DistanciaPistaMetros });

        entidad.HasOne<Vuelta>()
            .WithMany(vuelta => vuelta.Muestras)
            .HasForeignKey(muestra => muestra.VueltaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
