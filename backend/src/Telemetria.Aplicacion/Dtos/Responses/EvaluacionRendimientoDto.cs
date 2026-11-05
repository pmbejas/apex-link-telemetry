namespace Telemetria.Aplicacion.Dtos.Responses;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// DTO de evaluación del rendimiento del piloto (radar de 5 ejes, habilidades y AI Race Coach).
/// </summary>
public sealed record EvaluacionRendimientoDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [JsonPropertyName("usuarioId")]
    public Guid UsuarioId { get; init; }

    [JsonPropertyName("sesionId")]
    public Guid? SesionId { get; init; }

    // Radar 5 ejes (0..100)
    [JsonPropertyName("velocidad")]
    public int Velocidad { get; init; }

    [JsonPropertyName("consistencia")]
    public int Consistencia { get; init; }

    [JsonPropertyName("frenada")]
    public int Frenada { get; init; }

    [JsonPropertyName("control")]
    public int Control { get; init; }

    [JsonPropertyName("agresividad")]
    public int Agresividad { get; init; }

    // Habilidades clave (0..100)
    [JsonPropertyName("trailBraking")]
    public int TrailBraking { get; init; }

    [JsonPropertyName("traccionSalida")]
    public int TraccionSalida { get; init; }

    [JsonPropertyName("velocidadApice")]
    public int VelocidadApice { get; init; }

    [JsonPropertyName("respetoLimites")]
    public int RespetoLimites { get; init; }

    // AI Race Coach Heurístico
    [JsonPropertyName("puntoFuerteTitulo")]
    public string PuntoFuerteTitulo { get; init; } = string.Empty;

    [JsonPropertyName("puntoFuerteDesc")]
    public string PuntoFuerteDesc { get; init; } = string.Empty;

    [JsonPropertyName("oportunidadMejoraTitulo")]
    public string OportunidadMejoraTitulo { get; init; } = string.Empty;

    [JsonPropertyName("oportunidadMejoraDesc")]
    public string OportunidadMejoraDesc { get; init; } = string.Empty;

    // Consejos por habilidad
    [JsonPropertyName("consejoFrenada")]
    public string ConsejoFrenada { get; init; } = string.Empty;

    [JsonPropertyName("consejoTraccion")]
    public string ConsejoTraccion { get; init; } = string.Empty;

    [JsonPropertyName("consejoApice")]
    public string ConsejoApice { get; init; } = string.Empty;

    [JsonPropertyName("consejoConsistencia")]
    public string ConsejoConsistencia { get; init; } = string.Empty;

    [JsonPropertyName("consejoLimites")]
    public string ConsejoLimites { get; init; } = string.Empty;

    [JsonPropertyName("fechaCreacion")]
    public DateTimeOffset FechaCreacion { get; init; } = DateTimeOffset.UtcNow;
}
