namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Representa un punto de muestreo de telemetría de alta densidad ordenado por distancia métrica de pista.
/// </summary>
public sealed class MuestraTelemetriaDto
{
    [JsonPropertyName("distanceMeters")]
    public float DistanciaPistaMetros { get; set; }

    [JsonPropertyName("speedKmh")]
    public float VelocidadKmh { get; set; }

    [JsonPropertyName("throttle")]
    public float Acelerador { get; set; }

    [JsonPropertyName("brake")]
    public float Freno { get; set; }

    [JsonPropertyName("gear")]
    public int Marcha { get; set; }

    [JsonPropertyName("rpm")]
    public int Rpms { get; set; }

    [JsonPropertyName("gLat")]
    public float FuerzaGLat { get; set; }

    [JsonPropertyName("gLon")]
    public float FuerzaGLon { get; set; }

    [JsonPropertyName("worldPositionX")]
    public float PosicionX { get; set; }

    [JsonPropertyName("worldPositionY")]
    public float PosicionY { get; set; }

    [JsonPropertyName("worldPositionZ")]
    public float PosicionZ { get; set; }
}
