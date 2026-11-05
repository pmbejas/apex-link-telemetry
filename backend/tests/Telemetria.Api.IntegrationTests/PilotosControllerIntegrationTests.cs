namespace Telemetria.Api.IntegrationTests;

using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Dtos.Requests;
using Telemetria.Aplicacion.Dtos.Responses;
using Xunit;

public sealed class PilotosControllerIntegrationTests : IAsyncDisposable
{
    private readonly FactoriaAplicacionTelemetria _factoria;
    private readonly HttpClient _cliente;

    public PilotosControllerIntegrationTests()
    {
        _factoria = new FactoriaAplicacionTelemetria();
        _cliente = _factoria.CreateClient();
    }

    [Fact]
    public async Task GetPerfil_PilotoGarlati_RetornaOkConPerfilYRadar()
    {
        // Arrange
        var garlatiId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var respuesta = await _cliente.GetAsync($"/api/pilotos/{garlatiId}/perfil");

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<PerfilPilotoDetalleDto>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exitoso);
        Assert.NotNull(cuerpo.Datos);
        Assert.Equal("Alejo Garlati", cuerpo.Datos.Nombre);
        Assert.Equal("AG_PRO", cuerpo.Datos.Alias);
        Assert.NotNull(cuerpo.Datos.UltimaEvaluacion);
        Assert.True(cuerpo.Datos.PaceRating >= 2500);
    }

    [Fact]
    public async Task GetPerfil_PilotoSalas_RetornaOkConPerfilYRadar()
    {
        // Arrange
        var salasId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act
        var respuesta = await _cliente.GetAsync($"/api/pilotos/{salasId}/perfil");

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<PerfilPilotoDetalleDto>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exitoso);
        Assert.NotNull(cuerpo.Datos);
        Assert.Equal("Alejo Salas", cuerpo.Datos.Nombre);
        Assert.Equal("AS_GT3", cuerpo.Datos.Alias);
        Assert.NotNull(cuerpo.Datos.UltimaEvaluacion);
        Assert.Equal("Pro-Am GT3", cuerpo.Datos.Nivel);
    }

    [Fact]
    public async Task PostEvaluacion_SinSesionId_RetornaBadRequest()
    {
        // Arrange
        var pilotoId = Guid.NewGuid();
        var solicitudInvalida = new GenerarEvaluacionRequest { SesionId = Guid.Empty };

        // Act
        var respuesta = await _cliente.PostAsJsonAsync($"/api/pilotos/{pilotoId}/evaluacion", solicitudInvalida);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostEvaluacion_ConSesionId_CalculaYRetornaOk()
    {
        // Arrange
        var pilotoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var sesionId = Guid.NewGuid();
        var peticion = new GenerarEvaluacionRequest { SesionId = sesionId, UsuarioId = pilotoId };

        // Act
        var respuesta = await _cliente.PostAsJsonAsync($"/api/pilotos/{pilotoId}/evaluacion", peticion);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<EvaluacionRendimientoDto>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exitoso);
        Assert.NotNull(cuerpo.Datos);
        Assert.InRange(cuerpo.Datos.Velocidad, 0, 100);
        Assert.InRange(cuerpo.Datos.TrailBraking, 0, 100);
    }

    [Fact]
    public async Task PutRating_ActualizaYRetornaOk()
    {
        // Arrange
        var pilotoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var peticion = new ActualizarRatingRequest
        {
            PaceRating = 2860,
            SafetyRating = "S",
            PuntajeGeneral = 97.0f,
            VueltasSumar = 5,
            KmSumar = 28.9f
        };

        // Act
        var respuesta = await _cliente.PutAsJsonAsync($"/api/pilotos/{pilotoId}/rating", peticion);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<string>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exitoso);
    }

    public async ValueTask DisposeAsync()
    {
        _cliente.Dispose();
        await _factoria.DisposeAsync();
    }
}
