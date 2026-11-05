namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// DTO para la tabla de clasificación y récords de vuelta por circuito.
/// </summary>
public sealed record LeaderboardItemDto
{
    [JsonPropertyName("posicion")]
    public int Posicion { get; init; }

    [JsonPropertyName("piloto")]
    public string Piloto { get; init; } = string.Empty;

    [JsonPropertyName("alias")]
    public string Alias { get; init; } = string.Empty;

    [JsonPropertyName("tiempo")]
    public string Tiempo { get; init; } = string.Empty;

    [JsonPropertyName("tiempoMs")]
    public int TiempoMs { get; init; }

    [JsonPropertyName("delta")]
    public string Delta { get; init; } = string.Empty;

    [JsonPropertyName("auto")]
    public string Auto { get; init; } = string.Empty;

    [JsonPropertyName("circuito")]
    public string Circuito { get; init; } = string.Empty;

    [JsonPropertyName("vueltas")]
    public int Vueltas { get; init; }

    [JsonPropertyName("esRecord")]
    public bool EsRecord { get; init; }
}
