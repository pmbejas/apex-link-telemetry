namespace Telemetria.Infraestructura.Sincronizacion;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Telemetria.Dominio.Contratos.Repositorios;
using Telemetria.Dominio.Contratos.Sincronizacion;
using Telemetria.Dominio.Modelos;

/// <summary>
/// Implementación de sincronización asíncrona Store-and-Forward hacia PostgreSQL
/// utilizando transacciones por sesión y lotes de comandos masivos NpgsqlBatch.
/// </summary>
public sealed class SincronizadorSesionesPostgreSql : ISincronizadorSesiones
{
    private const int TAMANIO_LOTE_MUESTRAS_POSTGRES = 500;

    private readonly IRepositorioSesionesLocal _repositorioSesiones;
    private readonly IRepositorioVueltasLocal _repositorioVueltas;
    private readonly IConfiguration _configuracion;
    private readonly ILogger<SincronizadorSesionesPostgreSql> _logger;

    public SincronizadorSesionesPostgreSql(
        IRepositorioSesionesLocal repositorioSesiones,
        IRepositorioVueltasLocal repositorioVueltas,
        IConfiguration configuracion,
        ILogger<SincronizadorSesionesPostgreSql> logger)
    {
        _repositorioSesiones = repositorioSesiones;
        _repositorioVueltas = repositorioVueltas;
        _configuracion = configuracion;
        _logger = logger;
    }

