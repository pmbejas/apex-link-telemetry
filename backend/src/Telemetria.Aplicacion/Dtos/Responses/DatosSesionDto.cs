namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

public sealed class DatosSesionDto
{
    [JsonPropertyName("flag")]
    public string Bandera { get; set; } = "GREEN";

    [JsonPropertyName("position")]
    public int Posicion { get; set; } = 1;

    [JsonPropertyName("track")]
    public string Circuito { get; set; } = string.Empty;

    [JsonPropertyName("carModel")]
    public string ModeloAuto { get; set; } = string.Empty;
}
