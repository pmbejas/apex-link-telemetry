namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Información de estado y conexión del servicio de telemetría y simulador.
/// </summary>
public class EstadoServicioDto
{
    [JsonPropertyName("isConnected")]
    public bool EstaConectado { get; set; }

    [JsonPropertyName("isMockMode")]
    public bool ModoMock { get; set; }

    [JsonPropertyName("circuit")]
    public string Circuito { get; set; } = string.Empty;

    [JsonPropertyName("carModel")]
    public string ModeloVehiculo { get; set; } = string.Empty;

    [JsonPropertyName("trackLengthMeters")]
    public float LongitudPistaMetros { get; set; }

    [JsonPropertyName("activeCars")]
    public int AutosActivos { get; set; }
}
