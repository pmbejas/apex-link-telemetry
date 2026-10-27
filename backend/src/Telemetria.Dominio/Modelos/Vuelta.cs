namespace Telemetria.Dominio.Modelos;

/// <summary>
/// Representa un giro o vuelta completada dentro de una sesión de telemetría.
/// </summary>
public class Vuelta : EntidadBase
{
    public Guid SesionId { get; set; }

    public Sesion? Sesion { get; set; }

    public int NumeroVuelta { get; set; }

    public int TiempoVueltaMs { get; set; }

    public string TiempoVueltaTexto { get; set; } = string.Empty;

    public int Sector1Ms { get; set; }

    public int Sector2Ms { get; set; }

    public int Sector3Ms { get; set; }

    public bool EsValida { get; set; } = true;

    public bool EsMejorVuelta { get; set; } = false;

    public List<MuestraTelemetria> Muestras { get; set; } = new();
}
