using Telemetria.Dominio.Modelos;

namespace Telemetria.Dominio.Contratos.Hardware;

public interface ILectorTelemetria : IDisposable
{
    bool EstaConectado();
    bool IntentarConectar();
    CuadroTelemetria ObtenerCuadroActual();
    CuadroTelemetria ObtenerUltimoCuadroConocido();
    MetadatosEstaticos ObtenerMetadatosEstaticos();
    void Desconectar();
}
