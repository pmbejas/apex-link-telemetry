namespace Telemetria.Aplicacion.Tests;

using System;
using Telemetria.Aplicacion.Servicios;
using Xunit;

public sealed class CalculadorDeltaTrayectoriaTests
{
    [Fact]
    public void CalcularDeltaMs_SinVueltaReferenciaPrevia_RetornaCeroEnTodoElCircuito()
    {
        var calculador = new CalculadorDeltaTrayectoria();

        calculador.RegistrarMuestra(0.01f, 800);
        calculador.RegistrarMuestra(0.15f, 20000);
        calculador.RegistrarMuestra(0.50f, 45000);

        Assert.False(calculador.TienePerfilReferencia);
        Assert.Equal(0, calculador.CalcularDeltaMs(0.15f, 20000));
        Assert.Equal(0, calculador.CalcularDeltaMs(0.50f, 45000));
    }

    [Fact]
    public void CalcularDeltaMs_ConVelocidadNoUniformeEnChicanaYRecta_NoSufreSaltosLinealesYReflejaDiferenciaReal()
    {
        var calculador = new CalculadorDeltaTrayectoria();

        // Simular Vuelta 1 (80,000 ms total) con velocidad fuertemente variable:
        // Tramo 0.00 -> 0.15 (Chicana lenta): toma 20,000 ms (el 25% del tiempo en solo 15% de distancia)
        // Tramo 0.15 -> 0.50 (Recta rápida): toma 18,000 ms (llega a 38,000 ms al 50% de pista)
        // Tramo 0.50 -> 1.00 (Resto del circuito): llega a 80,000 ms
        for (int i = 1; i <= 150; i++)
        {
            float p = i / 1000.0f;
            int t = (int)Math.Round((i / 150.0) * 20000.0);
            calculador.RegistrarMuestra(p, t);
        }

        for (int i = 151; i <= 500; i++)
        {
            float p = i / 1000.0f;
            int t = 20000 + (int)Math.Round(((i - 150) / 350.0) * 18000.0);
            calculador.RegistrarMuestra(p, t);
        }

        for (int i = 501; i < 1000; i++)
        {
            float p = i / 1000.0f;
            int t = 38000 + (int)Math.Round(((i - 500) / 500.0) * 42000.0);
            calculador.RegistrarMuestra(p, t);
        }

        bool consolidado = calculador.ConsolidarCierreDeVuelta(80000, esVueltaValida: true);

        Assert.True(consolidado);
        Assert.True(calculador.TienePerfilReferencia);
        Assert.Equal(80000, calculador.MejorTiempoReferenciaMs);

        // En la chicana (progreso 0.15, tiempo 20,000 ms):
        // Con la antigua fórmula lineal daba +8,000 ms; con el mapa de trayectoria real debe dar 0 ms
        Assert.Equal(0, calculador.CalcularDeltaMs(0.15f, 20000));

        // Si en la Vuelta 2 pasa por la chicana 185 ms más rápido (19,815 ms):
        Assert.Equal(-185, calculador.CalcularDeltaMs(0.15f, 19815));

        // Si en la mitad del circuito (0.50, ref = 38,000 ms) viene 240 ms más lento (38,240 ms):
        Assert.Equal(240, calculador.CalcularDeltaMs(0.50f, 38240));
    }

    [Fact]
    public void RegistrarMuestra_ConDesfaseDeLineaDeMetaAssettoCorsa_IgnoraCuadroDesincronizado()
    {
        var calculador = new CalculadorDeltaTrayectoria();

        // Cuadro espurio al cruzar la línea donde el progreso ya es 0.001 pero el reloj aún marca 81,200 ms
        calculador.RegistrarMuestra(0.001f, 81200);

        // Luego continúa la vuelta normal desde 20 ms
        for (int i = 2; i < 995; i += 10)
        {
            float p = i / 1000.0f;
            int t = (int)(p * 80000);
            calculador.RegistrarMuestra(p, t);
        }

        bool consolidado = calculador.ConsolidarCierreDeVuelta(80000, esVueltaValida: true);
        Assert.True(consolidado);

        // Al consultar el delta en el inicio de la siguiente vuelta con un cuadro desfasado (p=0.002, t=80000), debe dar 0 y no +79,840 ms
        Assert.Equal(0, calculador.CalcularDeltaMs(0.002f, 80000));
    }
}
