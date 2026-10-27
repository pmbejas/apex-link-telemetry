namespace Telemetria.Aplicacion.Dtos.Responses;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Colección densa de telemetría de una vuelta completa ordenada espacialmente por distancia.
/// </summary>
public sealed class SerieTelemetriaVueltaDto
{
    [JsonPropertyName("lapId")]
    public Guid VueltaId { get; set; }

    [JsonPropertyName("samplesCount")]
    public int TotalMuestras => Muestras.Count;

    [JsonPropertyName("samples")]
    public List<MuestraTelemetriaDto> Muestras { get; set; } = new();
}
