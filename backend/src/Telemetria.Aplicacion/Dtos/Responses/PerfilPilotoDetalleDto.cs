namespace Telemetria.Aplicacion.Dtos.Responses;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed record LogroPilotoDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("titulo")]
    public string Titulo { get; init; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; init; } = string.Empty;

    [JsonPropertyName("icono")]
    public string Icono { get; init; } = string.Empty;

    [JsonPropertyName("desbloqueado")]
    public bool Desbloqueado { get; init; }

    [JsonPropertyName("fechaDesbloqueo")]
    public string? FechaDesbloqueo { get; init; }
}

public sealed record RecordCircuitoPilotoDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("circuito")]
    public string Circuito { get; init; } = string.Empty;

    [JsonPropertyName("pais")]
    public string Pais { get; init; } = string.Empty;

    [JsonPropertyName("tiempo")]
    public string Tiempo { get; init; } = string.Empty;

    [JsonPropertyName("esRecordSesion")]
    public bool EsRecordSesion { get; init; }
}

/// <summary>
/// DTO con el perfil integral del piloto para el carnet y tableros de boxes.
/// </summary>
public sealed record PerfilPilotoDetalleDto
{
    [JsonPropertyName("usuarioId")]
    public Guid UsuarioId { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("alias")]
    public string Alias { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("licencia")]
    public string Licencia { get; init; } = string.Empty;

    [JsonPropertyName("nacionalidad")]
    public string Nacionalidad { get; init; } = "Argentina";

    [JsonPropertyName("codigoPais")]
    public string CodigoPais { get; init; } = "ARG";

    [JsonPropertyName("vehiculoPrincipal")]
    public string VehiculoPrincipal { get; init; } = string.Empty;

    [JsonPropertyName("simuladorPrincipal")]
    public string SimuladorPrincipal { get; init; } = "Assetto Corsa · Shared Memory Win32 MMF";

    [JsonPropertyName("paceRating")]
    public int PaceRating { get; init; }

    [JsonPropertyName("safetyRating")]
    public string SafetyRating { get; init; } = "A";

    [JsonPropertyName("nivel")]
    public string Nivel { get; init; } = string.Empty;

    [JsonPropertyName("puntajeGeneral")]
    public float PuntajeGeneral { get; init; }

    [JsonPropertyName("vueltasTotales")]
    public int VueltasTotales { get; init; }

    [JsonPropertyName("kilometrosTotales")]
    public float KilometrosTotales { get; init; }

    [JsonPropertyName("ultimaEvaluacion")]
    public EvaluacionRendimientoDto? UltimaEvaluacion { get; init; }

    [JsonPropertyName("logros")]
    public List<LogroPilotoDto> Logros { get; init; } = new();

    [JsonPropertyName("records")]
    public List<RecordCircuitoPilotoDto> Records { get; init; } = new();
}
