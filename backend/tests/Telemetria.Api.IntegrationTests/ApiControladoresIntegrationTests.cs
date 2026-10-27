namespace Telemetria.Api.IntegrationTests;

using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Dtos.Requests;
using Telemetria.Aplicacion.Dtos.Responses;
using Xunit;

public sealed class ApiControladoresIntegrationTests : IAsyncDisposable
{
    private readonly FactoriaAplicacionTelemetria _factoria;
    private readonly HttpClient _cliente;

    public ApiControladoresIntegrationTests()
    {
        _factoria = new FactoriaAplicacionTelemetria();
        _cliente = _factoria.CreateClient();
    }

    [Fact]
    public async Task GetEstado_RetornaOkConFormatoRespuestaApi()
    {
        // Act
        var respuesta = await _cliente.GetAsync("/api/estado");

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<EstadoServicioDto>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exitoso);
        Assert.NotNull(cuerpo.Datos);
    }

    [Fact]
    public async Task PostAlternarMock_RetornaOkYConmuta()
    {
        // Arrange
        var solicitud = new AlternarModoMockRequest { UsarMock = true };

        // Act
        var respuesta = await _cliente.PostAsJsonAsync("/api/mock/alternar", solicitud);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<ModoMockRespuestaDto>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exitoso);
        Assert.NotNull(cuerpo.Datos);
        Assert.True(cuerpo.Datos.ModoMockActivo);
    }

    [Fact]
    public async Task GetSesiones_RetornaOkConPaginacion()
    {
        // Act
        var respuesta = await _cliente.GetAsync("/api/sesiones?pagina=1&tamanioPagina=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<PaginacionRespuesta<SesionResumenDto>>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exitoso);
        Assert.NotNull(cuerpo.Datos);
        Assert.Equal(1, cuerpo.Datos.PaginaActual);
    }

    [Fact]
    public async Task GetVueltaInexistente_Retorna404ConRespuestaApi()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var respuesta = await _cliente.GetAsync($"/api/vueltas/{idInexistente}/telemetria");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<List<MuestraTelemetriaDto>>>();
        Assert.NotNull(cuerpo);
        Assert.False(cuerpo.Exitoso);
        Assert.Contains("No se hallaron registros", cuerpo.Errores[0]);
    }

    public async ValueTask DisposeAsync()
    {
        _cliente.Dispose();
        await _factoria.DisposeAsync();
    }
}
