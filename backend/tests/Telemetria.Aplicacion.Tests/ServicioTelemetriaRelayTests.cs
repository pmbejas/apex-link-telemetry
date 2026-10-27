namespace Telemetria.Aplicacion.Tests;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Aplicacion.Servicios;
using Telemetria.Dominio.Contratos.Hardware;
using Telemetria.Dominio.Modelos;
using Xunit;

public sealed class ServicioTelemetriaRelayTests
{
    [Fact]
    public async Task ExecuteAsync_SinClientesConectados_EnviaCuadroAlCanalPersistenciaSinInvocarDifusor()
    {
        var mockLector = new Mock<ILectorTelemetria>();
        mockLector.Setup(lector => lector.EstaConectado()).Returns(true);
        mockLector.Setup(lector => lector.ObtenerCuadroActual()).Returns(new CuadroTelemetria
        {
            EstadoSimulador = 2,
            Circuito = "monza",
            ModeloAuto = "ks_ferrari_sf70h",
            VelocidadKmh = 280.0f
        });

        var mockDifusor = new Mock<IDifusorTelemetria>();
        mockDifusor.Setup(difusor => difusor.TieneClientesConectados).Returns(false);

        var canalPersistencia = Channel.CreateUnbounded<CuadroTelemetria>();
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telemetria:FrecuenciaPistaMs"] = "5.0"
            })
            .Build();

        using var relay = new ServicioTelemetriaRelay(
            mockLector.Object,
            mockDifusor.Object,
            configuracion,
            NullLogger<ServicioTelemetriaRelay>.Instance,
            canalPersistencia: canalPersistencia.Writer);

        using var cts = new CancellationTokenSource();
        await relay.StartAsync(cts.Token);

        CuadroTelemetria cuadroRecibido = await canalPersistencia.Reader.ReadAsync();
        await cts.CancelAsync();
        await relay.StopAsync(CancellationToken.None);

        Assert.Equal(280.0f, cuadroRecibido.VelocidadKmh);
        mockDifusor.Verify(
            difusor => difusor.DifundirTelemetriaAsync(It.IsAny<PaqueteTelemetriaDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CruceDeMetaEnModoAislado_EmiteEventoLapCompletedConSectoresConsolidados()
    {
        int llamada = 0;
        var mockLector = new Mock<ILectorTelemetria>();
        mockLector.Setup(lector => lector.EstaConectado()).Returns(true);
        mockLector.Setup(lector => lector.ObtenerCuadroActual()).Returns(() =>
        {
            int indice = Interlocked.Increment(ref llamada);
            return indice == 1
                ? new CuadroTelemetria
                {
                    EstadoSimulador = 2,
                    Circuito = "monza",
                    ModeloAuto = "ks_ferrari_sf70h",
                    VueltasCompletadas = 0
                }
                : new CuadroTelemetria
                {
                    EstadoSimulador = 2,
                    Circuito = "monza",
                    ModeloAuto = "ks_ferrari_sf70h",
                    VueltasCompletadas = 1,
                    UltimoTiempoVueltaMs = 81200,
                    UltimoTiempoVueltaTexto = "01:21.200",
                    EsVueltaValida = true,
                    TiemposSectoresMs = new[] { 27000, 0, 0 }
                };
        });

        var eventoCapturadoTcs = new TaskCompletionSource<EventoVueltaCompletadaDto>();
        var mockDifusor = new Mock<IDifusorTelemetria>();
        mockDifusor.Setup(difusor => difusor.TieneClientesConectados).Returns(true);
        mockDifusor.Setup(difusor => difusor.EmitirEventoVueltaAsync(
                It.IsAny<EventoVueltaCompletadaDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventoVueltaCompletadaDto, CancellationToken>((evento, _) =>
                eventoCapturadoTcs.TrySetResult(evento))
            .Returns(ValueTask.CompletedTask);

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telemetria:FrecuenciaPistaMs"] = "5.0"
            })
            .Build();

        using var relay = new ServicioTelemetriaRelay(
            mockLector.Object,
            mockDifusor.Object,
            configuracion,
            NullLogger<ServicioTelemetriaRelay>.Instance,
            canalPersistencia: null);

        using var cts = new CancellationTokenSource(1000);
        await relay.StartAsync(cts.Token);

        EventoVueltaCompletadaDto evento = await eventoCapturadoTcs.Task.WaitAsync(cts.Token);
        await cts.CancelAsync();
        await relay.StopAsync(CancellationToken.None);

        Assert.Equal("LAP_COMPLETED", evento.Tipo);
        Assert.Equal(1, evento.NumeroVuelta);
        Assert.Equal(81200, evento.TiempoVueltaMs);
        Assert.True(evento.EsMejorPersonal);
        Assert.Equal(81200, evento.SectoresMs[0] + evento.SectoresMs[1] + evento.SectoresMs[2]);
    }
}
