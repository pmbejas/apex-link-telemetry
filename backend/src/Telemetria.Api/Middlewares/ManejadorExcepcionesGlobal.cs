namespace Telemetria.Api.Middlewares;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware global para el tratamiento centralizado de excepciones no controladas según el estándar RFC 7807 (ProblemDetails).
/// Sanitiza los mensajes al usuario final sin exponer trazas de ejecución internas ni stack traces.
/// </summary>
public sealed class ManejadorExcepcionesGlobal
{
    private static readonly JsonSerializerOptions OpcionesSerializacionProblemDetails = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorExcepcionesGlobal> _logger;

    public ManejadorExcepcionesGlobal(
        RequestDelegate siguiente,
        ILogger<ManejadorExcepcionesGlobal> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Excepción no controlada interceptada durante la petición HTTP: {Ruta}", contexto.Request.Path);
            await TratarExcepcionAsync(contexto, excepcion);
        }
    }

    private static async Task TratarExcepcionAsync(HttpContext contexto, Exception excepcion)
    {
        if (contexto.Response.HasStarted)
        {
            return;
        }

        contexto.Response.ContentType = "application/problem+json";

        var (codigoEstado, titulo, detalleSeguro) = excepcion switch
        {
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "Recurso no encontrado",
                "El recurso solicitado no existe en el registro local de telemetría."),
            ArgumentException => (
                HttpStatusCode.BadRequest,
                "Parámetros de solicitud incorrectos",
                "Los parámetros proporcionados en la solicitud no son válidos o están fuera de rango."),
            TimeoutException => (
                HttpStatusCode.GatewayTimeout,
                "Tiempo de espera agotado",
                "La operación excedió el tiempo máximo de respuesta permitido."),
            _ => (
                HttpStatusCode.InternalServerError,
                "Error interno del servidor de telemetría",
                "Se ha producido un error interno no controlado en el servidor de telemetría.")
        };

        contexto.Response.StatusCode = (int)codigoEstado;

        var respuestaFallo = new ProblemDetails
        {
            Status = (int)codigoEstado,
            Title = titulo,
            Detail = detalleSeguro,
            Instance = contexto.Request.Path,
            Extensions = { ["traceId"] = contexto.TraceIdentifier }
        };

        await contexto.Response.WriteAsync(
            JsonSerializer.Serialize(respuestaFallo, OpcionesSerializacionProblemDetails));
    }
}
