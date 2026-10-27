namespace Telemetria.Aplicacion.Dtos.Responses;

using System;
using System.Text.Json.Serialization;

public sealed class DatosNeumaticosDto
{
    [JsonPropertyName("pressurePsi")]
    public float[] PresionPsi { get; set; } = Array.Empty<float>();

    [JsonPropertyName("tempCoreC")]
    public float[] TemperaturaInternaC { get; set; } = Array.Empty<float>();

    [JsonPropertyName("wear")]
    public float[] DesgastePorcentaje { get; set; } = Array.Empty<float>();
}
