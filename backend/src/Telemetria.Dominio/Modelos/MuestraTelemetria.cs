namespace Telemetria.Dominio.Modelos;

/// <summary>
/// Representa un punto espacial y temporal de telemetría registrado a alta frecuencia (60 Hz).
/// </summary>
public class MuestraTelemetria : EntidadBase
{
    public Guid VueltaId { get; set; }

    public long MarcaTemporalUnixMs { get; set; }

    public float DistanciaPistaMetros { get; set; }

    public float VelocidadKmh { get; set; }

    public float Acelerador { get; set; }

    public float Freno { get; set; }

    public int Marcha { get; set; }

    public int Rpms { get; set; }

    public float FuerzaGLat { get; set; }

    public float FuerzaGLon { get; set; }

    public float PosicionX { get; set; }

    public float PosicionY { get; set; }

    public float PosicionZ { get; set; }
}
