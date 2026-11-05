namespace Telemetria.Aplicacion.Dtos.Requests;

using System.Text.Json.Serialization;

/// <summary>
/// Petición para actualización directa del Apex Rating y parámetros del piloto.
/// </summary>
public sealed record ActualizarRatingRequest
{
    [JsonPropertyName("paceRating")]
    public int PaceRating { get; init; }

    [JsonPropertyName("safetyRating")]
    public string SafetyRating { get; init; } = "A";

    [JsonPropertyName("puntajeGeneral")]
    public float PuntajeGeneral { get; init; }

    [JsonPropertyName("vueltasSumar")]
    public int VueltasSumar { get; init; } = 0;

    [JsonPropertyName("kmSumar")]
    public float KmSumar { get; init; } = 0.0f;
}
