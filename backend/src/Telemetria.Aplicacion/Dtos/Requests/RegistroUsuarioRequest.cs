namespace Telemetria.Aplicacion.Dtos.Requests;

using System.Text.Json.Serialization;

public sealed record RegistroUsuarioRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("alias")]
    public string Alias { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;

    [JsonPropertyName("nacionalidad")]
    public string Nacionalidad { get; init; } = "Argentina";

    [JsonPropertyName("codigoPais")]
    public string CodigoPais { get; init; } = "ARG";
}
