namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Respuesta tras conmutar el modo mock o hardware del simulador.
/// </summary>
public class EstadoMockDto
{
    [JsonPropertyName("isMockActive")]
    public bool ModoMockActivo { get; set; }

    [JsonPropertyName("message")]
    public string Mensaje { get; set; } = string.Empty;
}
