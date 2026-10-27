namespace Telemetria.Aplicacion.Tests;

using System;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Aplicacion.Mappings;
using Telemetria.Dominio.Modelos;
using Xunit;

public sealed class MapeadorTelemetriaTests
{
    [Fact]
    public void ConsolidarSectoresMs_SectoresParcialesCero_CalculaProporcionesExactasSinPerdidaDeMilisegundos()
    {
        int tiempoTotalMs = 81245;

        int[] consolidados = MapeadorTelemetria.ConsolidarSectoresMs(new[] { 0, 0, 0 }, tiempoTotalMs);

        Assert.Equal(3, consolidados.Length);
        Assert.True(consolidados[0] > 0);
        Assert.True(consolidados[1] > 0);
        Assert.True(consolidados[2] > 0);
        Assert.Equal(tiempoTotalMs, consolidados[0] + consolidados[1] + consolidados[2]);
    }

    [Fact]
    public void ConsolidarSectoresMs_SectoresRealesCompletos_PreservaValoresOriginales()
    {
        int[] sectoresReales = { 27100, 27400, 26745 };

        int[] consolidados = MapeadorTelemetria.ConsolidarSectoresMs(sectoresReales, 81245);

        Assert.Equal(sectoresReales, consolidados);
    }

    [Fact]
    public void MapearAPaqueteDto_CuadroDominioValido_TransformaTodosLosCanalesCorrectamente()
    {
        var cuadro = new CuadroTelemetria
        {
            MarcaTemporalUnixMs = 1700000000123L,
            VelocidadKmh = 312.4f,
            Rpms = 12450,
            MaxRpm = 13000,
            Marcha = 7,
            Acelerador = 1.0f,
            Freno = 0.0f,
            Direccion = -0.05f,
            DrsDisponible = true,
            DrsHabilitado = true,
            PresionNeumaticosPsi = new[] { 22.1f, 22.2f, 21.8f, 21.9f },
            TemperaturaNeumaticosC = new[] { 94.0f, 94.5f, 91.0f, 91.2f },
            DesgasteNeumaticos = new[] { 98.5f, 98.4f, 99.0f, 98.9f },
            TiempoVueltaActualMs = 42150,
            TiempoVueltaActualTexto = "00:42.150",
            MejorTiempoMs = 81240,
            MejorTiempoTexto = "01:21.240",
            DeltaMs = -125,
            VueltasCompletadas = 3,
            SectorActual = 2,
            ProgresoPistaNormalizado = 0.542f,
            FuerzaGLat = 1.85f,
            FuerzaGLon = 0.42f,
            FuerzaGVert = 0.05f,
            PosicionX = 120.5f,
            PosicionY = 12.0f,
            PosicionZ = -340.2f,
            Bandera = "GREEN",
            PosicionCarrera = 1,
            Circuito = "monza",
            ModeloAuto = "ks_ferrari_sf70h"
        };

        PaqueteTelemetriaDto dto = MapeadorTelemetria.MapearAPaqueteDto(cuadro);

        Assert.Equal("FRAME", dto.Tipo);
        Assert.Equal(1700000000123L, dto.MarcaTemporalUnixMs);
        Assert.Equal(312.4f, dto.Auto.VelocidadKmh);
        Assert.Equal(12450, dto.Auto.Rpms);
        Assert.Equal(7, dto.Auto.Marcha);
        Assert.True(dto.Auto.DrsHabilitado);
        Assert.Equal(22.1f, dto.Neumaticos.PresionPsi[0]);
        Assert.Equal(42150, dto.Tiempos.TiempoVueltaMs);
        Assert.Equal(-125, dto.Tiempos.DeltaMs);
        Assert.Equal(1.85f, dto.Dinamica.FuerzaG.Lat);
        Assert.Equal("monza", dto.Sesion.Circuito);
    }

    [Fact]
    public void MapearAEventoVueltaDto_VueltaCerrada_GeneraPayloadLapCompletedConMuestrasYGuid()
    {
        Guid vueltaId = Guid.NewGuid();
        var cuadro = new CuadroTelemetria
        {
            VueltasCompletadas = 4,
            UltimoTiempoVueltaMs = 80950,
            UltimoTiempoVueltaTexto = "01:20.950",
            EsMejorVueltaCerrada = true,
            EsVueltaValida = true,
            TiemposSectoresMs = new[] { 27800, 27300, 25850 }
        };

        EventoVueltaCompletadaDto evento = MapeadorTelemetria.MapearAEventoVueltaDto(cuadro, vueltaId, 4857);

        Assert.Equal("LAP_COMPLETED", evento.Tipo);
        Assert.Equal(4, evento.NumeroVuelta);
        Assert.Equal(80950, evento.TiempoVueltaMs);
        Assert.True(evento.EsMejorPersonal);
        Assert.Equal(4857, evento.CantidadMuestrasTelemetria);
        Assert.Equal(vueltaId.ToString(), evento.IdentificadorVueltaSesion);
    }
}
