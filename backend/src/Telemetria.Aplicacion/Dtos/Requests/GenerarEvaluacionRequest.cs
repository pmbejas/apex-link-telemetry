namespace Telemetria.Aplicacion.Dtos.Requests;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Petición para disparar la evaluación algorítmica de rendimiento de una tanda.
/// </summary>
public sealed record GenerarEvaluacionRequest
{
    [JsonPropertyName("sesionId")]
    public Guid SesionId { get; init; }

    [JsonPropertyName("usuarioId")]
    public Guid? UsuarioId { get; init; }
}
