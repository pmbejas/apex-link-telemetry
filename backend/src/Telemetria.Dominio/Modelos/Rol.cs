namespace Telemetria.Dominio.Modelos;

/// <summary>
/// Representa un rol de seguridad dentro del sistema RBAC parametrizable.
/// </summary>
public class Rol : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public List<Permiso> Permisos { get; set; } = new();

    public List<RolPermiso> RolesPermisos { get; set; } = new();

    public List<Usuario> Usuarios { get; set; } = new();
}
