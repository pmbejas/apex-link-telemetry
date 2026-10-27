namespace Telemetria.Aplicacion.Common;

using System.Text.Json.Serialization;
using Telemetria.Aplicacion.Dtos.Responses;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(CabeceraMensajeWsDto))]
[JsonSerializable(typeof(PaqueteTelemetriaDto))]
[JsonSerializable(typeof(EventoVueltaCompletadaDto))]
public partial class ContextoSerializacionTelemetria : JsonSerializerContext
{
}
