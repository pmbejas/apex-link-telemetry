namespace Telemetria.Aplicacion.Contratos;

using System;
using System.Collections.Generic;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Dominio.Modelos;

/// <summary>
/// Contrato para el motor determinístico de análisis de telemetría y evaluación de rendimiento del piloto.
/// </summary>
public interface IMotorEvaluacionPiloto
{
    /// <summary>
    /// Analiza las vueltas y telemetría de una sesión y genera la evaluación completa de 5 ejes,
    /// habilidades y diagnóstico de ingeniería de pista.
    /// </summary>
    EvaluacionRendimientoDto EvaluarSesion(
        Sesion sesion,
        IReadOnlyList<Vuelta> vueltas,
        IReadOnlyList<MuestraTelemetria> telemetriaReferencia,
        Guid usuarioId);
}
