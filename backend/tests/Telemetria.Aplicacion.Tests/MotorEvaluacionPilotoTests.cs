namespace Telemetria.Aplicacion.Tests;

using System;
using System.Collections.Generic;
using Telemetria.Aplicacion.Servicios;
using Telemetria.Dominio.Modelos;
using Xunit;

public sealed class MotorEvaluacionPilotoTests
{
    private readonly MotorEvaluacionPiloto _motor = new();

    [Fact]
    public void Evaluar_SesionConVueltasValidasYSmoothBraking_RetornaPuntajesAltos()
    {
        // Arrange: Sesión limpia en Monza, 4 vueltas válidas consistentes
        var usuarioId = Guid.NewGuid();
        var sesion = new Sesion
        {
            Id = Guid.NewGuid(),
            Circuito = "Autodromo Nazionale Monza",
            ModeloVehiculo = "Ferrari SF70H",
            TotalVueltas = 4,
            MejorTiempoMs = 81240
        };

        var vueltas = new List<Vuelta>
        {
            new() { NumeroVuelta = 1, TiempoVueltaMs = 81500, Sector1Ms = 27100, Sector2Ms = 27200, Sector3Ms = 27200, EsValida = true },
            new() { NumeroVuelta = 2, TiempoVueltaMs = 81240, Sector1Ms = 27000, Sector2Ms = 27140, Sector3Ms = 27100, EsValida = true, EsMejorVuelta = true },
            new() { NumeroVuelta = 3, TiempoVueltaMs = 81400, Sector1Ms = 27050, Sector2Ms = 27200, Sector3Ms = 27150, EsValida = true },
            new() { NumeroVuelta = 4, TiempoVueltaMs = 81350, Sector1Ms = 27020, Sector2Ms = 27180, Sector3Ms = 27150, EsValida = true },
        };

        // Generar muestras sintéticas de trail-braking suave (rampa descendente mientras gira)
        var muestras = new List<MuestraTelemetria>();
        for (int i = 0; i < 100; i++)
        {
            float dist = i * 50f;
            float freno = i < 20 ? 1.0f : Math.Max(0.0f, 1.0f - ((i - 20) * 0.05f)); // Descompresión suave
            float gas = i > 40 ? Math.Min(1.0f, (i - 40) * 0.04f) : 0f; // Tracción progresiva
            float gLat = i >= 20 && i <= 50 ? 2.8f : 0.2f;
            float vel = i < 35 ? Math.Max(80f, 320f - (i * 7f)) : Math.Min(310f, 80f + ((i - 35) * 6f));

            muestras.Add(new MuestraTelemetria
            {
                DistanciaPistaMetros = dist,
                VelocidadKmh = vel,
                Freno = freno,
                Acelerador = gas,
                FuerzaGLat = gLat,
                FuerzaGLon = i < 30 ? -2.5f : (i > 40 ? 1.5f : 0f)
            });
        }

        // Act
        var evaluacion = _motor.EvaluarSesion(sesion, vueltas, muestras, usuarioId);

        // Assert
        Assert.NotNull(evaluacion);
        Assert.Equal(usuarioId, evaluacion.UsuarioId);
        Assert.Equal(100, evaluacion.RespetoLimites); // 4 de 4 vueltas válidas
        Assert.InRange(evaluacion.TrailBraking, 80, 100);
        Assert.InRange(evaluacion.TraccionSalida, 80, 100);
        Assert.InRange(evaluacion.Consistencia, 85, 100);
        Assert.InRange(evaluacion.Velocidad, 80, 100);
        Assert.False(string.IsNullOrWhiteSpace(evaluacion.PuntoFuerteTitulo));
        Assert.False(string.IsNullOrWhiteSpace(evaluacion.OportunidadMejoraTitulo));
    }

    [Fact]
    public void Evaluar_SesionConVueltasInvalidadasPorLimites_PenalizaRespetoLimites()
    {
        // Arrange: 4 vueltas pero solo 1 es válida
        var usuarioId = Guid.NewGuid();
        var sesion = new Sesion { Id = Guid.NewGuid(), TotalVueltas = 4 };
        var vueltas = new List<Vuelta>
        {
            new() { NumeroVuelta = 1, TiempoVueltaMs = 82000, EsValida = true },
            new() { NumeroVuelta = 2, TiempoVueltaMs = 80000, EsValida = false }, // Infracción límites
            new() { NumeroVuelta = 3, TiempoVueltaMs = 80100, EsValida = false }, // Infracción límites
            new() { NumeroVuelta = 4, TiempoVueltaMs = 81200, EsValida = false }, // Infracción límites
        };

        var muestras = new List<MuestraTelemetria>
        {
            new() { DistanciaPistaMetros = 100f, VelocidadKmh = 200f, Freno = 0f, Acelerador = 1f }
        };

        // Act
        var evaluacion = _motor.EvaluarSesion(sesion, vueltas, muestras, usuarioId);

        // Assert
        Assert.Equal(25, evaluacion.RespetoLimites); // 1 de 4 vueltas válidas = 25%
    }

    [Fact]
    public void Evaluar_FrenadaEnEscalonYJitter_PenalizaTrailBraking()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var sesion = new Sesion { Id = Guid.NewGuid(), TotalVueltas = 1 };
        var vueltas = new List<Vuelta> { new() { NumeroVuelta = 1, TiempoVueltaMs = 85000, EsValida = true } };

        // Muestras con freno en serrucho / escalonado / bombeo brusco en curva
        var muestrasJitter = new List<MuestraTelemetria>();
        for (int i = 0; i < 40; i++)
        {
            // Repite freno 0 -> 1 -> 0 -> 1 mientras gira (bombeo / rebote)
            float freno = (i % 4 == 0) ? 0.9f : ((i % 4 == 2) ? 0.1f : 0.6f);
            muestrasJitter.Add(new MuestraTelemetria
            {
                DistanciaPistaMetros = i * 20f,
                VelocidadKmh = 140f,
                Freno = freno,
                Acelerador = 0f,
                FuerzaGLat = 2.0f
            });
        }

        // Act
        var evaluacion = _motor.EvaluarSesion(sesion, vueltas, muestrasJitter, usuarioId);

        // Assert: el trail braking debe estar fuertemente penalizado por bombeo de pedal
        Assert.True(evaluacion.TrailBraking < 75, $"TrailBraking esperado < 75 pero fue {evaluacion.TrailBraking}");
    }
}
