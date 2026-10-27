namespace Telemetria.Aplicacion.Dtos.Requests;

using System.Text.Json.Serialization;

/// <summary>
/// Solicitud para conmutar en caliente entre el lector de hardware de Assetto Corsa y el generador mock.
/// </summary>
public sealed class AlternarModoMockRequest
{
    [JsonPropertyName("usarMock")]
    public bool? UsarMock { get; set; }
}
