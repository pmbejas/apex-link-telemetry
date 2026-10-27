namespace Telemetria.Dominio.Modelos;

public sealed record class MetadatosEstaticos
{
    public string NombreAuto { get; init; } = string.Empty;
    public string NombrePista { get; init; } = string.Empty;
    public string ConfiguracionPista { get; init; } = string.Empty;
    public int MaxRpm { get; init; }
    public float MaxCombustible { get; init; }
    public int CantidadSectores { get; init; }
    public float LongitudPistaMetros { get; init; }
    public string VersionSm { get; init; } = string.Empty;
    public string VersionAc { get; init; } = string.Empty;
}
