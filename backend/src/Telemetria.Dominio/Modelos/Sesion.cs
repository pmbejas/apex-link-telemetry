namespace Telemetria.Dominio.Modelos;

/// <summary>
/// Representa una tanda o sesión de telemetría en el simulador o generador sintético.
/// </summary>
public class Sesion : EntidadBase
{
    public Guid? UsuarioId { get; set; }

    public string Circuito { get; set; } = string.Empty;

    public string ConfiguracionCircuito { get; set; } = string.Empty;

    public string ModeloVehiculo { get; set; } = string.Empty;

    public int TipoSesion { get; set; }

    public bool EsSimulacionMock { get; set; }

    public DateTimeOffset FechaInicio { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? FechaFin { get; set; }

    public int TotalVueltas { get; set; }

    public int MejorTiempoMs { get; set; }

    public bool SincronizadoConServidorCentral { get; set; } = false;

    public List<Vuelta> Vueltas { get; set; } = new();
}
