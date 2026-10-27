namespace Telemetria.Aplicacion.Servicios;

using System.Threading;
using Telemetria.Dominio.Contratos.Hardware;
using Telemetria.Dominio.Modelos;

public sealed class LectorTelemetriaConmutable : ILectorTelemetria, IControlModoTelemetria
{
    private readonly ILectorTelemetria _lectorReal;
    private readonly ILectorTelemetria _lectorMock;
    private volatile bool _usarMock;
    private long _versionConmutacion;

    public LectorTelemetriaConmutable(
        ILectorTelemetria lectorReal,
        ILectorTelemetria lectorMock,
        bool usarMockInicial = false)
    {
        _lectorReal = lectorReal;
        _lectorMock = lectorMock;
        _usarMock = usarMockInicial;
    }

    public bool EstaEnModoMock => _usarMock;
    public long VersionConmutacion => Interlocked.Read(ref _versionConmutacion);

    public void ConmutarModoMock(bool activarMock)
    {
        if (_usarMock != activarMock)
        {
            _usarMock = activarMock;
            Interlocked.Increment(ref _versionConmutacion);
        }
    }

    private ILectorTelemetria ProveedorActivo => _usarMock ? _lectorMock : _lectorReal;

    public bool EstaConectado() => ProveedorActivo.EstaConectado();
    public bool IntentarConectar() => ProveedorActivo.IntentarConectar();
    public CuadroTelemetria ObtenerCuadroActual() => ProveedorActivo.ObtenerCuadroActual();
    public CuadroTelemetria ObtenerUltimoCuadroConocido() => ProveedorActivo.ObtenerUltimoCuadroConocido();
    public MetadatosEstaticos ObtenerMetadatosEstaticos() => ProveedorActivo.ObtenerMetadatosEstaticos();
    public void Desconectar() => ProveedorActivo.Desconectar();

    public void Dispose()
    {
        _lectorReal.Dispose();
        _lectorMock.Dispose();
    }
}
