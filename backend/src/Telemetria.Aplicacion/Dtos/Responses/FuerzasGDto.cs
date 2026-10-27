namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

public sealed class FuerzasGDto
{
    [JsonPropertyName("lat")]
    public float Lat { get; set; }

    [JsonPropertyName("lon")]
    public float Lon { get; set; }

    [JsonPropertyName("vert")]
    public float Vert { get; set; }
}
