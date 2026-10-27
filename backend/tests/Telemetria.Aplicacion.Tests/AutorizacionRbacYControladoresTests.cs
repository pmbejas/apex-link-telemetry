namespace Telemetria.Aplicacion.Tests;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Telemetria.Api.Autorizacion;
using Telemetria.Api.Controladores;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Dtos.Requests;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Dominio.Contratos.Hardware;
using Telemetria.Dominio.Modelos;
using Xunit;

public sealed class AutorizacionRbacYControladoresTests
{
    [Fact]
    public async Task ProveedorPoliticasPermiso_ConPrefijoPermiso_ConstruyePoliticaDinamica()
    {
        var opciones = Options.Create(new AuthorizationOptions());
        var proveedor = new ProveedorPoliticasPermiso(opciones);

        var politica = await proveedor.GetPolicyAsync("Permiso:telemetria:leer");

        Assert.NotNull(politica);
        Assert.Single(politica.Requirements);
        var requisito = Assert.IsType<RequisitoPermiso>(politica.Requirements[0]);
        Assert.Equal("telemetria:leer", requisito.Permiso);
    }

    [Fact]
    public async Task ManejadorAutorizacionPermiso_ConClaimExplicito_ConcedeAcceso()
    {
        var configuracionMock = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seguridad:PermitirAccesoLocalEdgeSinToken"] = "false"
            })
            .Build();

        var manejador = new ManejadorAutorizacionPermiso(configuracionMock);
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("permiso", "telemetria:leer")
        }, "TestAuth"));

        var requisito = new RequisitoPermiso("telemetria:leer");
        var contexto = new AuthorizationHandlerContext(new[] { requisito }, usuario, null);

        await manejador.HandleAsync(contexto);

        Assert.True(contexto.HasSucceeded);
    }

    [Fact]
    public async Task ManejadorAutorizacionPermiso_SinClaimPeroConBypassEdgeActivo_ConcedeAcceso()
    {
        var configuracionMock = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seguridad:PermitirAccesoLocalEdgeSinToken"] = "true"
            })
            .Build();

        var manejador = new ManejadorAutorizacionPermiso(configuracionMock);
        var usuarioAnonimo = new ClaimsPrincipal(new ClaimsIdentity());

        var requisito = new RequisitoPermiso("telemetria:leer");
        var contexto = new AuthorizationHandlerContext(new[] { requisito }, usuarioAnonimo, null);

        await manejador.HandleAsync(contexto);

        Assert.True(contexto.HasSucceeded);
    }

    [Fact]
    public async Task ManejadorAutorizacionPermiso_SinClaimYConBypassEdgeDesactivado_NoConcedeAcceso()
    {
        var configuracionMock = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seguridad:PermitirAccesoLocalEdgeSinToken"] = "false"
            })
            .Build();

        var manejador = new ManejadorAutorizacionPermiso(configuracionMock);
        var usuarioAnonimo = new ClaimsPrincipal(new ClaimsIdentity());

        var requisito = new RequisitoPermiso("telemetria:leer");
        var contexto = new AuthorizationHandlerContext(new[] { requisito }, usuarioAnonimo, null);

        await manejador.HandleAsync(contexto);

        Assert.False(contexto.HasSucceeded);
    }

    [Fact]
    public async Task TelemetriaController_ObtenerSesiones_RetornaOkConRespuestaApi()
    {
        var mockServicioAnalisis = new Mock<IServicioAnalisisTelemetria>();
        mockServicioAnalisis.Setup(s => s.ObtenerSesionesPaginadasAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginacionRespuesta<SesionResumenDto>
            {
                Elementos = new List<SesionResumenDto> { new() { Circuito = "monza" } },
                PaginaActual = 1,
                TamanioPagina = 10,
                TotalRegistros = 1
            });

        var controlador = new TelemetriaController(mockServicioAnalisis.Object);

        var accionResultado = await controlador.ObtenerSesiones(1, 10);
        var okResult = Assert.IsType<OkObjectResult>(accionResultado.Result);
        var respuestaApi = Assert.IsType<RespuestaApi<PaginacionRespuesta<SesionResumenDto>>>(okResult.Value);

        Assert.True(respuestaApi.Exitoso);
        Assert.NotNull(respuestaApi.Datos);
        Assert.Single(respuestaApi.Datos.Elementos);
    }

    [Fact]
    public void EstadoController_AlternarModoMock_ConmutaCorrectamente()
    {
        var mockLector = new Mock<ILectorTelemetria>();
        var mockControl = new Mock<IControlModoTelemetria>();
        mockControl.SetupGet(c => c.EstaEnModoMock).Returns(true);

        var controlador = new EstadoController(
            mockLector.Object,
            mockControl.Object,
            NullLogger<EstadoController>.Instance);

        var peticion = new AlternarModoMockRequest { UsarMock = true };
        var accionResultado = controlador.AlternarModoMock(peticion);

        mockControl.Verify(c => c.ConmutarModoMock(true), Times.Once);

        var okResult = Assert.IsType<OkObjectResult>(accionResultado.Result);
        var respuesta = Assert.IsType<RespuestaApi<EstadoMockDto>>(okResult.Value);
        Assert.True(respuesta.Exitoso);
        Assert.True(respuesta.Datos!.ModoMockActivo);
    }
}
