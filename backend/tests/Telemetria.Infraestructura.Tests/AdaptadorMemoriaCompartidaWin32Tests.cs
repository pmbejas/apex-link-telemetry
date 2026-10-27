namespace Telemetria.Infraestructura.Tests;

using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Telemetria.Dominio.Modelos;
using Telemetria.Infraestructura.MemoriaCompartida;
using Telemetria.Infraestructura.MemoriaCompartida.Estructuras;
using Xunit;

public sealed class AdaptadorMemoriaCompartidaWin32Tests
{
    [Fact]
    public unsafe void EstructurasMemoriaCompartida_AlineacionPack4_TienenTamanosYOffsetsBinariosExactos()
    {
        Assert.Equal(0, (int)Marshal.OffsetOf<EstructuraFisicaAC>(nameof(EstructuraFisicaAC.PacketId)));
        Assert.Equal(4, (int)Marshal.OffsetOf<EstructuraFisicaAC>(nameof(EstructuraFisicaAC.Gas)));
        Assert.Equal(28, (int)Marshal.OffsetOf<EstructuraFisicaAC>(nameof(EstructuraFisicaAC.SpeedKmh)));
        Assert.Equal(568, (int)Marshal.OffsetOf<EstructuraFisicaAC>(nameof(EstructuraFisicaAC.LocalVelocity)));
        Assert.Equal(580, sizeof(EstructuraFisicaAC));

        Assert.Equal(0, (int)Marshal.OffsetOf<EstructuraGraficaAC>(nameof(EstructuraGraficaAC.PacketId)));
        Assert.Equal(4, (int)Marshal.OffsetOf<EstructuraGraficaAC>(nameof(EstructuraGraficaAC.Status)));
        Assert.Equal(8, (int)Marshal.OffsetOf<EstructuraGraficaAC>(nameof(EstructuraGraficaAC.Session)));
        Assert.Equal(12, (int)Marshal.OffsetOf<EstructuraGraficaAC>(nameof(EstructuraGraficaAC.CurrentTime)));
        Assert.Equal(132, (int)Marshal.OffsetOf<EstructuraGraficaAC>(nameof(EstructuraGraficaAC.CompletedLaps)));
        Assert.Equal(1256, sizeof(EstructuraGraficaAC));

        Assert.Equal(0, (int)Marshal.OffsetOf<EstructuraEstaticaAC>(nameof(EstructuraEstaticaAC.SmVersion)));
        Assert.Equal(60, (int)Marshal.OffsetOf<EstructuraEstaticaAC>(nameof(EstructuraEstaticaAC.NumberOfSessions)));
        Assert.Equal(688, sizeof(EstructuraEstaticaAC));
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(8, 7)]
    public void NormalizarMarcha_ValorCrudoAssettoCorsa_RetornaMarchaEstandarizada(int marchaCruda, int marchaEsperada)
    {
        int resultado = UtilitariosInteroperabilidadAC.NormalizarMarcha(marchaCruda);
        Assert.Equal(marchaEsperada, resultado);
    }

    [Theory]
    [InlineData(0, "GREEN")]
    [InlineData(1, "BLUE")]
    [InlineData(2, "YELLOW")]
    [InlineData(5, "CHECKERED")]
    public void MapearBanderaTexto_CodigoBanderaKunos_RetornaTextoProtocolo(int codigoBandera, string textoEsperado)
    {
        string resultado = UtilitariosInteroperabilidadAC.MapearBanderaTexto(codigoBandera);
        Assert.Equal(textoEsperado, resultado);
    }

    [Fact]
    public unsafe void LeerCadenaFija_BufferConTerminadorNuloYBasuraResidual_TruncaEnPrimerTerminador()
    {
        char* buffer = stackalloc char[15];
        buffer[0] = 'm';
        buffer[1] = 'o';
        buffer[2] = 'n';
        buffer[3] = 'z';
        buffer[4] = 'a';
        buffer[5] = '\0';
        buffer[6] = 'X';
        buffer[7] = 'Y';

        string resultado = UtilitariosInteroperabilidadAC.LeerCadenaFija(buffer, 15);

        Assert.Equal("monza", resultado);
    }

    [Fact]
    public void IntentarConectar_SimuladorCerradoSinBloquesMmf_RetornaFalsoSinExcepcionNoControlada()
    {
        using var lector = new LectorAssettoCorsaWin32(NullLogger<LectorAssettoCorsaWin32>.Instance);

        bool conectado = lector.IntentarConectar();
        CuadroTelemetria ultimoCuadro = lector.ObtenerUltimoCuadroConocido();
        MetadatosEstaticos metadatos = lector.ObtenerMetadatosEstaticos();

        Assert.False(conectado);
        Assert.False(lector.EstaConectado());
        Assert.Equal(0, ultimoCuadro.EstadoSimulador);
        Assert.Equal(string.Empty, metadatos.NombrePista);
        Assert.Throws<InvalidOperationException>(() => lector.ObtenerCuadroActual());
    }
}
