namespace Telemetria.Dominio.Modelos;

public sealed record class CuadroTelemetria
{
    // Marcas temporales y estado de sesión/control
    public long MarcaTemporalUnixMs { get; init; }
    public int EstadoSimulador { get; init; } = 2; // 0=Off, 1=Replay, 2=Live, 3=Pause
    public int TipoSesion { get; init; } = 0;      // 0=Practice, 1=Qualify, 2=Race
    public bool EsSimulacionMock { get; init; }
    public bool EstaEnBoxes { get; init; }
    public bool EsVueltaValida { get; init; } = true;

    // Cinemática y controles del monoplaza
    public float VelocidadKmh { get; init; }
    public int Rpms { get; init; }
    public int MaxRpm { get; init; }
    public int Marcha { get; init; }
    public float Acelerador { get; init; }
    public float Freno { get; init; }
    public float Direccion { get; init; }
    public bool DrsDisponible { get; init; }
    public bool DrsHabilitado { get; init; }

    // Dinámica de neumáticos (4 ruedas: 0=FL, 1=FR, 2=RL, 3=RR)
    public float[] PresionNeumaticosPsi { get; init; } = Array.Empty<float>();
    public float[] TemperaturaNeumaticosC { get; init; } = Array.Empty<float>();
    public float[] DesgasteNeumaticos { get; init; } = Array.Empty<float>();

    // Cronometraje, sectores (base 1..3) y correlación con persistencia
    public int TiempoVueltaActualMs { get; init; }
    public string TiempoVueltaActualTexto { get; init; } = string.Empty;
    public int UltimoTiempoVueltaMs { get; init; }
    public string UltimoTiempoVueltaTexto { get; init; } = string.Empty;
    public int MejorTiempoMs { get; init; }
    public string MejorTiempoTexto { get; init; } = string.Empty;
    public int DeltaMs { get; init; }
    public int VueltasCompletadas { get; init; }
    public int SectorActual { get; init; } = 1;
    public int UltimoTiempoSectorMs { get; init; }
    public int[] TiemposSectoresMs { get; init; } = Array.Empty<int>();
    public Guid? VueltaCerradaId { get; init; }
    public int MuestrasVueltaCerrada { get; init; }
    public bool EsMejorVueltaCerrada { get; init; }

    // Posición espacial (m), aceleraciones (G) y contexto de carrera
    public float ProgresoPistaNormalizado { get; init; }
    public float DistanciaMetros { get; init; }
    public float FuerzaGLat { get; init; }
    public float FuerzaGLon { get; init; }
    public float FuerzaGVert { get; init; }
    public float PosicionX { get; init; }
    public float PosicionY { get; init; }
    public float PosicionZ { get; init; }
    public string Bandera { get; init; } = "GREEN";
    public int PosicionCarrera { get; init; } = 1;
    public int CantidadAutosActivos { get; init; } = 1;
    public string Circuito { get; init; } = string.Empty;
    public string ConfiguracionCircuito { get; init; } = string.Empty;
    public string ModeloAuto { get; init; } = string.Empty;
}
