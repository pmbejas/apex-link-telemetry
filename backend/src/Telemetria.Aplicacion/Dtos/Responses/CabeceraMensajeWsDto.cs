namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

public sealed class CabeceraMensajeWsDto
{
    [JsonPropertyName("type")]
    public string Tipo { get; set; } = string.Empty;
}
