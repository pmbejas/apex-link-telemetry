namespace Telemetria.Infraestructura.Persistencia.Interceptors;

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Interceptor de conexión SQLite que configura de manera determinista los pragmas de alto rendimiento:
/// Modo WAL (Write-Ahead Logging), llaves foráneas activadas, sincronización normal y tamaño de caché en memoria.
/// </summary>
public sealed class PragmasConexionSqliteInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection conexion, ConnectionEndEventData datosEvento)
    {
        AplicarPragmas(conexion);
        base.ConnectionOpened(conexion, datosEvento);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection conexion,
        ConnectionEndEventData datosEvento,
        CancellationToken tokenCancelacion = default)
    {
        AplicarPragmas(conexion);
        await base.ConnectionOpenedAsync(conexion, datosEvento, tokenCancelacion);
    }

    private static void AplicarPragmas(DbConnection conexion)
    {
        using var comando = conexion.CreateCommand();
        comando.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;
            PRAGMA synchronous = NORMAL;
            PRAGMA temp_store = MEMORY;
            PRAGMA cache_size = -8000;
            PRAGMA busy_timeout = 5000;
        ";
        comando.ExecuteNonQuery();
    }
}
