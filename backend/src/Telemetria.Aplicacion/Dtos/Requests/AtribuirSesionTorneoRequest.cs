namespace Telemetria.Aplicacion.Dtos.Requests;

using System.Text.Json.Serialization;

/// <summary>
/// Solicitud para confirmar si una sesión recién finalizada pertenece a un participante temporal del Torneo FAPPE.
/// </summary>
public sealed class AtribuirSesionTorneoRequest
{
    [JsonPropertyName("esParticipanteTorneo")]
    public bool EsParticipanteTorneo { get; set; }

    [JsonPropertyName("nombreParticipante")]
    public string? NombreParticipante { get; set; }
}
