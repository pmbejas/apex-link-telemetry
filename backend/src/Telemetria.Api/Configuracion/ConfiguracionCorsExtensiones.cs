namespace Telemetria.Api.Configuracion;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ConfiguracionCorsExtensiones
{
    public const string POLITICA_CORS_SIMULADOR = "PoliticaCorsSimulador";

    public static IServiceCollection ConfigurarCorsTelemetria(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var origenesConfigurados = configuracion.GetSection("Cors:OrigenesPermitidos").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://127.0.0.1:3000", "http://localhost:8080" };

        servicios.AddCors(opciones =>
        {
            opciones.AddPolicy(POLITICA_CORS_SIMULADOR, politica =>
            {
                politica.SetIsOriginAllowed(origen =>
                    ValidadorOrigenRedLocal.EsOrigenPermitido(origen, origenesConfigurados))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            });
        });

        return servicios;
    }
}
