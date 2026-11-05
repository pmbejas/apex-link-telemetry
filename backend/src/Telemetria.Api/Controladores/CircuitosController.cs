namespace Telemetria.Api.Controladores;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telemetria.Api.Autorizacion;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Dtos.Responses;

/// <summary>
/// Controlador REST para servir metadatos oficiales y calibración cartográfica de circuitos FAPPE 2026.
/// </summary>
[ApiController]
[Route("api/circuitos")]
[Produces("application/json")]
public sealed class CircuitosController : ControllerBase
{
    private static readonly List<CircuitoDetalleDto> CircuitosFappe = new()
    {
        new CircuitoDetalleDto
        {
            Id = "monza_f1_2025",
            Nombre = "Autodromo Nazionale Monza",
            Pais = "Italia",
            CodigoPais = "IT",
            LongitudMetros = 5793.0f,
            CantidadCurvas = 11,
            RecordVuelta = "01:21.046",
            PilotoRecord = "Rubens Barrichello (Ferrari F2004)",
            ZonasDrsCount = 2,
            EsOficialFappe = true,
            Descripcion = "El Templo de la Velocidad. Trazado ultra-rápido de baja carga aerodinámica y fuertes frenadas chicaneras.",
            Variante = "Grand Prix (F1 2025)",
            Escala = 1.4f,
            XOffset = 232.866f,
            ZOffset = 1297.7f,
            AnchoPista = 927.422f,
            AltoPista = 1575.82f,
            ArchivoMapa = "map.png"
        },
        new CircuitoDetalleDto
        {
            Id = "fn_imola",
            Nombre = "Autodromo Internazionale Enzo e Dino Ferrari",
            Pais = "Italia",
            CodigoPais = "IT",
            LongitudMetros = 4909.0f,
            CantidadCurvas = 19,
            RecordVuelta = "01:15.484",
            PilotoRecord = "Lewis Hamilton (Mercedes W11)",
            ZonasDrsCount = 1,
            EsOficialFappe = false,
            Descripcion = "Trazado técnico y ondulado clásico en sentido antihorario con chicanas exigentes como Acque Minerali y Variante Alta.",
            Variante = "F1 2025",
            Escala = 1.0f,
            XOffset = 952.928f,
            ZOffset = 533.051f,
            AnchoPista = 1847.0f,
            AltoPista = 1035.0f,
            ArchivoMapa = "map.png"
        },
        new CircuitoDetalleDto
        {
            Id = "ks_red_bull_ring",
            Nombre = "Red Bull Ring",
            Pais = "Austria",
            CodigoPais = "AT",
            LongitudMetros = 4318.0f,
            CantidadCurvas = 10,
            RecordVuelta = "01:05.619",
            PilotoRecord = "Carlos Sainz (McLaren MCL35)",
            ZonasDrsCount = 3,
            EsOficialFappe = false,
            Descripcion = "Trazado corto y dinámico en las colinas de Spielberg, con 3 rectas consecutivas de DRS y fuertes cambios de elevación.",
            Variante = "Grand Prix",
            Escala = 1.0f,
            XOffset = 620.875f,
            ZOffset = 401.055f,
            AnchoPista = 1296.6f,
            AltoPista = 822.72f,
            ArchivoMapa = "map.png"
        }
    };

    [HttpGet]
    [HasPermission("estado:leer")]
    [ProducesResponseType(typeof(RespuestaApi<List<CircuitoDto>>), StatusCodes.Status200OK)]
    public ActionResult<RespuestaApi<List<CircuitoDto>>> ObtenerCircuitos()
    {
        var resumenes = CircuitosFappe.Cast<CircuitoDto>().ToList();
        return Ok(RespuestaApi<List<CircuitoDto>>.ConstruirExito(resumenes));
    }

    [HttpGet("{id}")]
    [HasPermission("estado:leer")]
    [ProducesResponseType(typeof(RespuestaApi<CircuitoDetalleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi<CircuitoDetalleDto>), StatusCodes.Status404NotFound)]
    public ActionResult<RespuestaApi<CircuitoDetalleDto>> ObtenerCircuitoPorId(string id)
    {
        var circuito = CircuitosFappe.FirstOrDefault(c =>
            string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase) ||
            (id.Equals("monza", StringComparison.OrdinalIgnoreCase) && c.Id == "monza_f1_2025") ||
            (id.Equals("imola", StringComparison.OrdinalIgnoreCase) && c.Id == "fn_imola") ||
            (id.Equals("redbullring", StringComparison.OrdinalIgnoreCase) && c.Id == "ks_red_bull_ring")
        );

        if (circuito == null)
        {
            return NotFound(RespuestaApi<CircuitoDetalleDto>.ConstruirFallo(
                $"No se encontró el circuito con identificador '{id}'."));
        }

        return Ok(RespuestaApi<CircuitoDetalleDto>.ConstruirExito(circuito));
    }
}
