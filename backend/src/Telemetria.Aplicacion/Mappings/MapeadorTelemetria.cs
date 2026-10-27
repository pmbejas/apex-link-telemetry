namespace Telemetria.Aplicacion.Mappings;

using System;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Dominio.Modelos;

public static class MapeadorTelemetria
{
    private const double PROPORCION_SECTOR_1_POR_DEFECTO = 0.346;
    private const double PROPORCION_SECTOR_2_POR_DEFECTO = 0.338;

    public static int[] ConsolidarSectoresMs(int[]? sectoresReales, int tiempoVueltaMs)
    {
        int s1Real = (sectoresReales is { Length: >= 1 }) ? sectoresReales[0] : 0;
        int s2Real = (sectoresReales is { Length: >= 2 }) ? sectoresReales[1] : 0;
        int s3Real = (sectoresReales is { Length: >= 3 }) ? sectoresReales[2] : 0;

        int sector1Ms = s1Real > 0 ? s1Real : (int)Math.Round(tiempoVueltaMs * PROPORCION_SECTOR_1_POR_DEFECTO);
        int sector2Ms = s2Real > 0 ? s2Real : (int)Math.Round(tiempoVueltaMs * PROPORCION_SECTOR_2_POR_DEFECTO);
        int sector3Ms = s3Real > 0 ? s3Real : Math.Max(0, tiempoVueltaMs - sector1Ms - sector2Ms);

        return new int[] { sector1Ms, sector2Ms, sector3Ms };
    }

    public static PaqueteTelemetriaDto MapearAPaqueteDto(CuadroTelemetria cuadro)
    {
        return new PaqueteTelemetriaDto
        {
            Tipo = "FRAME",
            MarcaTemporalUnixMs = cuadro.MarcaTemporalUnixMs,
            Auto = new DatosAutoDto
            {
                VelocidadKmh = cuadro.VelocidadKmh,
                Rpms = cuadro.Rpms,
                MaxRpm = cuadro.MaxRpm,
                Marcha = cuadro.Marcha,
                Acelerador = cuadro.Acelerador,
                Freno = cuadro.Freno,
                Direccion = cuadro.Direccion,
                DrsDisponible = cuadro.DrsDisponible,
                DrsHabilitado = cuadro.DrsHabilitado
            },
            Neumaticos = new DatosNeumaticosDto
            {
                PresionPsi = cuadro.PresionNeumaticosPsi,
                TemperaturaInternaC = cuadro.TemperaturaNeumaticosC,
                DesgastePorcentaje = cuadro.DesgasteNeumaticos
            },
            Tiempos = new DatosTiemposDto
            {
                TiempoVueltaMs = cuadro.TiempoVueltaActualMs,
                TiempoVueltaTexto = cuadro.TiempoVueltaActualTexto,
                MejorTiempoMs = cuadro.MejorTiempoMs,
                MejorTiempoTexto = cuadro.MejorTiempoTexto,
                DeltaMs = cuadro.DeltaMs,
                VueltaActual = cuadro.VueltasCompletadas,
                Sector = cuadro.SectorActual,
                ProgresoVuelta = cuadro.ProgresoPistaNormalizado
            },
            Dinamica = new DatosDinamicaDto
            {
                FuerzaG = new FuerzasGDto { Lat = cuadro.FuerzaGLat, Lon = cuadro.FuerzaGLon, Vert = cuadro.FuerzaGVert },
                PosicionMundo = new PosicionMundoDto { X = cuadro.PosicionX, Y = cuadro.PosicionY, Z = cuadro.PosicionZ }
            },
            Sesion = new DatosSesionDto
            {
                Bandera = cuadro.Bandera,
                Posicion = cuadro.PosicionCarrera,
                Circuito = cuadro.Circuito,
                ModeloAuto = cuadro.ModeloAuto
            }
        };
    }

    public static EventoVueltaCompletadaDto MapearAEventoVueltaDto(
        CuadroTelemetria cuadro,
        Guid vueltaId,
        int cantidadMuestras)
    {
        return new EventoVueltaCompletadaDto
        {
            Tipo = "LAP_COMPLETED",
            NumeroVuelta = cuadro.VueltasCompletadas,
            TiempoVueltaMs = cuadro.UltimoTiempoVueltaMs,
            TiempoVueltaTexto = cuadro.UltimoTiempoVueltaTexto,
            EsMejorPersonal = cuadro.EsMejorVueltaCerrada,
            EsVueltaValida = cuadro.EsVueltaValida,
            SectoresMs = cuadro.TiemposSectoresMs,
            CantidadMuestrasTelemetria = cantidadMuestras,
            IdentificadorVueltaSesion = vueltaId.ToString()
        };
    }
}
