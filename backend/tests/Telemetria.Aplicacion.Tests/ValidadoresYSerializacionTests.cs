namespace Telemetria.Aplicacion.Tests;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Dtos.Requests;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Aplicacion.Validadores;
using Xunit;

public sealed class ValidadoresYSerializacionTests
{
    [Fact]
    public void AlternarModoMockRequestValidator_InstanciaValida_ValidaSinErrores()
    {
        // Arrange
        var validador = new AlternarModoMockRequestValidator();
        var solicitud = new AlternarModoMockRequest { UsarMock = true };

        // Act
        var resultado = validador.Validate(solicitud);

        // Assert
        Assert.True(resultado.IsValid);
        Assert.Empty(resultado.Errors);
    }

    [Fact]
    public void ContextoSerializacionTelemetria_SerializacionUtf8_ProduceJsonValido()
    {
        // Arrange
        var cabecera = new CabeceraMensajeWsDto
        {
            Tipo = "TELEMETRY_PACKET"
        };

        // Act: Serialización optimizada con Source Generator
        string json = JsonSerializer.Serialize(cabecera, ContextoSerializacionTelemetria.Default.CabeceraMensajeWsDto);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"type\":\"TELEMETRY_PACKET\"", json);
    }

    [Fact]
    public void RespuestaApi_ConstruirExito_AsignaCamposCorrectamente()
    {
        // Arrange & Act
        var respuesta = RespuestaApi<string>.ConstruirExito("Operación completada");

        // Assert
        Assert.True(respuesta.Exitoso);
        Assert.Equal("Operación completada", respuesta.Datos);
        Assert.Empty(respuesta.Errores);
    }

    [Fact]
    public void RespuestaApi_ConstruirFallo_AsignaErroresCorrectamente()
    {
        // Arrange & Act
        var respuesta = RespuestaApi<string>.ConstruirFallo("Recurso no encontrado");

        // Assert
        Assert.False(respuesta.Exitoso);
        Assert.Null(respuesta.Datos);
        Assert.Single(respuesta.Errores);
        Assert.Equal("Recurso no encontrado", respuesta.Errores[0]);
    }

    [Fact]
    public void PaginacionRespuesta_PaginaUnica_CalculaBanderasDeNavegacion()
    {
        // Arrange & Act
        var paginacion = new PaginacionRespuesta<int>
        {
            Elementos = new List<int> { 1, 2, 3 },
            TotalRegistros = 3,
            PaginaActual = 1,
            TamanioPagina = 10
        };

        // Assert
        Assert.Equal(1, paginacion.TotalPaginas);
        Assert.False(paginacion.TienePaginaAnterior);
        Assert.False(paginacion.TienePaginaSiguiente);
        Assert.Equal(3, paginacion.Elementos.Count);
    }
}
