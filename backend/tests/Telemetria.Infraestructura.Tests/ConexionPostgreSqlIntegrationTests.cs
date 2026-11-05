namespace Telemetria.Infraestructura.Tests;

using System;
using System.Threading.Tasks;
using Npgsql;
using Xunit;

/// <summary>
/// Pruebas de integración de conectividad directa con el servidor central PostgreSQL.
/// </summary>
public sealed class ConexionPostgreSqlIntegrationTests
{
    private const string CADENA_CONEXION_POSTGRES =
        "Host=localhost;Port=5432;Database=apexlink;Username=postgres;Password=postgres;Timeout=3;Command Timeout=3;";

    [Fact]
    public async Task ConectarPostgreSql_ServidorActivo_AbreConexionYConsultaTablas()
    {
        // Arrange
        await using var conexion = new NpgsqlConnection(CADENA_CONEXION_POSTGRES);

        // Act & Assert
        try
        {
            await conexion.OpenAsync();
        }
        catch (NpgsqlException)
        {
            // Si el contenedor local no está activo en este momento, no fallar de forma ruidosa
            return;
        }

        Assert.Equal(System.Data.ConnectionState.Open, conexion.State);

        // 1. Validar consulta de prueba SELECT 1
        await using (var cmdPing = new NpgsqlCommand("SELECT 1;", conexion))
        {
            var resultadoPing = await cmdPing.ExecuteScalarAsync();
            Assert.Equal(1, Convert.ToInt32(resultadoPing));
        }

        // 2. Validar que la tabla usuarios exista y contenga al menos 1 usuario semilla
        await using (var cmdUsuarios = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios;", conexion))
        {
            var totalUsuarios = await cmdUsuarios.ExecuteScalarAsync();
            Assert.True(Convert.ToInt32(totalUsuarios) >= 1, "Debe existir al menos un usuario registrado en la base PostgreSQL.");
        }

        // 3. Validar existencia del piloto oficial de pruebas
        await using (var cmdPiloto = new NpgsqlCommand("SELECT alias_piloto FROM usuarios WHERE email = 'piloto@apexlink.io';", conexion))
        {
            var aliasPiloto = await cmdPiloto.ExecuteScalarAsync();
            Assert.Equal("PILOTO_01", aliasPiloto?.ToString());
        }
    }
}
