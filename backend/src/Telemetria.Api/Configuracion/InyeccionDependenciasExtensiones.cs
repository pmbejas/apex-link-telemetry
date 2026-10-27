namespace Telemetria.Api.Configuracion;

using System.Threading.Channels;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Telemetria.Api.Autorizacion;
using Telemetria.Api.WebSockets;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Servicios;
using Telemetria.Aplicacion.Validadores;
using Telemetria.Dominio.Contratos.Hardware;
using Telemetria.Dominio.Contratos.Repositorios;
using Telemetria.Dominio.Contratos.Sincronizacion;
using Telemetria.Dominio.Modelos;
using Telemetria.Infraestructura.MemoriaCompartida;
using Telemetria.Infraestructura.Persistencia.Contexto;
using Telemetria.Infraestructura.Persistencia.Interceptors;
using Telemetria.Infraestructura.Persistencia.Repositorios;
using Telemetria.Infraestructura.Sincronizacion;

/// <summary>
/// Métodos de extensión para la configuración modular de servicios en el contenedor de inyección de dependencias.
/// </summary>
public static class InyeccionDependenciasExtensiones
{
    private const int CAPACIDAD_CANAL_PERSISTENCIA = 2000;
    private const string ARCHIVO_DB_POR_DEFECTO = "telemetria_local.db";

    public static IServiceCollection AgregarPersistenciaTelemetria(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        string cadenaNormalizada = ConstruirCadenaSqliteConRutaAbsoluta(configuracion);

        servicios.AddSingleton<PragmasConexionSqliteInterceptor>();
        servicios.AddSingleton<AuditoriaInterceptor>();

        servicios.AddDbContext<TelemetriaLocalDbContext>((proveedor, opciones) =>
        {
            opciones.UseSqlite(cadenaNormalizada);
            opciones.AddInterceptors(
                proveedor.GetRequiredService<PragmasConexionSqliteInterceptor>(),
                proveedor.GetRequiredService<AuditoriaInterceptor>());
        });

        servicios.AddScoped<IRepositorioSesionesLocal, RepositorioSesionesSqlite>();
        servicios.AddScoped<IRepositorioVueltasLocal, RepositorioVueltasSqlite>();
        servicios.AddScoped<IServicioAnalisisTelemetria, ServicioAnalisisTelemetria>();
        servicios.AddValidatorsFromAssemblyContaining<AlternarModoMockRequestValidator>();

        return servicios;
    }

    public static IServiceCollection AgregarSeguridadRbacTelemetria(this IServiceCollection servicios)
    {
        servicios.AddAuthentication();
        servicios.AddAuthorization();
        servicios.AddSingleton<IAuthorizationPolicyProvider, ProveedorPoliticasPermiso>();
        servicios.AddSingleton<IAuthorizationHandler, ManejadorAutorizacionPermiso>();

        return servicios;
    }

    public static IServiceCollection AgregarSincronizacionPostgreSql(this IServiceCollection servicios)
    {
        servicios.AddScoped<ISincronizadorSesiones, SincronizadorSesionesPostgreSql>();
        servicios.AddHostedService<ServicioSincronizacionSegundoPlano>();

        return servicios;
    }

    public static IServiceCollection AgregarMotorAdquisicionYRelay(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        bool forzarModoDemo = false)
    {
        var canalPersistencia = Channel.CreateBounded<CuadroTelemetria>(
            new BoundedChannelOptions(capacity: CAPACIDAD_CANAL_PERSISTENCIA)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleWriter = true,
                SingleReader = true
            });

        servicios.AddSingleton(canalPersistencia.Reader);
        servicios.AddSingleton(canalPersistencia.Writer);

        bool modoMockInicial = forzarModoDemo || configuracion.GetValue<bool>("Telemetria:ModoMock", false);
        servicios.AddSingleton<GeneradorTelemetriaMock>();
        servicios.AddSingleton<LectorAssettoCorsaWin32>();
        servicios.AddSingleton(proveedor => new LectorTelemetriaConmutable(
            proveedor.GetRequiredService<LectorAssettoCorsaWin32>(),
            proveedor.GetRequiredService<GeneradorTelemetriaMock>(),
            modoMockInicial));

        servicios.AddSingleton<ILectorTelemetria>(proveedor =>
            proveedor.GetRequiredService<LectorTelemetriaConmutable>());
        servicios.AddSingleton<IControlModoTelemetria>(proveedor =>
            proveedor.GetRequiredService<LectorTelemetriaConmutable>());

        servicios.AddSingleton<ManejadorWebSocketTelemetria>();
        servicios.AddSingleton<IDifusorTelemetria>(proveedor =>
            proveedor.GetRequiredService<ManejadorWebSocketTelemetria>());

        servicios.AddHostedService<ServicioTelemetriaRelay>();
        servicios.AddHostedService<ServicioGestionVueltas>();

        return servicios;
    }

    public static async Task MigrarBaseDatosSqliteAsync(this WebApplication aplicacion)
    {
        using var alcance = aplicacion.Services.CreateScope();
        var contextoDb = alcance.ServiceProvider.GetRequiredService<TelemetriaLocalDbContext>();
        await contextoDb.Database.MigrateAsync();
        await contextoDb.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
    }

    public static async Task EjecutarCheckpointFinalSqliteAsync(this WebApplication aplicacion)
    {
        var registrador = aplicacion.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("CicloVidaSqlite");

        try
        {
            using var alcance = aplicacion.Services.CreateScope();
            var contextoDb = alcance.ServiceProvider.GetRequiredService<TelemetriaLocalDbContext>();
            await contextoDb.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");
            registrador.LogInformation("Checkpoint WAL final completado. Base SQLite consolidada.");
        }
        catch (Exception excepcion)
        {
            registrador.LogError(excepcion, "Error al ejecutar checkpoint WAL durante el cierre del host.");
        }
    }

    private static string ConstruirCadenaSqliteConRutaAbsoluta(IConfiguration configuracion)
    {
        string cadenaConfigurada = configuracion.GetConnectionString("TelemetriaLocal")
            ?? $"Data Source={ARCHIVO_DB_POR_DEFECTO};Mode=ReadWriteCreate;";

        var constructorSqlite = new SqliteConnectionStringBuilder(cadenaConfigurada);
        if (string.IsNullOrWhiteSpace(constructorSqlite.DataSource))
        {
            constructorSqlite.DataSource = ARCHIVO_DB_POR_DEFECTO;
        }

        if (!Path.IsPathRooted(constructorSqlite.DataSource))
        {
            constructorSqlite.DataSource = Path.Combine(
                AppContext.BaseDirectory,
                constructorSqlite.DataSource);
        }

        return constructorSqlite.ToString();
    }
}