    public async Task SincronizarSesionesPendientesAsync(CancellationToken tokenCancelacion = default)
    {
        string? cadenaPostgreSql = _configuracion.GetConnectionString("ServidorCentralPostgreSql");
        if (string.IsNullOrWhiteSpace(cadenaPostgreSql))
        {
            // Modo Edge autónomo: si no se configuró servidor central, las sesiones permanecen en SQLite local
            return;
        }

        var sesionesPendientes = await _repositorioSesiones.ObtenerSesionesPendientesSincronizacionAsync(tokenCancelacion);
        if (sesionesPendientes.Count == 0)
        {
            return;
        }

        try
        {
            await using var conexion = new NpgsqlConnection(cadenaPostgreSql);
            await conexion.OpenAsync(tokenCancelacion);

            foreach (var sesion in sesionesPendientes)
            {
                tokenCancelacion.ThrowIfCancellationRequested();
                await ExportarSesionTransaccionalAsync(conexion, sesion, tokenCancelacion);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            _logger.LogWarning(
                excepcion,
                "No fue posible conectar con el servidor PostgreSQL central. Se reintentará en el próximo ciclo.");
        }
    }

    private async Task ExportarSesionTransaccionalAsync(
        NpgsqlConnection conexion,
        Sesion sesion,
        CancellationToken tokenCancelacion)
    {
        await using var transaccion = await conexion.BeginTransactionAsync(tokenCancelacion);

        try
        {
            await InsertarSesionIdempotenteAsync(conexion, transaccion, sesion, tokenCancelacion);

            foreach (var vuelta in sesion.Vueltas)
            {
                await InsertarVueltaIdempotenteAsync(conexion, transaccion, vuelta, tokenCancelacion);
                var muestras = await _repositorioVueltas.ObtenerTelemetriaVueltaAsync(vuelta.Id, tokenCancelacion);
                await InsertarMuestrasVueltaEnLotesAsync(conexion, transaccion, muestras, tokenCancelacion);
            }

            await transaccion.CommitAsync(tokenCancelacion);
            await _repositorioSesiones.MarcarComoSincronizadaAsync(sesion.Id, tokenCancelacion);

            _logger.LogInformation(
                "Sesión {SesionId} ({Circuito} - {TotalVueltas} vueltas) exportada exitosamente a PostgreSQL central.",
                sesion.Id,
                sesion.Circuito,
                sesion.TotalVueltas);
        }
        catch (Exception excepcion)
        {
            await transaccion.RollbackAsync(tokenCancelacion);
            _logger.LogWarning(
                excepcion,
                "Fallo al exportar la sesión {SesionId} hacia PostgreSQL. Se reintentará en el próximo ciclo.",
                sesion.Id);
        }
    }

    private static async Task InsertarSesionIdempotenteAsync(
        NpgsqlConnection conexion,
        NpgsqlTransaction transaccion,
        Sesion sesion,
        CancellationToken tokenCancelacion)
    {
        const string sqlSesion = @"
            INSERT INTO sesiones (
                id, usuario_id, circuito, configuracion_circuito, modelo_vehiculo,
                tipo_sesion, es_simulacion_mock, fecha_inicio, fecha_fin,
                total_vueltas, mejor_tiempo_ms, created_at, updated_at
            ) VALUES (
                @id, @usuarioId, @circuito, @configuracionCircuito, @modeloVehiculo,
                @tipoSesion, @esSimulacionMock, @fechaInicio, @fechaFin,
                @totalVueltas, @mejorTiempoMs, @createdAt, @updatedAt
            ) ON CONFLICT (id) DO NOTHING;";

        await using var comando = new NpgsqlCommand(sqlSesion, conexion, transaccion);
        comando.Parameters.AddWithValue("id", sesion.Id);
        comando.Parameters.AddWithValue("usuarioId", (object?)sesion.UsuarioId ?? DBNull.Value);
        comando.Parameters.AddWithValue("circuito", sesion.Circuito);
        comando.Parameters.AddWithValue("configuracionCircuito", sesion.ConfiguracionCircuito);
        comando.Parameters.AddWithValue("modeloVehiculo", sesion.ModeloVehiculo);
        comando.Parameters.AddWithValue("tipoSesion", (int)sesion.TipoSesion);
        comando.Parameters.AddWithValue("esSimulacionMock", sesion.EsSimulacionMock);
        comando.Parameters.AddWithValue("fechaInicio", sesion.FechaInicio.UtcDateTime);
        comando.Parameters.AddWithValue("fechaFin", sesion.FechaFin.HasValue ? sesion.FechaFin.Value.UtcDateTime : DBNull.Value);
        comando.Parameters.AddWithValue("totalVueltas", sesion.TotalVueltas);
        comando.Parameters.AddWithValue("mejorTiempoMs", sesion.MejorTiempoMs);
        comando.Parameters.AddWithValue("createdAt", sesion.CreatedAt.UtcDateTime);
        comando.Parameters.AddWithValue("updatedAt", sesion.UpdatedAt.HasValue ? sesion.UpdatedAt.Value.UtcDateTime : DBNull.Value);

        await comando.ExecuteNonQueryAsync(tokenCancelacion);
    }

    private static async Task InsertarVueltaIdempotenteAsync(
        NpgsqlConnection conexion,
        NpgsqlTransaction transaccion,
        Vuelta vuelta,
        CancellationToken tokenCancelacion)
    {
        const string sqlVuelta = @"
            INSERT INTO vueltas (
                id, sesion_id, numero_vuelta, tiempo_vuelta_ms, tiempo_vuelta_formateado,
                sector_1_ms, sector_2_ms, sector_3_ms, es_valida, es_mejor_vuelta, created_at, updated_at
            ) VALUES (
                @id, @sesionId, @numeroVuelta, @tiempoVueltaMs, @tiempoTexto,
                @sector1Ms, @sector2Ms, @sector3Ms, @esValida, @esMejorVuelta, @createdAt, @updatedAt
            ) ON CONFLICT (sesion_id, numero_vuelta) DO NOTHING;";

        await using var comando = new NpgsqlCommand(sqlVuelta, conexion, transaccion);
        comando.Parameters.AddWithValue("id", vuelta.Id);
        comando.Parameters.AddWithValue("sesionId", vuelta.SesionId);
        comando.Parameters.AddWithValue("numeroVuelta", vuelta.NumeroVuelta);
        comando.Parameters.AddWithValue("tiempoVueltaMs", vuelta.TiempoVueltaMs);
        comando.Parameters.AddWithValue("tiempoTexto", vuelta.TiempoVueltaTexto);
        comando.Parameters.AddWithValue("sector1Ms", vuelta.Sector1Ms);
        comando.Parameters.AddWithValue("sector2Ms", vuelta.Sector2Ms);
        comando.Parameters.AddWithValue("sector3Ms", vuelta.Sector3Ms);
        comando.Parameters.AddWithValue("esValida", vuelta.EsValida);
        comando.Parameters.AddWithValue("esMejorVuelta", vuelta.EsMejorVuelta);
        comando.Parameters.AddWithValue("createdAt", vuelta.CreatedAt.UtcDateTime);
        comando.Parameters.AddWithValue("updatedAt", vuelta.UpdatedAt.HasValue ? vuelta.UpdatedAt.Value.UtcDateTime : DBNull.Value);

        await comando.ExecuteNonQueryAsync(tokenCancelacion);
    }

    private static async Task InsertarMuestrasVueltaEnLotesAsync(
        NpgsqlConnection conexion,
        NpgsqlTransaction transaccion,
        List<MuestraTelemetria> muestras,
        CancellationToken tokenCancelacion)
    {
        if (muestras.Count == 0)
        {
            return;
        }

        foreach (var bloqueMuestras in muestras.Chunk(TAMANIO_LOTE_MUESTRAS_POSTGRES))
        {
            await using var loteComandos = new NpgsqlBatch(conexion, transaccion);

            foreach (var muestra in bloqueMuestras)
            {
                loteComandos.BatchCommands.Add(CrearComandoMuestraLote(muestra));
            }

            await loteComandos.ExecuteNonQueryAsync(tokenCancelacion);
        }
    }

    private static NpgsqlBatchCommand CrearComandoMuestraLote(MuestraTelemetria muestra)
    {
        const string sqlMuestra = @"
            INSERT INTO telemetria_vueltas (
                id, vuelta_id, marca_temporal_unix_ms, distancia_pista_metros, velocidad_kmh,
                acelerador, freno, marcha, rpms, fuerza_g_lat, fuerza_g_lon,
                pos_x, pos_y, pos_z, created_at, updated_at
            ) VALUES (
                @id, @vueltaId, @marcaUnixMs, @distancia, @velocidad,
                @acelerador, @freno, @marcha, @rpms, @gLat, @gLon,
                @posX, @posY, @posZ, @createdAt, @updatedAt
            ) ON CONFLICT (id) DO NOTHING;";

        var comando = new NpgsqlBatchCommand(sqlMuestra);
        comando.Parameters.AddWithValue("id", muestra.Id);
        comando.Parameters.AddWithValue("vueltaId", muestra.VueltaId);
        comando.Parameters.AddWithValue("marcaUnixMs", muestra.MarcaTemporalUnixMs);
        comando.Parameters.AddWithValue("distancia", muestra.DistanciaPistaMetros);
        comando.Parameters.AddWithValue("velocidad", muestra.VelocidadKmh);
        comando.Parameters.AddWithValue("acelerador", muestra.Acelerador);
        comando.Parameters.AddWithValue("freno", muestra.Freno);
        comando.Parameters.AddWithValue("marcha", muestra.Marcha);
        comando.Parameters.AddWithValue("rpms", muestra.Rpms);
        comando.Parameters.AddWithValue("gLat", muestra.FuerzaGLat);
        comando.Parameters.AddWithValue("gLon", muestra.FuerzaGLon);
        comando.Parameters.AddWithValue("posX", muestra.PosicionX);
        comando.Parameters.AddWithValue("posY", muestra.PosicionY);
        comando.Parameters.AddWithValue("posZ", muestra.PosicionZ);
        comando.Parameters.AddWithValue("createdAt", muestra.CreatedAt.UtcDateTime);
        comando.Parameters.AddWithValue("updatedAt", muestra.UpdatedAt.HasValue ? muestra.UpdatedAt.Value.UtcDateTime : DBNull.Value);

        return comando;
    }
}
