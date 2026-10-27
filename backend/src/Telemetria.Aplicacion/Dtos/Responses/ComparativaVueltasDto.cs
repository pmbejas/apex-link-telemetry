namespace Telemetria.Aplicacion.Dtos.Responses;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Resultado del análisis comparativo entre una vuelta base y una vuelta de referencia.
/// </summary>
public sealed class ComparativaVueltasDto
{
    [JsonPropertyName("baseLapId")]
    public Guid VueltaBaseId { get; set; }

    [JsonPropertyName("comparedLapId")]
    public Guid VueltaComparadaId { get; set; }

    [JsonPropertyName("baseLapSamplesCount")]
    public int TotalMuestrasBase { get; set; }

    [JsonPropertyName("comparedLapSamplesCount")]
    public int TotalMuestrasComparada { get; set; }

    [JsonPropertyName("points")]
    public List<PuntoComparativaTelemetriaDto> PuntosComparados { get; set; } = new();
}
