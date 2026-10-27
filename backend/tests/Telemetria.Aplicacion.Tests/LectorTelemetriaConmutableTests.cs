namespace Telemetria.Aplicacion.Tests;

using Moq;
using Telemetria.Aplicacion.Servicios;
using Telemetria.Dominio.Contratos.Hardware;
using Telemetria.Dominio.Modelos;
using Xunit;

public sealed class LectorTelemetriaConmutableTests
{
    [Fact]
    public void ConmutarModoMock_CambioDeRealAMock_IncrementaVersionYDelegaAlProveedorMock()
    {
        var mockLectorReal = new Mock<ILectorTelemetria>();
        var mockLectorSintetico = new Mock<ILectorTelemetria>();

        mockLectorReal.Setup(lector => lector.ObtenerCuadroActual())
            .Returns(new CuadroTelemetria { EsSimulacionMock = false, VelocidadKmh = 100.0f });
        mockLectorSintetico.Setup(lector => lector.ObtenerCuadroActual())
            .Returns(new CuadroTelemetria { EsSimulacionMock = true, VelocidadKmh = 250.0f });

        using var conmutador = new LectorTelemetriaConmutable(
            mockLectorReal.Object,
            mockLectorSintetico.Object,
            usarMockInicial: false);

        Assert.False(conmutador.EstaEnModoMock);
        Assert.Equal(0L, conmutador.VersionConmutacion);
        Assert.Equal(100.0f, conmutador.ObtenerCuadroActual().VelocidadKmh);

        conmutador.ConmutarModoMock(true);

        Assert.True(conmutador.EstaEnModoMock);
        Assert.Equal(1L, conmutador.VersionConmutacion);
        Assert.Equal(250.0f, conmutador.ObtenerCuadroActual().VelocidadKmh);
    }

    [Fact]
    public void ConmutarModoMock_MismoEstadoActivo_NoIncrementaVersionConmutacion()
    {
        var mockLectorReal = new Mock<ILectorTelemetria>();
        var mockLectorSintetico = new Mock<ILectorTelemetria>();

        using var conmutador = new LectorTelemetriaConmutable(
            mockLectorReal.Object,
            mockLectorSintetico.Object,
            usarMockInicial: true);

        conmutador.ConmutarModoMock(true);

        Assert.True(conmutador.EstaEnModoMock);
        Assert.Equal(0L, conmutador.VersionConmutacion);
    }
}
