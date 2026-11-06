namespace Telemetria.Aplicacion.Dtos.Requests;

using System.Text.Json.Serialization;

public sealed record SolicitarRecuperoRequest
{
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
}

public sealed record ConfirmarRecuperoRequest
{
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("codigo")]
    public string Codigo { get; init; } = string.Empty;

    [JsonPropertyName("nuevaPassword")]
    public string NuevaPassword { get; init; } = string.Empty;
}
