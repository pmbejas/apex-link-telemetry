namespace Telemetria.Aplicacion.Dtos.Responses;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Objeto de transferencia para el resumen analítico y listado histórico de sesiones en boxes.
/// </summary>
public sealed class SesionResumenDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("circuit")]
    public string Circuito { get; set; } = string.Empty;

    [JsonPropertyName("trackConfig")]
    public string ConfiguracionCircuito { get; set; } = string.Empty;

    [JsonPropertyName("carModel")]
    public string ModeloVehiculo { get; set; } = string.Empty;

    [JsonPropertyName("sessionType")]
    public int TipoSesion { get; set; }

    [JsonPropertyName("isMockSimulation")]
    public bool EsSimulacionMock { get; set; }

    [JsonPropertyName("startTime")]
    public DateTimeOffset FechaInicio { get; set; }

    [JsonPropertyName("endTime")]
    public DateTimeOffset? FechaFin { get; set; }

    [JsonPropertyName("totalLaps")]
    public int TotalVueltas { get; set; }

    [JsonPropertyName("bestLapTimeMs")]
    public int MejorTiempoMs { get; set; }
}
