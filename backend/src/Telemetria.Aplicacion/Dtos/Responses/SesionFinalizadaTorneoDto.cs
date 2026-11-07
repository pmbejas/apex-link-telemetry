namespace Telemetria.Aplicacion.Dtos.Responses;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Resumen de una tanda recién finalizada con vueltas limpias pendiente de confirmación para el Torneo FAPPE.
/// </summary>
public sealed record SesionFinalizadaTorneoDto
{
    [JsonPropertyName("sesionId")]
    public Guid SesionId { get; init; }

    [JsonPropertyName("circuito")]
    public string Circuito { get; init; } = string.Empty;

    [JsonPropertyName("modeloVehiculo")]
    public string ModeloVehiculo { get; init; } = string.Empty;

    [JsonPropertyName("totalVueltas")]
    public int TotalVueltas { get; init; }

    [JsonPropertyName("vueltasValidas")]
    public int VueltasValidas { get; init; }

    [JsonPropertyName("mejorTiempoMs")]
    public int MejorTiempoMs { get; init; }

    [JsonPropertyName("mejorTiempoTexto")]
    public string MejorTiempoTexto { get; init; } = string.Empty;

    [JsonPropertyName("fechaFin")]
    public DateTimeOffset FechaFin { get; init; }
}
