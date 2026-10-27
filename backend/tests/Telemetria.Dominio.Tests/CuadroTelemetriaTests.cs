namespace Telemetria.Dominio.Tests;

using Telemetria.Dominio.Modelos;
using Xunit;

public sealed class CuadroTelemetriaTests
{
    [Fact]
    public void CuadroTelemetria_CreacionConValoresPorDefecto_MantieneInvariantes()
    {
        // Act
        var cuadro = new CuadroTelemetria();

        // Assert
        Assert.Equal(0, cuadro.VelocidadKmh);
        Assert.Equal(0, cuadro.Acelerador);
        Assert.Equal(0, cuadro.Freno);
        Assert.Equal(0, cuadro.Marcha);
        Assert.Equal(0, cuadro.Rpms);
        Assert.Equal(0, cuadro.TiempoVueltaActualMs);
        Assert.Equal(1, cuadro.SectorActual);
        Assert.Equal(0, cuadro.VueltasCompletadas);
    }

    [Fact]
    public void CuadroTelemetria_ExpresionWith_CreaNuevaInstanciaInmutableSinAlterarOriginal()
    {
        // Arrange
        var original = new CuadroTelemetria
        {
            VelocidadKmh = 120.5f,
            Acelerador = 0.85f,
            Marcha = 3,
            Rpms = 6200
        };

        // Act: mutación inmutable mediante expresión 'with'
        var mutado = original with
        {
            VelocidadKmh = 180.0f,
            Marcha = 4,
            Rpms = 7100
        };

        // Assert
        Assert.NotSame(original, mutado);
        Assert.Equal(120.5f, original.VelocidadKmh);
        Assert.Equal(3, original.Marcha);
        Assert.Equal(180.0f, mutado.VelocidadKmh);
        Assert.Equal(4, mutado.Marcha);
        Assert.Equal(0.85f, mutado.Acelerador);
    }

    [Fact]
    public void CuadroTelemetria_IgualdadPorValor_DosInstanciasConMismosCamposSonIguales()
    {
        // Arrange
        var cuadro1 = new CuadroTelemetria
        {
            VelocidadKmh = 200.0f,
            Acelerador = 1.0f,
            Marcha = 5,
            DistanciaMetros = 1500.0f
        };

        var cuadro2 = new CuadroTelemetria
        {
            VelocidadKmh = 200.0f,
            Acelerador = 1.0f,
            Marcha = 5,
            DistanciaMetros = 1500.0f
        };

        // Assert
        Assert.Equal(cuadro1, cuadro2);
        Assert.True(cuadro1 == cuadro2);
    }
}
