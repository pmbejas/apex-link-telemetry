namespace Telemetria.Api.IntegrationTests;

using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Dtos.Requests;
using Telemetria.Aplicacion.Dtos.Responses;
using Xunit;

public sealed class AuthControllerIntegrationTests : IAsyncDisposable
{
    private readonly FactoriaAplicacionTelemetria _factoria;
    private readonly HttpClient _cliente;

    public AuthControllerIntegrationTests()
    {
        _factoria = new FactoriaAplicacionTelemetria();
        _cliente = _factoria.CreateClient();
    }

    [Fact]
    public async Task PostLogin_CredencialesGarlatiValidas_RetornaOkConTokenYPerfil()
    {
        // Arrange
        var solicitud = new LoginRequest
        {
            Email = "agarlati@apexlink.io",
            Password = "Password123!"
        };

        // Act
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login", solicitud);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<LoginRespuestaDto>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exitoso);
        Assert.NotNull(cuerpo.Datos);
        Assert.False(string.IsNullOrWhiteSpace(cuerpo.Datos.Token));
        Assert.NotNull(cuerpo.Datos.Usuario);
        Assert.Equal("Alejo Garlati", cuerpo.Datos.Usuario.Nombre);
        Assert.Equal("AG_PRO", cuerpo.Datos.Usuario.Alias);
    }

    [Fact]
    public async Task PostLogin_CredencialesSalasValidas_RetornaOkConTokenYPerfil()
    {
        // Arrange
        var solicitud = new LoginRequest
        {
            Email = "asalas@apexlink.io",
            Password = "Password123!"
        };

        // Act
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login", solicitud);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaApi<LoginRespuestaDto>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exitoso);
        Assert.NotNull(cuerpo.Datos);
        Assert.Equal("Alejo Salas", cuerpo.Datos.Usuario.Nombre);
        Assert.Equal("AS_GT3", cuerpo.Datos.Usuario.Alias);
    }

    [Fact]
    public async Task PostLogin_PasswordInvalido_RetornaUnauthorized()
    {
        // Arrange
        var solicitud = new LoginRequest
        {
            Email = "agarlati@apexlink.io",
            Password = "PasswordErronea999"
        };

        // Act
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login", solicitud);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostLogin_EmailInexistente_RetornaUnauthorized()
    {
        // Arrange
        var solicitud = new LoginRequest
        {
            Email = "inexistente@apexlink.io",
            Password = "Password123!"
        };

        // Act
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login", solicitud);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        _cliente.Dispose();
        await _factoria.DisposeAsync();
    }
}
