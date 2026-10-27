namespace Telemetria.Infraestructura.Sincronizacion;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telemetria.Dominio.Contratos.Sincronizacion;

/// <summary>
/// Servicio hospedado en segundo plano que ejecuta periódicamente la sincronización
/// Store-and-Forward hacia la base de datos central PostgreSQL en un ámbito Scoped.
/// </summary>
public sealed class ServicioSincronizacionSegundoPlano : BackgroundService
{
    private const int INTERVALO_SINCRONIZACION_SEGUNDOS = 60;

    private readonly IServiceProvider _proveedorServicios;
    private readonly ILogger<ServicioSincronizacionSegundoPlano> _logger;

    public ServicioSincronizacionSegundoPlano(
        IServiceProvider proveedorServicios,
        ILogger<ServicioSincronizacionSegundoPlano> logger)
    {
        _proveedorServicios = proveedorServicios;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken tokenDetencion)
    {
        _logger.LogInformation("Servicio de sincronización asíncrona hacia base de datos central iniciado.");

        while (!tokenDetencion.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(INTERVALO_SINCRONIZACION_SEGUNDOS), tokenDetencion);

                using var alcance = _proveedorServicios.CreateScope();
                var sincronizador = alcance.ServiceProvider.GetRequiredService<ISincronizadorSesiones>();

                await sincronizador.SincronizarSesionesPendientesAsync(tokenDetencion);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception excepcion)
            {
                _logger.LogWarning(
                    excepcion,
                    "Intento de sincronización hacia servidor central fallido. Se reintentará en el próximo ciclo.");
            }
        }
    }
}
