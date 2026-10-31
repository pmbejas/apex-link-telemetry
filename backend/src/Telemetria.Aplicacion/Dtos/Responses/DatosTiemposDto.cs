namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

public sealed class DatosTiemposDto
{
    [JsonPropertyName("lapTimeMs")]
    public int TiempoVueltaMs { get; set; }

    [JsonPropertyName("lapTimeStr")]
    public string TiempoVueltaTexto { get; set; } = "00:00.000";

    [JsonPropertyName("lastLapTimeMs")]
    public int UltimoTiempoVueltaMs { get; set; }

    [JsonPropertyName("lastLapTimeStr")]
    public string UltimoTiempoVueltaTexto { get; set; } = "--:--.---";

    [JsonPropertyName("bestTimeMs")]
    public int MejorTiempoMs { get; set; }

    [JsonPropertyName("bestTimeStr")]
    public string MejorTiempoTexto { get; set; } = "--:--.---";

    [JsonPropertyName("deltaMs")]
    public int DeltaMs { get; set; }

    [JsonPropertyName("currentLap")]
    public int VueltaActual { get; set; }

    [JsonPropertyName("sector")]
    public int Sector { get; set; }

    [JsonPropertyName("lapProgress")]
    public float ProgresoVuelta { get; set; }
}
