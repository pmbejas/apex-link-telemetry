namespace Telemetria.Api.IntegrationTests;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Dtos.Responses;
using Xunit;

public sealed class CircuitosControllerIntegrationTests : IAsyncDisposable
{
    private readonly FactoriaAplicacionTelemetria _factoria;
    private readonly HttpClient _cliente;

    public CircuitosControllerIntegrationTests()
    {
        _factoria = new FactoriaAplicacionTelemetria();
        _cliente = _factoria.CreateClient();
    }

    [Fact]
    public async Task GetCircuitos_RetornaOkConListaDeCircuitosFappe()
    {
        // Act
        var respuesta = await _cliente.GetAsync("/api/circuitos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<List<CircuitoDto>>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exitoso);
        Assert.NotNull(cuerpo.Datos);
        Assert.True(cuerpo.Datos.Count >= 3);
        Assert.Contains(cuerpo.Datos, c => c.Id == "monza_f1_2025" || c.Id == "monza");
    }

    [Fact]
    public async Task GetCircuitoPorId_MonzaExistente_RetornaDetalleYCalibracion()
    {
        // Act
        var respuesta = await _cliente.GetAsync("/api/circuitos/monza_f1_2025");

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<CircuitoDetalleDto>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exitoso);
        Assert.NotNull(cuerpo.Datos);
        Assert.Equal(5793f, cuerpo.Datos.LongitudMetros);
        Assert.Equal(1.4f, cuerpo.Datos.Escala);
    }

    public async ValueTask DisposeAsync()
    {
        _cliente.Dispose();
        await _factoria.DisposeAsync();
    }
}
