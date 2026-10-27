namespace Telemetria.Dominio.Modelos;

/// <summary>
/// Clase base abstracta para todas las entidades del dominio.
/// Implementa identificador único Guid y campos de auditoría temporal obligatorios.
/// </summary>
public abstract class EntidadBase
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
