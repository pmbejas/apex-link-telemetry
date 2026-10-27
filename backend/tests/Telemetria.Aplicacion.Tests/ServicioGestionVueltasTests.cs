namespace Telemetria.Aplicacion.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Aplicacion.Servicios;
using Telemetria.Dominio.Contratos.Repositorios;
using Telemetria.Dominio.Modelos;
using Xunit;

public sealed class ServicioGestionVueltasTests
{
    [Fact]
    public async Task AlCompletarVueltaValida_PersisteVueltaEnRepositorioYEmitLAP_COMPLETED()
    {
        var canal = Channel.CreateUnbounded<CuadroTelemetria>();
        var mockSesiones = new Mock<IRepositorioSesionesLocal>();
        var mockVueltas = new Mock<IRepositorioVueltasLocal>();
        var mockDifusor = new Mock<IDifusorTelemetria>();

        Guid sesionId = Guid.NewGuid();
        mockSesiones
            .Setup(r => r.IniciarSesionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesionId);

        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IRepositorioSesionesLocal))).Returns(mockSesiones.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IRepositorioVueltasLocal))).Returns(mockVueltas.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        var servicio = new ServicioGestionVueltas(
            canal.Reader,
            mockScopeFactory.Object,
            NullLogger<ServicioGestionVueltas>.Instance,
            mockDifusor.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // 1. Primer cuadro en pista (vuelta 0 acumulando muestras)
        var cuadro1 = new CuadroTelemetria
        {
            EstadoSimulador = 2,
            Circuito = "monza",
            ConfiguracionCircuito = "gp",
            ModeloAuto = "ferrari_488",
            TipoSesion = 2,
            VueltasCompletadas = 0,
            DistanciaMetros = 100f,
            VelocidadKmh = 220f
        };
        await canal.Writer.WriteAsync(cuadro1);

        // 2. Segundo cuadro cruza la meta (vuelta completada 1 con tiempo oficial)
        Guid vueltaIdEsperada = Guid.NewGuid();
        var cuadro2 = new CuadroTelemetria
        {
            EstadoSimulador = 2,
            Circuito = "monza",
            ConfiguracionCircuito = "gp",
            ModeloAuto = "ferrari_488",
            TipoSesion = 2,
            VueltasCompletadas = 1,
            UltimoTiempoVueltaMs = 82500,
            UltimoTiempoVueltaTexto = "1:22.500",
            TiemposSectoresMs = new[] { 27500, 27500, 27500 },
            EsVueltaValida = true,
            VueltaCerradaId = vueltaIdEsperada,
            DistanciaMetros = 10f,
            VelocidadKmh = 230f
        };
        await canal.Writer.WriteAsync(cuadro2);

        canal.Writer.Complete();

        // Iniciar y esperar que el canal se procese por completo
        await servicio.StartAsync(cts.Token);
        await servicio.ExecuteTask!;

        // Verificaciones
        mockSesiones.Verify(r => r.IniciarSesionAsync("monza", "gp", "ferrari_488", 2, false, It.IsAny<CancellationToken>()), Times.Once);

        mockVueltas.Verify(r => r.GuardarVueltaConTelemetriaAsync(
            It.Is<Vuelta>(v => v.Id == vueltaIdEsperada && v.NumeroVuelta == 1 && v.TiempoVueltaMs == 82500 && v.EsValida),
            It.Is<List<MuestraTelemetria>>(muestras => muestras.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);

        mockDifusor.Verify(d => d.EmitirEventoVueltaAsync(
            It.Is<EventoVueltaCompletadaDto>(e => e.IdentificadorVueltaSesion == vueltaIdEsperada.ToString() && e.TiempoVueltaMs == 82500 && e.CantidadMuestrasTelemetria == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConVueltaIncompletaTiempoCero_DescartaMuestrasYNoPersisteVuelta()
    {
        var canal = Channel.CreateUnbounded<CuadroTelemetria>();
        var mockSesiones = new Mock<IRepositorioSesionesLocal>();
        var mockVueltas = new Mock<IRepositorioVueltasLocal>();
        var mockDifusor = new Mock<IDifusorTelemetria>();

        Guid sesionId = Guid.NewGuid();
        mockSesiones
            .Setup(r => r.IniciarSesionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesionId);

        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IRepositorioSesionesLocal))).Returns(mockSesiones.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IRepositorioVueltasLocal))).Returns(mockVueltas.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        var servicio = new ServicioGestionVueltas(
            canal.Reader,
            mockScopeFactory.Object,
            NullLogger<ServicioGestionVueltas>.Instance,
            mockDifusor.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // Cuadro con VueltasCompletadas = 0
        await canal.Writer.WriteAsync(new CuadroTelemetria
        {
            EstadoSimulador = 2,
            Circuito = "spa",
            VueltasCompletadas = 0,
            UltimoTiempoVueltaMs = 0
        });

        // Cruce sin tiempo válido (ej. out-lap)
        await canal.Writer.WriteAsync(new CuadroTelemetria
        {
            EstadoSimulador = 2,
            Circuito = "spa",
            VueltasCompletadas = 1,
            UltimoTiempoVueltaMs = 0
        });

        canal.Writer.Complete();

        await servicio.StartAsync(cts.Token);
        await servicio.ExecuteTask!;

        mockVueltas.Verify(r => r.GuardarVueltaConTelemetriaAsync(
            It.IsAny<Vuelta>(), It.IsAny<List<MuestraTelemetria>>(), It.IsAny<CancellationToken>()), Times.Never);

        mockDifusor.Verify(d => d.EmitirEventoVueltaAsync(
            It.IsAny<EventoVueltaCompletadaDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AlDesconectarSimulador_CierraSesionActiva()
    {
        var canal = Channel.CreateUnbounded<CuadroTelemetria>();
        var mockSesiones = new Mock<IRepositorioSesionesLocal>();
        var mockVueltas = new Mock<IRepositorioVueltasLocal>();

        Guid sesionId = Guid.NewGuid();
        mockSesiones
            .Setup(r => r.IniciarSesionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesionId);

        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IRepositorioSesionesLocal))).Returns(mockSesiones.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IRepositorioVueltasLocal))).Returns(mockVueltas.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        var servicio = new ServicioGestionVueltas(
            canal.Reader,
            mockScopeFactory.Object,
            NullLogger<ServicioGestionVueltas>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // 1. Inicia en pista
        await canal.Writer.WriteAsync(new CuadroTelemetria
        {
            EstadoSimulador = 2,
            Circuito = "suzuka",
            VueltasCompletadas = 0
        });

        // 2. Se apaga el simulador (EstadoSimulador = 0)
        await canal.Writer.WriteAsync(new CuadroTelemetria
        {
            EstadoSimulador = 0
        });

        canal.Writer.Complete();

        await servicio.StartAsync(cts.Token);
        await servicio.ExecuteTask!;

        mockSesiones.Verify(r => r.FinalizarSesionAsync(sesionId, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
