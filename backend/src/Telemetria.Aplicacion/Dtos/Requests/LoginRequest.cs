namespace Telemetria.Aplicacion.Dtos.Requests;

using System.Text.Json.Serialization;

public sealed record LoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;
}
