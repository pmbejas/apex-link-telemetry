namespace Telemetria.Aplicacion.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Telemetria.Aplicacion.Servicios;
using Telemetria.Dominio.Contratos.Repositorios;
using Telemetria.Dominio.Modelos;
using Xunit;

public sealed class ServicioAnalisisTelemetriaTests
{
    private readonly Mock<IRepositorioSesionesLocal> _mockSesiones = new();
    private readonly Mock<IRepositorioVueltasLocal> _mockVueltas = new();
    private readonly ServicioAnalisisTelemetria _servicio;

    public ServicioAnalisisTelemetriaTests()
    {
        _servicio = new ServicioAnalisisTelemetria(_mockSesiones.Object, _mockVueltas.Object);
    }

    [Fact]
    public async Task ObtenerSesionesPaginadasAsync_ConDatos_RetornaPaginaYCalculaTotalPaginas()
    {
        var sesiones = new List<Sesion>
        {
            new() { Id = Guid.NewGuid(), Circuito = "monza", TotalVueltas = 10, FechaInicio = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), Circuito = "spa", TotalVueltas = 15, FechaInicio = DateTimeOffset.UtcNow }
        };

        _mockSesiones.Setup(r => r.ObtenerHistorialSesionesAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesiones);
        _mockSesiones.Setup(r => r.ContarTotalSesionesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var respuesta = await _servicio.ObtenerSesionesPaginadasAsync(numeroPagina: 1, tamanioPagina: 2);

        Assert.NotNull(respuesta);
        Assert.Equal(2, respuesta.Elementos.Count);
        Assert.Equal(1, respuesta.PaginaActual);
        Assert.Equal(2, respuesta.TamanioPagina);
        Assert.Equal(5, respuesta.TotalRegistros);
        Assert.Equal(3, respuesta.TotalPaginas);
        Assert.False(respuesta.TienePaginaAnterior);
        Assert.True(respuesta.TienePaginaSiguiente);
    }

    [Fact]
    public async Task ObtenerVueltasPorSesionAsync_ConVueltas_MapeaCamposCorrectamente()
    {
        Guid sesionId = Guid.NewGuid();
        var vueltas = new List<Vuelta>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SesionId = sesionId,
                NumeroVuelta = 1,
                TiempoVueltaMs = 85000,
                TiempoVueltaTexto = "1:25.000",
                Sector1Ms = 28000,
                Sector2Ms = 28500,
                Sector3Ms = 28500,
                EsValida = true,
                EsMejorVuelta = true
            }
        };

        _mockVueltas.Setup(r => r.ObtenerVueltasPorSesionAsync(sesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vueltas);

        var resultado = await _servicio.ObtenerVueltasPorSesionAsync(sesionId);

        Assert.Single(resultado);
        Assert.Equal(85000, resultado[0].TiempoVueltaMs);
        Assert.Equal("1:25.000", resultado[0].TiempoVueltaTexto);
        Assert.True(resultado[0].EsMejorVuelta);
    }

    [Fact]
    public async Task ObtenerTelemetriaDeVueltaAsync_ConMuestras_RedondeaDecimales()
    {
        Guid vueltaId = Guid.NewGuid();
        var muestras = new List<MuestraTelemetria>
        {
            new()
            {
                DistanciaPistaMetros = 125.456f,
                VelocidadKmh = 230.789f,
                Acelerador = 1.0f,
                Freno = 0.0f,
                Marcha = 6,
                Rpms = 11500,
                FuerzaGLat = 1.556f,
                FuerzaGLon = -0.333f
            }
        };

        _mockVueltas.Setup(r => r.ObtenerTelemetriaVueltaAsync(vueltaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(muestras);

        var resultado = await _servicio.ObtenerTelemetriaDeVueltaAsync(vueltaId);

        Assert.Single(resultado);
        Assert.Equal(125.46f, resultado[0].DistanciaPistaMetros);
        Assert.Equal(230.8f, resultado[0].VelocidadKmh);
        Assert.Equal(1.56f, resultado[0].FuerzaGLat);
        Assert.Equal(-0.33f, resultado[0].FuerzaGLon);
    }

    [Fact]
    public async Task CompararVueltasPorDistanciaAsync_ConDosVueltasValidas_RealizaInterpolacionYCalculaDeltas()
    {
        Guid vueltaBaseId = Guid.NewGuid();
        Guid vueltaCompId = Guid.NewGuid();

        var telemetriaBase = new List<MuestraTelemetria>
        {
            new() { DistanciaPistaMetros = 0f, VelocidadKmh = 180f, Acelerador = 1.0f, Freno = 0f },
            new() { DistanciaPistaMetros = 50f, VelocidadKmh = 200f, Acelerador = 1.0f, Freno = 0f },
            new() { DistanciaPistaMetros = 100f, VelocidadKmh = 220f, Acelerador = 1.0f, Freno = 0f }
        };

        var telemetriaComp = new List<MuestraTelemetria>
        {
            new() { DistanciaPistaMetros = 0f, VelocidadKmh = 190f, Acelerador = 1.0f, Freno = 0f },
            new() { DistanciaPistaMetros = 40f, VelocidadKmh = 210f, Acelerador = 1.0f, Freno = 0f },
            new() { DistanciaPistaMetros = 100f, VelocidadKmh = 230f, Acelerador = 1.0f, Freno = 0f }
        };

        _mockVueltas.Setup(r => r.ObtenerTelemetriaVueltaAsync(vueltaBaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(telemetriaBase);
        _mockVueltas.Setup(r => r.ObtenerTelemetriaVueltaAsync(vueltaCompId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(telemetriaComp);

        var comparativa = await _servicio.CompararVueltasPorDistanciaAsync(vueltaBaseId, vueltaCompId);

        Assert.NotNull(comparativa);
        Assert.Equal(3, comparativa.PuntosComparados.Count);
        Assert.Equal(vueltaBaseId, comparativa.VueltaBaseId);
        Assert.Equal(vueltaCompId, comparativa.VueltaComparadaId);

        // En distancia 50m, la vuelta comparada está entre 40m (210 km/h) y 100m (230 km/h)
        var punto50m = comparativa.PuntosComparados[1];
        Assert.Equal(50f, punto50m.DistanciaPistaMetros);
        Assert.Equal(200f, punto50m.VelocidadBaseKmh);
        Assert.True(punto50m.VelocidadComparadaKmh > 210f && punto50m.VelocidadComparadaKmh < 230f);
        Assert.True(punto50m.DeltaVelocidadKmh > 0);
    }

    [Fact]
    public async Task CompararVueltasPorDistanciaAsync_ConUnaVueltaVacia_RetornaNull()
    {
        Guid vueltaBaseId = Guid.NewGuid();
        Guid vueltaCompId = Guid.NewGuid();

        _mockVueltas.Setup(r => r.ObtenerTelemetriaVueltaAsync(vueltaBaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MuestraTelemetria>());
        _mockVueltas.Setup(r => r.ObtenerTelemetriaVueltaAsync(vueltaCompId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MuestraTelemetria> { new() { DistanciaPistaMetros = 10f } });

        var comparativa = await _servicio.CompararVueltasPorDistanciaAsync(vueltaBaseId, vueltaCompId);

        Assert.Null(comparativa);
    }
}
