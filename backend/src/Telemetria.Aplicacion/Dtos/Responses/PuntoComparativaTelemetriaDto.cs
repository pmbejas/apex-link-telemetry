namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Punto de comparación espacial entre dos vueltas interpoladas sobre el dominio de distancia métrica en pista.
/// </summary>
public class PuntoComparativaTelemetriaDto
{
    [JsonPropertyName("distanceMeters")]
    public float DistanciaPistaMetros { get; set; }

    [JsonPropertyName("baseSpeedKmh")]
    public float VelocidadBaseKmh { get; set; }

    [JsonPropertyName("comparedSpeedKmh")]
    public float VelocidadComparadaKmh { get; set; }

    [JsonPropertyName("deltaSpeedKmh")]
    public float DeltaVelocidadKmh { get; set; }

    [JsonPropertyName("baseThrottle")]
    public float AceleradorBase { get; set; }

    [JsonPropertyName("comparedThrottle")]
    public float AceleradorComparada { get; set; }

    [JsonPropertyName("baseBrake")]
    public float FrenoBase { get; set; }

    [JsonPropertyName("comparedBrake")]
    public float FrenoComparada { get; set; }

    [JsonPropertyName("accumulatedDeltaTimeMs")]
    public int DeltaTiempoAcumuladoMs { get; set; }
}
