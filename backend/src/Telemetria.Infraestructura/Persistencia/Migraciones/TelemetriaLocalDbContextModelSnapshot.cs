using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Telemetria.Infraestructura.Persistencia.Contexto;

#nullable disable

namespace Telemetria.Infraestructura.Persistencia.Migraciones;

[DbContext(typeof(TelemetriaLocalDbContext))]
partial class TelemetriaLocalDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12");

        modelBuilder.Entity("Telemetria.Dominio.Modelos.MuestraTelemetria", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasColumnName("id");

                b.Property<float>("Acelerador")
                    .HasColumnType("REAL")
                    .HasColumnName("acelerador");

                b.Property<long>("CreatedAt")
                    .HasColumnType("INTEGER")
                    .HasColumnName("created_at");

                b.Property<float>("DistanciaPistaMetros")
                    .HasColumnType("REAL")
                    .HasColumnName("distancia_pista_metros");

                b.Property<float>("Freno")
                    .HasColumnType("REAL")
                    .HasColumnName("freno");

                b.Property<float>("FuerzaGLat")
                    .HasColumnType("REAL")
                    .HasColumnName("fuerza_g_lat");

                b.Property<float>("FuerzaGLon")
                    .HasColumnType("REAL")
                    .HasColumnName("fuerza_g_lon");

                b.Property<long>("MarcaTemporalUnixMs")
                    .HasColumnType("INTEGER")
                    .HasColumnName("marca_temporal_unix_ms");

                b.Property<int>("Marcha")
                    .HasColumnType("INTEGER")
                    .HasColumnName("marcha");

                b.Property<float>("PosicionX")
                    .HasColumnType("REAL")
                    .HasColumnName("pos_x");

                b.Property<float>("PosicionY")
                    .HasColumnType("REAL")
                    .HasColumnName("pos_y");

                b.Property<float>("PosicionZ")
                    .HasColumnType("REAL")
                    .HasColumnName("pos_z");

                b.Property<int>("Rpms")
                    .HasColumnType("INTEGER")
                    .HasColumnName("rpms");

                b.Property<long?>("UpdatedAt")
                    .HasColumnType("INTEGER")
                    .HasColumnName("updated_at");

                b.Property<float>("VelocidadKmh")
                    .HasColumnType("REAL")
                    .HasColumnName("velocidad_kmh");

                b.Property<Guid>("VueltaId")
                    .HasColumnType("TEXT")
                    .HasColumnName("vuelta_id");

                b.HasKey("Id");

                b.HasIndex("VueltaId", "DistanciaPistaMetros");

                b.ToTable("telemetria_vueltas", (string)null);
            });

        modelBuilder.Entity("Telemetria.Dominio.Modelos.Sesion", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasColumnName("id");

                b.Property<string>("Circuito")
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnType("TEXT")
                    .HasColumnName("circuito");

                b.Property<string>("ConfiguracionCircuito")
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnType("TEXT")
                    .HasColumnName("configuracion_circuito");

                b.Property<long>("CreatedAt")
                    .HasColumnType("INTEGER")
                    .HasColumnName("created_at");

                b.Property<bool>("EsSimulacionMock")
                    .HasColumnType("INTEGER")
                    .HasColumnName("es_simulacion_mock");

                b.Property<long?>("FechaFin")
                    .HasColumnType("INTEGER")
                    .HasColumnName("fecha_fin");

                b.Property<long>("FechaInicio")
                    .HasColumnType("INTEGER")
                    .HasColumnName("fecha_inicio");

                b.Property<int>("MejorTiempoMs")
                    .HasColumnType("INTEGER")
                    .HasColumnName("mejor_tiempo_ms");

                b.Property<string>("ModeloVehiculo")
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnType("TEXT")
                    .HasColumnName("modelo_vehiculo");

                b.Property<bool>("SincronizadoConServidorCentral")
                    .HasColumnType("INTEGER")
                    .HasColumnName("sincronizado_servidor_central");

                b.Property<int>("TipoSesion")
                    .HasColumnType("INTEGER")
                    .HasColumnName("tipo_sesion");

                b.Property<int>("TotalVueltas")
                    .HasColumnType("INTEGER")
                    .HasColumnName("total_vueltas");

                b.Property<long?>("UpdatedAt")
                    .HasColumnType("INTEGER")
                    .HasColumnName("updated_at");

                b.Property<Guid?>("UsuarioId")
                    .HasColumnType("TEXT")
                    .HasColumnName("usuario_id");

                b.HasKey("Id");

                b.HasIndex("FechaInicio");

                b.HasIndex("SincronizadoConServidorCentral", "EsSimulacionMock", "FechaFin");

                b.ToTable("sesiones", (string)null);
            });

        modelBuilder.Entity("Telemetria.Dominio.Modelos.Vuelta", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasColumnName("id");

                b.Property<long>("CreatedAt")
                    .HasColumnType("INTEGER")
                    .HasColumnName("created_at");

                b.Property<bool>("EsMejorVuelta")
                    .HasColumnType("INTEGER")
                    .HasColumnName("es_mejor_vuelta");

                b.Property<bool>("EsValida")
                    .HasColumnType("INTEGER")
                    .HasColumnName("es_valida");

                b.Property<int>("NumeroVuelta")
                    .HasColumnType("INTEGER")
                    .HasColumnName("numero_vuelta");

                b.Property<int>("Sector1Ms")
                    .HasColumnType("INTEGER")
                    .HasColumnName("sector_1_ms");

                b.Property<int>("Sector2Ms")
                    .HasColumnType("INTEGER")
                    .HasColumnName("sector_2_ms");

                b.Property<int>("Sector3Ms")
                    .HasColumnType("INTEGER")
                    .HasColumnName("sector_3_ms");

                b.Property<Guid>("SesionId")
                    .HasColumnType("TEXT")
                    .HasColumnName("sesion_id");

                b.Property<int>("TiempoVueltaMs")
                    .HasColumnType("INTEGER")
                    .HasColumnName("tiempo_vuelta_ms");

                b.Property<string>("TiempoVueltaTexto")
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasColumnType("TEXT")
                    .HasColumnName("tiempo_vuelta_formateado");

                b.Property<long?>("UpdatedAt")
                    .HasColumnType("INTEGER")
                    .HasColumnName("updated_at");

                b.HasKey("Id");

                b.HasIndex("SesionId", "NumeroVuelta")
                    .IsUnique();

                b.ToTable("vueltas", (string)null);
            });

        modelBuilder.Entity("Telemetria.Dominio.Modelos.MuestraTelemetria", b =>
            {
                b.HasOne("Telemetria.Dominio.Modelos.Vuelta", null)
                    .WithMany("Muestras")
                    .HasForeignKey("VueltaId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

        modelBuilder.Entity("Telemetria.Dominio.Modelos.Vuelta", b =>
            {
                b.HasOne("Telemetria.Dominio.Modelos.Sesion", "Sesion")
                    .WithMany("Vueltas")
                    .HasForeignKey("SesionId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Sesion");
            });

        modelBuilder.Entity("Telemetria.Dominio.Modelos.Sesion", b =>
            {
                b.Navigation("Vueltas");
            });

        modelBuilder.Entity("Telemetria.Dominio.Modelos.Vuelta", b =>
            {
                b.Navigation("Muestras");
            });
#pragma warning restore 612, 618
    }
}
