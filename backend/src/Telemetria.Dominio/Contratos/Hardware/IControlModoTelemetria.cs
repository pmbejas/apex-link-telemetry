namespace Telemetria.Dominio.Contratos.Hardware;

public interface IControlModoTelemetria
{
    bool EstaEnModoMock { get; }
    long VersionConmutacion { get; }
    void ConmutarModoMock(bool activarMock);
}
