namespace Telemetria.Aplicacion.Dtos.Responses;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Representa el detalle completo de una vuelta para consumo de controladores y clientes REST.
/// </summary>
public sealed class VueltaDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("lapNumber")]
    public int NumeroVuelta { get; set; }

    [JsonPropertyName("lapTimeMs")]
    public int TiempoVueltaMs { get; set; }

    [JsonPropertyName("lapTimeStr")]
    public string TiempoVueltaTexto { get; set; } = string.Empty;

    [JsonPropertyName("sector1Ms")]
    public int Sector1Ms { get; set; }

    [JsonPropertyName("sector2Ms")]
    public int Sector2Ms { get; set; }

    [JsonPropertyName("sector3Ms")]
    public int Sector3Ms { get; set; }

    [JsonPropertyName("isValidLap")]
    public bool EsValida { get; set; }

    [JsonPropertyName("isBestLap")]
    public bool EsMejorVuelta { get; set; }
}
