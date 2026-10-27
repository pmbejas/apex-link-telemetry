namespace Telemetria.Infraestructura.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Telemetria.Dominio.Contratos.Repositorios;
using Telemetria.Dominio.Contratos.Sincronizacion;
using Telemetria.Dominio.Modelos;
using Telemetria.Infraestructura.Sincronizacion;
using Xunit;

public sealed class SincronizacionPostgreSqlTests
{
    private readonly Mock<IRepositorioSesionesLocal> _repositorioSesionesMock;
    private readonly Mock<IRepositorioVueltasLocal> _repositorioVueltasMock;
    private readonly Mock<ILogger<SincronizadorSesionesPostgreSql>> _loggerSincronizadorMock;
    private readonly Mock<ILogger<ServicioSincronizacionSegundoPlano>> _loggerServicioMock;

    public SincronizacionPostgreSqlTests()
    {
        _repositorioSesionesMock = new Mock<IRepositorioSesionesLocal>();
        _repositorioVueltasMock = new Mock<IRepositorioVueltasLocal>();
        _loggerSincronizadorMock = new Mock<ILogger<SincronizadorSesionesPostgreSql>>();
        _loggerServicioMock = new Mock<ILogger<ServicioSincronizacionSegundoPlano>>();
    }

    [Fact]
    public async Task SincronizarSesionesPendientesAsync_CadenaVacia_ModoEdgeAutonomoSinExcepcion()
    {
        // Arrange: Sin cadena de conexión central configurada (modo boxes desconectado)
        var configuracion = ConstruirConfiguracion(null);
        var sincronizador = new SincronizadorSesionesPostgreSql(
            _repositorioSesionesMock.Object,
            _repositorioVueltasMock.Object,
            configuracion,
            _loggerSincronizadorMock.Object);

        // Act
        await sincronizador.SincronizarSesionesPendientesAsync(CancellationToken.None);

        // Assert: Modo Edge autónomo no consulta SQLite ni abre conexiones
        _repositorioSesionesMock.Verify(
            r => r.ObtenerSesionesPendientesSincronizacionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SincronizarSesionesPendientesAsync_SinSesionesPendientes_NoIntentaConectar()
    {
        // Arrange: Cadena configurada pero cero sesiones pendientes
        var configuracion = ConstruirConfiguracion("Host=servidor-falso;Database=telemetria;");
        _repositorioSesionesMock
            .Setup(r => r.ObtenerSesionesPendientesSincronizacionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion>());

        var sincronizador = new SincronizadorSesionesPostgreSql(
            _repositorioSesionesMock.Object,
            _repositorioVueltasMock.Object,
            configuracion,
            _loggerSincronizadorMock.Object);

        // Act & Assert: Debe retornar sin arrojar excepción de conexión Npgsql
        await sincronizador.SincronizarSesionesPendientesAsync(CancellationToken.None);

        _repositorioSesionesMock.Verify(
            r => r.ObtenerSesionesPendientesSincronizacionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SincronizarSesionesPendientesAsync_ErrorConexionPostgreSql_CapturaExcepcionYNoLanza()
    {
        // Arrange: Cadena inválida apuntando a puerto cerrado para forzar error de conexión
        var configuracion = ConstruirConfiguracion("Host=127.0.0.1;Port=59999;Database=telemetria;Timeout=1;Command Timeout=1;");
        var sesionEjemplo = new Sesion
        {
            Circuito = "monza",
            ConfiguracionCircuito = "gp",
            ModeloVehiculo = "ferrari_488_gt3",
            TipoSesion = 1,
            EsSimulacionMock = false,
            FechaInicio = DateTimeOffset.UtcNow.AddMinutes(-30),
            FechaFin = DateTimeOffset.UtcNow,
            TotalVueltas = 10,
            MejorTiempoMs = 107500
        };

        _repositorioSesionesMock
            .Setup(r => r.ObtenerSesionesPendientesSincronizacionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { sesionEjemplo });

        var sincronizador = new SincronizadorSesionesPostgreSql(
            _repositorioSesionesMock.Object,
            _repositorioVueltasMock.Object,
            configuracion,
            _loggerSincronizadorMock.Object);

        // Act & Assert: El método no debe propagar la excepción hacia arriba
        await sincronizador.SincronizarSesionesPendientesAsync(CancellationToken.None);

        _repositorioSesionesMock.Verify(
            r => r.ObtenerSesionesPendientesSincronizacionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ServicioSincronizacionSegundoPlano_TokenCancelado_DetieneEjecucionSinExcepcion()
    {
        // Arrange
        var servicios = new ServiceCollection();
        servicios.AddSingleton(Mock.Of<ISincronizadorSesiones>());
        var proveedor = servicios.BuildServiceProvider();

        var servicio = new ServicioSincronizacionSegundoPlano(proveedor, _loggerServicioMock.Object);
        using var fuenteCancelacion = new CancellationTokenSource();
        fuenteCancelacion.Cancel(); // Token ya cancelado para detención inmediata

        // Act & Assert: StartAsync con token cancelado no debe lanzar excepción
        await servicio.StartAsync(fuenteCancelacion.Token);
        await servicio.StopAsync(CancellationToken.None);
    }

    private static IConfiguration ConstruirConfiguracion(string? cadenaPostgreSql)
    {
        var datos = new Dictionary<string, string?>();
        if (cadenaPostgreSql is not null)
        {
            datos["ConnectionStrings:ServidorCentralPostgreSql"] = cadenaPostgreSql;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(datos)
            .Build();
    }
}
