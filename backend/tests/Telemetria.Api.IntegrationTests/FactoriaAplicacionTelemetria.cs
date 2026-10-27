namespace Telemetria.Api.IntegrationTests;

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class FactoriaAplicacionTelemetria : WebApplicationFactory<Program>
{
    private readonly string _nombreBaseDatosPrueba = $"telemetria_test_{Guid.NewGuid():N}.db";

    static FactoriaAplicacionTelemetria()
    {
        string rutaApi = @"C:\Users\Pablo Bejas\Desktop\Pablo\WEB DEVELOP\Proyectos\UTN\FAPPE\backend\src\Telemetria.Api";
        if (Directory.Exists(rutaApi))
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_TELEMETRIA_API", rutaApi);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection()
                .UseEphemeralDataProtectionProvider();
        });

        builder.ConfigureAppConfiguration((contexto, config) =>
        {
            config.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ConnectionStrings:TelemetriaLocal", $"Data Source={_nombreBaseDatosPrueba};Mode=ReadWriteCreate;"),
                new KeyValuePair<string, string?>("ConnectionStrings:ServidorCentralPostgreSql", string.Empty),
                new KeyValuePair<string, string?>("Seguridad:PermitirAccesoLocalEdgeSinToken", "true"),
                new KeyValuePair<string, string?>("Telemetria:ModoMock", "true")
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_nombreBaseDatosPrueba))
        {
            try
            {
                File.Delete(_nombreBaseDatosPrueba);
            }
            catch
            {
                // Limpieza defensiva de archivo temporal de prueba
            }
        }
    }
}
