namespace Telemetria.Dominio.Modelos;

/// <summary>
/// Representa un permiso granular bajo la convención recurso:accion (ej. telemetria:leer, sesion:crear).
/// </summary>
public class Permiso : EntidadBase
{
    public string Codigo { get; set; } = string.Empty;

    public string Modulo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public List<Rol> Roles { get; set; } = new();

    public List<RolPermiso> RolesPermisos { get; set; } = new();
}
