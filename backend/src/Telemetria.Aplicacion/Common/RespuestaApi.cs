namespace Telemetria.Aplicacion.Common;

/// <summary>
/// Envoltorio estándar para todas las respuestas controladas de la API REST.
/// </summary>
/// <typeparam name="T">Tipo del payload transportado en la respuesta.</typeparam>
public class RespuestaApi<T>
{
    public bool Exitoso { get; set; }

    public string Mensaje { get; set; } = string.Empty;

    public T? Datos { get; set; }

    public List<string> Errores { get; set; } = new();

    public static RespuestaApi<T> ConstruirExito(T datos, string mensaje = "Operación completada satisfactoriamente")
    {
        return new RespuestaApi<T>
        {
            Exitoso = true,
            Mensaje = mensaje,
            Datos = datos
        };
    }

    public static RespuestaApi<T> ConstruirFallo(string error, string mensaje = "Se produjo un error al procesar la solicitud")
    {
        return new RespuestaApi<T>
        {
            Exitoso = false,
            Mensaje = mensaje,
            Errores = new List<string> { error }
        };
    }

    public static RespuestaApi<T> ConstruirFallo(List<string> errores, string mensaje = "Se produjeron errores de validación")
    {
        return new RespuestaApi<T>
        {
            Exitoso = false,
            Mensaje = mensaje,
            Errores = errores ?? new List<string>()
        };
    }
}
