namespace Telemetria.Dominio.Excepciones;

public sealed class SimuladorDesconectadoExcepcion : Exception
{
    public SimuladorDesconectadoExcepcion(string mensaje)
        : base(mensaje)
    {
    }

    public SimuladorDesconectadoExcepcion(string mensaje, Exception excepcionInterna)
        : base(mensaje, excepcionInterna)
    {
    }
}
