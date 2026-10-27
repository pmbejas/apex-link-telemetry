namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

public sealed class DatosAutoDto
{
    [JsonPropertyName("speedKmh")]
    public float VelocidadKmh { get; set; }

    [JsonPropertyName("rpms")]
    public int Rpms { get; set; }

    [JsonPropertyName("maxRpm")]
    public int MaxRpm { get; set; }

    [JsonPropertyName("gear")]
    public int Marcha { get; set; }

    [JsonPropertyName("throttle")]
    public float Acelerador { get; set; }

    [JsonPropertyName("brake")]
    public float Freno { get; set; }

    [JsonPropertyName("steer")]
    public float Direccion { get; set; }

    [JsonPropertyName("drsAvailable")]
    public bool DrsDisponible { get; set; }

    [JsonPropertyName("drsEnabled")]
    public bool DrsHabilitado { get; set; }
}
