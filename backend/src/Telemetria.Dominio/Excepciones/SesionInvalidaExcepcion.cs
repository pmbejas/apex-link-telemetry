namespace Telemetria.Dominio.Excepciones;

public sealed class SesionInvalidaExcepcion : Exception
{
    public SesionInvalidaExcepcion(string mensaje)
        : base(mensaje)
    {
    }

    public SesionInvalidaExcepcion(string mensaje, Exception excepcionInterna)
        : base(mensaje, excepcionInterna)
    {
    }
}
