namespace Telemetria.Aplicacion.Dtos.Responses;

using System.Text.Json.Serialization;

public sealed class DatosDinamicaDto
{
    [JsonPropertyName("gForce")]
    public required FuerzasGDto FuerzaG { get; set; }

    [JsonPropertyName("worldPos")]
    public required PosicionMundoDto PosicionMundo { get; set; }
}
