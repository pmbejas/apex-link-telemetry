namespace Telemetria.Dominio.Modelos;

/// <summary>
/// Representa un usuario registrado o piloto en el servidor central o estación local.
/// </summary>
public class Usuario : EntidadBase
{
    public string NombreCompleto { get; set; } = string.Empty;

    public string AliasPiloto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public List<Rol> Roles { get; set; } = new();

    public List<Sesion> Sesiones { get; set; } = new();
}
