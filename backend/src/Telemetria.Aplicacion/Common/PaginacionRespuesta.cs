namespace Telemetria.Aplicacion.Common;

/// <summary>
/// Envoltorio estandarizado para respuestas con paginación de colecciones analíticas.
/// </summary>
/// <typeparam name="T">Tipo del elemento contenido en la página.</typeparam>
public class PaginacionRespuesta<T>
{
    public List<T> Elementos { get; set; } = new();

    public int PaginaActual { get; set; }

    public int TamanioPagina { get; set; }

    public int TotalRegistros { get; set; }

    public int TotalPaginas => TamanioPagina > 0
        ? (int)Math.Ceiling((double)TotalRegistros / TamanioPagina)
        : 0;

    public bool TienePaginaAnterior => PaginaActual > 1;

    public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
}
