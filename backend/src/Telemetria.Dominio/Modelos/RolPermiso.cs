namespace Telemetria.Dominio.Modelos;

/// <summary>
/// Tabla intermedia que asocia un rol con un permiso granular dentro del modelo relacional 3FN.
/// </summary>
public class RolPermiso : EntidadBase
{
    public Guid RolId { get; set; }

    public Rol? Rol { get; set; }

    public Guid PermisoId { get; set; }

    public Permiso? Permiso { get; set; }
}
