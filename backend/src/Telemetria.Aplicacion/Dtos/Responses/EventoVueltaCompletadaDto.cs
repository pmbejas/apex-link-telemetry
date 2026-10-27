namespace Telemetria.Aplicacion.Dtos.Responses;

using System;
using System.Text.Json.Serialization;

public sealed class EventoVueltaCompletadaDto
{
    [JsonPropertyName("type")]
    public string Tipo { get; set; } = "LAP_COMPLETED";

    [JsonPropertyName("lapNumber")]
    public int NumeroVuelta { get; set; }

    [JsonPropertyName("lapTimeMs")]
    public int TiempoVueltaMs { get; set; }

    [JsonPropertyName("lapTimeStr")]
    public string TiempoVueltaTexto { get; set; } = string.Empty;

    [JsonPropertyName("isPersonalBest")]
    public bool EsMejorPersonal { get; set; }

    [JsonPropertyName("isValidLap")]
    public bool EsVueltaValida { get; set; } = true;

    [JsonPropertyName("sectors")]
    public int[] SectoresMs { get; set; } = Array.Empty<int>();

    [JsonPropertyName("telemetrySamplesCount")]
    public int CantidadMuestrasTelemetria { get; set; }

    [JsonPropertyName("sessionLapId")]
    public string IdentificadorVueltaSesion { get; set; } = string.Empty;
}
