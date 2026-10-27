namespace Telemetria.Aplicacion.Contratos;

using System.Threading;
using System.Threading.Tasks;
using Telemetria.Aplicacion.Dtos.Responses;

public interface IDifusorTelemetria
{
    bool TieneClientesConectados { get; }
    ValueTask DifundirTelemetriaAsync(PaqueteTelemetriaDto paquete, CancellationToken token = default);
    ValueTask EmitirEventoVueltaAsync(EventoVueltaCompletadaDto evento, CancellationToken token = default);
}
