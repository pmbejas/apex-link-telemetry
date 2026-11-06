namespace Telemetria.Aplicacion.Dtos.Requests;

using System;
using System.Text.Json.Serialization;

public sealed record DeclararPilotoActivoRequest
{
    [JsonPropertyName("usuarioId")]
    public Guid UsuarioId { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("alias")]
    public string Alias { get; init; } = string.Empty;
}
