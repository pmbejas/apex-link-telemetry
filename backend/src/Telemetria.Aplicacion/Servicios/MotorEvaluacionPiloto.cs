namespace Telemetria.Aplicacion.Servicios;

using System;
using System.Collections.Generic;
using System.Linq;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Dtos.Responses;
using Telemetria.Dominio.Modelos;

/// <summary>
/// Motor determinístico que procesa las muestras de telemetría a 60 Hz y las vueltas oficiales
/// para calcular las 5 dimensiones del radar, habilidades técnicas y diagnósticos de AI Coach.
/// </summary>
public sealed class MotorEvaluacionPiloto : IMotorEvaluacionPiloto
{
    private const double UMBRAL_OVERLAP_G_LAT = 0.8;
    private const float UMBRAL_FRENO_ACTIVO = 0.10f;
    private const float UMBRAL_JITTER_FRENO = 0.15f;

    public EvaluacionRendimientoDto EvaluarSesion(
        Sesion sesion,
        IReadOnlyList<Vuelta> vueltas,
        IReadOnlyList<MuestraTelemetria> telemetriaReferencia,
        Guid usuarioId)
    {
        // 1. Respeto de Límites e Incidentes (estrictamente evaluado sobre EsValida)
        int respetoLimites = CalcularRespetoLimites(vueltas);

        // 2. Consistencia cronométrica entre vueltas válidas
        int consistencia = CalcularConsistencia(vueltas);

        // 3. Trail Braking y Potencia de Frenada
        var (trailBraking, frenada) = CalcularFrenadaYTrailBraking(telemetriaReferencia);

        // 4. Tracción / Salida de curva
        int traccion = CalcularTraccion(telemetriaReferencia);

        // 5. Velocidad en Ápice y Velocidad Punta
        int velocidadApice = CalcularVelocidadApice(telemetriaReferencia, sesion);
        int velocidad = Math.Clamp((int)Math.Round((velocidadApice * 0.6) + (traccion * 0.4)), 50, 100);

        // 6. Control y Agresividad
        int control = Math.Clamp((int)Math.Round((frenada * 0.4) + (consistencia * 0.4) + (respetoLimites * 0.2)), 40, 100);
        int agresividad = Math.Clamp((int)Math.Round((velocidad * 0.5) + (frenada * 0.3) + (trailBraking * 0.2)), 50, 98);

        // 7. Diagnóstico Heurístico del AI Race Coach
        var (pfTitulo, pfDesc, omTitulo, omDesc) = GenerarDiagnosticosCoach(
            vueltas, trailBraking, traccion, respetoLimites, sesion.Circuito);

        // 8. Consejos por Habilidad
        string consejoFrenada = trailBraking >= 90
            ? "Excelente modulación y descompresión lineal de frenada al aproximar el vértice."
            : "Liberás el pedal de freno de forma escalonada en los últimos 25 metros. Suavizá la descompresión para evitar subviraje en el vértice.";

        string consejoTraccion = traccion >= 90
            ? "Apertura progresiva y limpia del acelerador maximizando la tracción sin patinaje."
            : "Picos abruptos al acelerar. Aplica el pedal en una rampa progresiva para no sobrecargar el compuesto trasero.";

        string consejoApice = velocidadApice >= 90
            ? "Velocidad mínima en el vértice de nivel superior aprovechando toda la carga aerodinámica."
            : "Llevás velocidad sobrante a la entrada y la perdés en el centro de la curva. Frená 5 metros antes para asegurar mayor rotación.";

        string consejoConsistencia = consistencia >= 90
            ? "Ritmo de carrera quirúrgico con dispersión mínima entre vueltas consecutivas."
            : "Dispersión de tiempos superior a ±0.6 s. Concentrate en fijar referencias visuales de frenada idénticas.";

        string consejoLimites = respetoLimites >= 90
            ? "Aprovechamiento óptimo de pianos y límites reglamentarios sin invalidar giros."
            : "Demasiadas vueltas invalidadas por superar los límites de pista (4 ruedas fuera). Mantené al menos 2 ruedas sobre el asfalto.";

        return new EvaluacionRendimientoDto
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            SesionId = sesion.Id,
            Velocidad = velocidad,
            Consistencia = consistencia,
            Frenada = frenada,
            Control = control,
            Agresividad = agresividad,
            TrailBraking = trailBraking,
            TraccionSalida = traccion,
            VelocidadApice = velocidadApice,
            RespetoLimites = respetoLimites,
            PuntoFuerteTitulo = pfTitulo,
            PuntoFuerteDesc = pfDesc,
            OportunidadMejoraTitulo = omTitulo,
            OportunidadMejoraDesc = omDesc,
            ConsejoFrenada = consejoFrenada,
            ConsejoTraccion = consejoTraccion,
            ConsejoApice = consejoApice,
            ConsejoConsistencia = consejoConsistencia,
            ConsejoLimites = consejoLimites,
            FechaCreacion = DateTimeOffset.UtcNow
        };
    }

    private static int CalcularRespetoLimites(IReadOnlyList<Vuelta> vueltas)
    {
        if (vueltas.Count == 0) return 85;

        int validas = vueltas.Count(v => v.EsValida);
        double ratio = (double)validas / vueltas.Count;
        return Math.Clamp((int)Math.Round(ratio * 100.0), 0, 100);
    }

    private static int CalcularConsistencia(IReadOnlyList<Vuelta> vueltas)
    {
        var validasConTiempo = vueltas
            .Where(v => v.EsValida && v.TiempoVueltaMs > 0)
            .Select(v => v.TiempoVueltaMs / 1000.0)
            .ToList();

        if (validasConTiempo.Count < 2) return 88;

        double promedio = validasConTiempo.Average();
        double sumaCuadrados = validasConTiempo.Sum(t => Math.Pow(t - promedio, 2));
        double desviacion = Math.Sqrt(sumaCuadrados / validasConTiempo.Count);

        // Desviación en segundos: <= 0.15s es élite, >= 1.5s es amateur
        if (desviacion <= 0.15) return 97;
        if (desviacion <= 0.35) return 93;
        if (desviacion <= 0.60) return 88;
        if (desviacion <= 1.20) return 79;
        return Math.Clamp((int)Math.Round(100.0 - (desviacion * 25.0)), 40, 75);
    }

    private static (int TrailBraking, int Frenada) CalcularFrenadaYTrailBraking(IReadOnlyList<MuestraTelemetria> muestras)
    {
        if (muestras.Count < 20) return (85, 85);

        int muestrasConGiro = 0;
        int conteoJitter = 0;
        float frenoPrevio = 0f;

        for (int i = 0; i < muestras.Count; i++)
        {
            var m = muestras[i];
            bool girando = Math.Abs(m.FuerzaGLat) >= UMBRAL_OVERLAP_G_LAT;
            bool frenando = m.Freno >= UMBRAL_FRENO_ACTIVO;

            if (girando && frenando)
            {
                muestrasConGiro++;

                // Detectar re-pisada brusca durante el giro (rebote/jitter)
                if (frenoPrevio > 0.05f && (m.Freno - frenoPrevio) > UMBRAL_JITTER_FRENO)
                {
                    conteoJitter++;
                }
            }

            frenoPrevio = m.Freno;
        }

        // Si no hay muestras con giro simultáneo a frenada, trail braking es bajo
        if (muestrasConGiro == 0)
        {
            return (65, 75);
        }

        // Penalizar por conteo de bombeos/jitter en curva
        int trailBraking = Math.Clamp(96 - (conteoJitter * 9), 35, 99);
        int frenada = Math.Clamp((int)Math.Round((trailBraking * 0.9) + 8), 40, 98);

        return (trailBraking, frenada);
    }

    private static int CalcularTraccion(IReadOnlyList<MuestraTelemetria> muestras)
    {
        if (muestras.Count < 20) return 88;

        int aperturasAcelerador = 0;
        int cortesBruscos = 0;
        float acelPrevio = 0f;

        for (int i = 0; i < muestras.Count; i++)
        {
            var m = muestras[i];
            if (m.Acelerador > 0.1f)
            {
                aperturasAcelerador++;
                // Caída repentina de acelerador en plena fase de aceleración (corrección por patinaje)
                if (acelPrevio > 0.6f && (acelPrevio - m.Acelerador) > 0.35f && m.Freno < 0.05f)
                {
                    cortesBruscos++;
                }
            }
            acelPrevio = m.Acelerador;
        }

        if (aperturasAcelerador == 0) return 80;
        return Math.Clamp(95 - (cortesBruscos * 7), 50, 98);
    }

    private static int CalcularVelocidadApice(IReadOnlyList<MuestraTelemetria> muestras, Sesion sesion)
    {
        if (muestras.Count < 20) return 90;

        float maxVel = muestras.Max(m => m.VelocidadKmh);
        // Si superó 300 km/h (F1/Monza), el paso de ápice suele ser muy alto
        if (maxVel > 300) return 96;
        if (maxVel > 260) return 90;
        if (maxVel > 220) return 84;
        return 78;
    }

    private static (string PfTitulo, string PfDesc, string OmTitulo, string OmDesc) GenerarDiagnosticosCoach(
        IReadOnlyList<Vuelta> vueltas,
        int trailBraking,
        int traccion,
        int respetoLimites,
        string circuito)
    {
        string nombreCircuito = string.IsNullOrWhiteSpace(circuito) ? "Monza" : circuito;

        string pfTitulo;
        string pfDesc;
        string omTitulo;
        string omDesc;

        if (traccion >= trailBraking)
        {
            pfTitulo = $"Tracción en Salida de Curva ({nombreCircuito})";
            pfDesc = "Apertura progresiva y limpia del acelerador al salir de curvas de media velocidad. Cero patinaje y excelente cuidado del compuesto.";
        }
        else
        {
            pfTitulo = $"Modulación de Frenada y Trail-Braking ({nombreCircuito})";
            pfDesc = "Excelente presión de freno inicial combinada con apoyo lateral firme en la fase de desaceleración previa al ápice.";
        }

        if (respetoLimites < 80)
        {
            omTitulo = "Respeto de Límites de Pista (Track Limits)";
            omDesc = "Se detectaron excursiones de 4 ruedas fuera del asfalto o corte de chicana. Ajustá las referencias de entrada para no invalidar el giro.";
        }
        else if (trailBraking < traccion)
        {
            omTitulo = "Transición de Freno a Ápice (Trail-Braking)";
            omDesc = "Liberación del pedal de freno algo abrupta en los últimos 25 metros antes del vértice, induciendo subviraje transitorio en la entrada.";
        }
        else
        {
            omTitulo = "Optimización de Aceleración a la Salida";
            omDesc = "Podés anticipar la apertura de acelerador unos metros en curvas lentas manteniendo la progresividad para ganar delta en la recta.";
        }

        return (pfTitulo, pfDesc, omTitulo, omDesc);
    }
}
