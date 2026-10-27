namespace Telemetria.Infraestructura.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Telemetria.Dominio.Modelos;
using Telemetria.Infraestructura.Persistencia.Contexto;
using Telemetria.Infraestructura.Persistencia.Interceptors;
using Telemetria.Infraestructura.Persistencia.Repositorios;
using Xunit;

public sealed class RepositoriosSqliteTests : IAsyncDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly TelemetriaLocalDbContext _contexto;
    private readonly RepositorioSesionesSqlite _repositorioSesiones;
    private readonly RepositorioVueltasSqlite _repositorioVueltas;

    public RepositoriosSqliteTests()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        var opciones = new DbContextOptionsBuilder<TelemetriaLocalDbContext>()
            .UseSqlite(_conexion)
            .AddInterceptors(new PragmasConexionSqliteInterceptor(), new AuditoriaInterceptor())
            .Options;

        _contexto = new TelemetriaLocalDbContext(opciones);
        _contexto.Database.EnsureCreated();

        _repositorioSesiones = new RepositorioSesionesSqlite(_contexto, NullLogger<RepositorioSesionesSqlite>.Instance);
        _repositorioVueltas = new RepositorioVueltasSqlite(_contexto, NullLogger<RepositorioVueltasSqlite>.Instance);
    }

    [Fact]
    public async Task AuditoriaInterceptor_AlCrearYModificarEntidad_AsignaMarcasTemporalesCorrectamente()
    {
        Guid sesionId = await _repositorioSesiones.IniciarSesionAsync(
            "monza", "gp", "ferrari_488", 2, false);

        var sesionCreada = await _contexto.Sesiones.FindAsync(sesionId);
        Assert.NotNull(sesionCreada);
        Assert.True(sesionCreada.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(sesionCreada.UpdatedAt);

        await Task.Delay(15);
        await _repositorioSesiones.FinalizarSesionAsync(sesionId);

        var sesionModificada = await _contexto.Sesiones.FindAsync(sesionId);
        Assert.NotNull(sesionModificada);
        Assert.NotNull(sesionModificada.UpdatedAt);
        Assert.True(sesionModificada.UpdatedAt >= sesionModificada.CreatedAt);
    }

    [Fact]
    public async Task IniciarYFinalizarSesion_CalculaTotalVueltasYMejorTiempo()
    {
        Guid sesionId = await _repositorioSesiones.IniciarSesionAsync(
            "spa", "gp", "porsche_911", 2, false);

        var vuelta1 = new Vuelta
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            NumeroVuelta = 1,
            TiempoVueltaMs = 125000,
            TiempoVueltaTexto = "2:05.000",
            EsValida = true,
            EsMejorVuelta = true
        };

        var vuelta2 = new Vuelta
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            NumeroVuelta = 2,
            TiempoVueltaMs = 123500,
            TiempoVueltaTexto = "2:03.500",
            EsValida = true,
            EsMejorVuelta = true
        };

        await _repositorioVueltas.GuardarVueltaConTelemetriaAsync(vuelta1, new List<MuestraTelemetria>());
        await _repositorioVueltas.GuardarVueltaConTelemetriaAsync(vuelta2, new List<MuestraTelemetria>());

        await _repositorioSesiones.FinalizarSesionAsync(sesionId);

        var sesionFinal = await _contexto.Sesiones.FindAsync(sesionId);
        Assert.NotNull(sesionFinal);
        Assert.Equal(2, sesionFinal.TotalVueltas);
        Assert.Equal(123500, sesionFinal.MejorTiempoMs);
    }

    [Fact]
    public async Task ObtenerHistorialSesiones_ConPaginacion_RespetaLimites()
    {
        for (int i = 1; i <= 5; i++)
        {
            await _repositorioSesiones.IniciarSesionAsync(
                $"circuito_{i}", "std", "auto_1", 2, false);
        }

        int total = await _repositorioSesiones.ContarTotalSesionesAsync();
        Assert.Equal(5, total);

        var pagina1 = await _repositorioSesiones.ObtenerHistorialSesionesAsync(pagina: 1, tamanioPagina: 2);
        Assert.Equal(2, pagina1.Count);

        var pagina3 = await _repositorioSesiones.ObtenerHistorialSesionesAsync(pagina: 3, tamanioPagina: 2);
        Assert.Single(pagina3);
    }

    [Fact]
    public async Task ObtenerSesionesPendientesSincronizacion_ExcluyeMockYSesionesActivas()
    {
        Guid idRealActiva = await _repositorioSesiones.IniciarSesionAsync("silverstone", "gp", "auto_1", 2, false);
        Guid idMockFinalizada = await _repositorioSesiones.IniciarSesionAsync("silverstone", "gp", "auto_1", 2, true);
        await _repositorioSesiones.FinalizarSesionAsync(idMockFinalizada);

        Guid idRealFinalizada = await _repositorioSesiones.IniciarSesionAsync("silverstone", "gp", "auto_1", 2, false);
        await _repositorioSesiones.FinalizarSesionAsync(idRealFinalizada);

        var pendientes = await _repositorioSesiones.ObtenerSesionesPendientesSincronizacionAsync();
        Assert.Single(pendientes);
        Assert.Equal(idRealFinalizada, pendientes[0].Id);

        await _repositorioSesiones.MarcarComoSincronizadaAsync(idRealFinalizada);
        var pendientesPostSync = await _repositorioSesiones.ObtenerSesionesPendientesSincronizacionAsync();
        Assert.Empty(pendientesPostSync);
    }

    [Fact]
    public async Task GuardarVueltaConTelemetria_PersisteMuestrasYRecalculaRecord()
    {
        Guid sesionId = await _repositorioSesiones.IniciarSesionAsync("imola", "gp", "bmw_m4", 2, false);

        var vuelta1 = new Vuelta
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            NumeroVuelta = 1,
            TiempoVueltaMs = 95000,
            TiempoVueltaTexto = "1:35.000",
            EsValida = true,
            EsMejorVuelta = true
        };

        var muestras = new List<MuestraTelemetria>
        {
            new() { DistanciaPistaMetros = 10f, VelocidadKmh = 150f, Marcha = 3 },
            new() { DistanciaPistaMetros = 20f, VelocidadKmh = 160f, Marcha = 3 }
        };

        await _repositorioVueltas.GuardarVueltaConTelemetriaAsync(vuelta1, muestras);

        var vueltasSesion = await _repositorioVueltas.ObtenerVueltasPorSesionAsync(sesionId);
        Assert.Single(vueltasSesion);
        Assert.True(vueltasSesion[0].EsMejorVuelta);

        var muestrasGuardadas = await _repositorioVueltas.ObtenerTelemetriaVueltaAsync(vuelta1.Id);
        Assert.Equal(2, muestrasGuardadas.Count);
        Assert.Equal(10f, muestrasGuardadas[0].DistanciaPistaMetros);

        // Guardar segunda vuelta que es nuevo récord
        var vuelta2 = new Vuelta
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            NumeroVuelta = 2,
            TiempoVueltaMs = 93000,
            TiempoVueltaTexto = "1:33.000",
            EsValida = true,
            EsMejorVuelta = true
        };

        await _repositorioVueltas.GuardarVueltaConTelemetriaAsync(vuelta2, new List<MuestraTelemetria>());

        var vueltasActualizadas = await _repositorioVueltas.ObtenerVueltasPorSesionAsync(sesionId);
        var vuelta1Actualizada = vueltasActualizadas.First(v => v.NumeroVuelta == 1);
        var vuelta2Actualizada = vueltasActualizadas.First(v => v.NumeroVuelta == 2);

        Assert.False(vuelta1Actualizada.EsMejorVuelta);
        Assert.True(vuelta2Actualizada.EsMejorVuelta);

        var mejorVueltaCircuito = await _repositorioVueltas.ObtenerMejorVueltaAsync("imola");
        Assert.NotNull(mejorVueltaCircuito);
        Assert.Equal(93000, mejorVueltaCircuito.TiempoVueltaMs);
    }

    public async ValueTask DisposeAsync()
    {
        await _contexto.DisposeAsync();
        await _conexion.DisposeAsync();
    }
}
