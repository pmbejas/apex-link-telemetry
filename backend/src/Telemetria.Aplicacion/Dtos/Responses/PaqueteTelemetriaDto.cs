namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

public sealed class PaqueteTelemetriaDto
{
    [JsonPropertyName("type")]
    public string Tipo { get; set; } = "FRAME";

    [JsonPropertyName("timestamp")]
    public long MarcaTemporalUnixMs { get; set; }

    [JsonPropertyName("car")]
    public required DatosAutoDto Auto { get; set; }

    [JsonPropertyName("tyres")]
    public required DatosNeumaticosDto Neumaticos { get; set; }

    [JsonPropertyName("timing")]
    public required DatosTiemposDto Tiempos { get; set; }

    [JsonPropertyName("dynamics")]
    public required DatosDinamicaDto Dinamica { get; set; }

    [JsonPropertyName("session")]
    public required DatosSesionDto Sesion { get; set; }
}
