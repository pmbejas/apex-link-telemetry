namespace Telemetria.Aplicacion.Dtos.Responses;

using System;
using System.Text.Json.Serialization;

public sealed record LoginRespuestaDto
{
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;

    [JsonPropertyName("expiraEn")]
    public string ExpiraEn { get; init; } = string.Empty;

    [JsonPropertyName("usuario")]
    public PerfilPilotoDetalleDto Usuario { get; init; } = new();
}
