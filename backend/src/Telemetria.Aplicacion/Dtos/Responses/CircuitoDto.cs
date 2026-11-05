namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

public class CircuitoDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("pais")]
    public string Pais { get; set; } = string.Empty;

    [JsonPropertyName("codigoPais")]
    public string CodigoPais { get; set; } = string.Empty;

    [JsonPropertyName("longitudMetros")]
    public float LongitudMetros { get; set; }

    [JsonPropertyName("cantidadCurvas")]
    public int CantidadCurvas { get; set; }

    [JsonPropertyName("recordVuelta")]
    public string RecordVuelta { get; set; } = string.Empty;

    [JsonPropertyName("pilotoRecord")]
    public string PilotoRecord { get; set; } = string.Empty;

    [JsonPropertyName("zonasDrsCount")]
    public int ZonasDrsCount { get; set; }

    [JsonPropertyName("esOficialFappe")]
    public bool EsOficialFappe { get; set; }
}

public sealed class CircuitoDetalleDto : CircuitoDto
{
    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("variante")]
    public string Variante { get; set; } = string.Empty;

    [JsonPropertyName("escala")]
    public float Escala { get; set; } = 1.0f;

    [JsonPropertyName("xOffset")]
    public float XOffset { get; set; }

    [JsonPropertyName("zOffset")]
    public float ZOffset { get; set; }

    [JsonPropertyName("anchoPista")]
    public float AnchoPista { get; set; }

    [JsonPropertyName("altoPista")]
    public float AltoPista { get; set; }

    [JsonPropertyName("archivoMapa")]
    public string ArchivoMapa { get; set; } = "map.png";
}
