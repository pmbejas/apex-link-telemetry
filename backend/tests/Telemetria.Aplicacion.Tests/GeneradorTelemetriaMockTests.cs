namespace Telemetria.Aplicacion.Tests;

using System.Threading.Tasks;
using Telemetria.Aplicacion.Servicios;
using Telemetria.Dominio.Modelos;
using Xunit;

public sealed class GeneradorTelemetriaMockTests
{
    [Fact]
    public void ObtenerMetadatosEstaticos_InvocacionInicial_RetornaDatosFerrariEnMonza()
    {
        using var generador = new GeneradorTelemetriaMock();

        MetadatosEstaticos metadatos = generador.ObtenerMetadatosEstaticos();

        Assert.Equal("ks_ferrari_sf70h", metadatos.NombreAuto);
        Assert.Equal("monza", metadatos.NombrePista);
        Assert.Equal("grand_prix", metadatos.ConfiguracionPista);
        Assert.Equal(13000, metadatos.MaxRpm);
        Assert.Equal(3, metadatos.CantidadSectores);
        Assert.Equal(5793.0f, metadatos.LongitudPistaMetros);
    }

    [Fact]
    public void ObtenerCuadroActual_SimulacionEnCurso_RetornaParametrosFisicosEnRangosValidos()
    {
        using var generador = new GeneradorTelemetriaMock();

        CuadroTelemetria cuadro = generador.ObtenerCuadroActual();

        Assert.True(generador.EstaConectado());
        Assert.True(cuadro.EsSimulacionMock);
        Assert.Equal(2, cuadro.EstadoSimulador);
        Assert.InRange(cuadro.VelocidadKmh, 70.0f, 330.0f);
        Assert.InRange(cuadro.Rpms, 4200, 13000);
        Assert.InRange(cuadro.Marcha, 1, 7);
        Assert.InRange(cuadro.Acelerador, 0.0f, 1.0f);
        Assert.InRange(cuadro.Freno, 0.0f, 1.0f);
        Assert.InRange(cuadro.Direccion, -1.0f, 1.0f);
        Assert.Equal(4, cuadro.PresionNeumaticosPsi.Length);
        Assert.Equal(4, cuadro.TemperaturaNeumaticosC.Length);
        Assert.Equal(4, cuadro.DesgasteNeumaticos.Length);
    }

    [Fact]
    public void ObtenerCuadroActual_AvanceContinuo_CompletaVueltaEnMonzaYRegistraTresSectores()
    {
        using var generador = new GeneradorTelemetriaMock();
        CuadroTelemetria? cuadroCierre = null;

        // Una vuelta en Monza (~82s a 60Hz) toma ~4950 cuadros
        for (int iteracion = 0; iteracion < 6500; iteracion++)
        {
            CuadroTelemetria cuadro = generador.ObtenerCuadroActual();
            if (cuadro.VueltasCompletadas == 1)
            {
                cuadroCierre = cuadro;
                break;
            }
        }

        Assert.NotNull(cuadroCierre);
        Assert.Equal(1, cuadroCierre!.VueltasCompletadas);
        Assert.InRange(cuadroCierre.UltimoTiempoVueltaMs, 75000, 95000);
        Assert.Equal(3, cuadroCierre.TiemposSectoresMs.Length);
        Assert.True(cuadroCierre.TiemposSectoresMs[0] > 0);
        Assert.True(cuadroCierre.TiemposSectoresMs[1] > 0);
        Assert.True(cuadroCierre.TiemposSectoresMs[2] > 0);
    }

    [Fact]
    public async Task ObtenerCuadroActual_AccesoConcurrenteMultiHilo_MantieneConsistenciaDeEstado()
    {
        using var generador = new GeneradorTelemetriaMock();
        var tareas = new Task[8];

        for (int indice = 0; indice < tareas.Length; indice++)
        {
            tareas[indice] = Task.Run(() =>
            {
                for (int paso = 0; paso < 150; paso++)
                {
                    CuadroTelemetria cuadro = generador.ObtenerCuadroActual();
                    Assert.InRange(cuadro.ProgresoPistaNormalizado, 0.0f, 1.0f);
                }
            });
        }

        await Task.WhenAll(tareas);
        CuadroTelemetria ultimo = generador.ObtenerUltimoCuadroConocido();
        Assert.True(ultimo.VelocidadKmh > 0);
    }
}
