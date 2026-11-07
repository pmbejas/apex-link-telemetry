namespace Telemetria.Aplicacion.Servicios;

using System;

/// <summary>
/// Motor de cálculo de delta en tiempo real basado en el mapeo espacial (Reference Lap Trajectory Map)
/// de la mejor vuelta de la sesión activa (1000 segmentos de progreso de pista con interpolación sub-segmento).
/// Evita el error físico de asumir velocidad constante alrededor del circuito.
/// </summary>
public sealed class CalculadorDeltaTrayectoria
{
    public const int RESOLUCION_SEGMENTOS = 1000;
    private const float UMBRAL_ZONA_INICIO_PISTA = 0.05f;
    private const float UMBRAL_ZONA_META_PISTA = 0.95f;
    private const int TIEMPO_MAXIMO_ZONA_INICIO_MS = 15000;
    private const int TIEMPO_MINIMO_ZONA_META_MS = 5000;
    private const int MINIMO_MUESTRAS_PARA_PERFIL_VALIDO = 15;
    private const int INDICE_MAXIMO_PRIMER_REGISTRO = 80; // La vuelta debe haber iniciado en el primer 8% del trazado

    private readonly int[] _tiemposVueltaEnCursoMs = new int[RESOLUCION_SEGMENTOS];
    private int[]? _perfilMejorVueltaMs;

    private int _ultimoIndiceRegistrado = -1;
    private int _ultimoTiempoRegistradoMs = 0;
    private int _cantidadSegmentosPoblados = 0;
    private int _primerIndiceRegistrado = -1;
    private int _mejorTiempoReferenciaMs = 0;
    private int _ultimoDeltaCalculadoMs = 0;

    public bool TienePerfilReferencia => _perfilMejorVueltaMs != null && _mejorTiempoReferenciaMs > 0;

    public int MejorTiempoReferenciaMs => _mejorTiempoReferenciaMs;

    /// <summary>
    /// Reinicia por completo tanto la vuelta en curso como la referencia de mejor vuelta de la sesión.
    /// </summary>
    public void ReiniciarSesion()
    {
        ReiniciarVueltaEnCurso();
        _perfilMejorVueltaMs = null;
        _mejorTiempoReferenciaMs = 0;
        _ultimoDeltaCalculadoMs = 0;
    }

    /// <summary>
    /// Limpia únicamente el buffer de la vuelta actual (ej. al cruzar meta, abortar giro o volver a boxes).
    /// </summary>
    public void ReiniciarVueltaEnCurso()
    {
        Array.Clear(_tiemposVueltaEnCursoMs, 0, _tiemposVueltaEnCursoMs.Length);
        _ultimoIndiceRegistrado = -1;
        _ultimoTiempoRegistradoMs = 0;
        _cantidadSegmentosPoblados = 0;
        _primerIndiceRegistrado = -1;
        _ultimoDeltaCalculadoMs = 0;
    }

    /// <summary>
    /// Registra una muestra espacial-temporal de la vuelta actual.
    /// Incluye protecciones contra el desfase de 1 cuadro de Assetto Corsa al cruzar la línea de sentencia.
    /// </summary>
    public void RegistrarMuestra(float progresoNormalizado, int tiempoVueltaActualMs, bool enBoxes = false)
    {
        if (enBoxes || tiempoVueltaActualMs <= 0 || float.IsNaN(progresoNormalizado))
        {
            return;
        }

        float progreso = Math.Clamp(progresoNormalizado, 0.0f, 0.9999f);

        // Protección contra desincronización de fin de vuelta en memoria compartida de AC:
        // 1. El progreso ya cruzó a 0.001 pero ICurrentTime aún conserva los ~80,000 ms de la vuelta anterior
        if (progreso < UMBRAL_ZONA_INICIO_PISTA && tiempoVueltaActualMs > TIEMPO_MAXIMO_ZONA_INICIO_MS)
        {
            return;
        }

        // 2. El reloj ya se reinició a ~10 ms pero el progreso aún figura en 0.999
        if (progreso > UMBRAL_ZONA_META_PISTA && tiempoVueltaActualMs < TIEMPO_MINIMO_ZONA_META_MS)
        {
            return;
        }

        // Detectar si el reloj de la vuelta retrocedió abruptamente (ej. reinicio rápido / vuelta a boxes)
        if (_ultimoTiempoRegistradoMs > 3000 && tiempoVueltaActualMs < _ultimoTiempoRegistradoMs - 2000)
        {
            ReiniciarVueltaEnCurso();
        }

        int indice = Math.Clamp((int)(progreso * RESOLUCION_SEGMENTOS), 0, RESOLUCION_SEGMENTOS - 1);

        // Evitar sobrescribir si el auto retrocede o si en el último cuadro antes del cierre el progreso envuelve a 0
        if (_ultimoIndiceRegistrado != -1 && indice < _ultimoIndiceRegistrado)
        {
            return;
        }

        if (tiempoVueltaActualMs < _ultimoTiempoRegistradoMs)
        {
            return;
        }

        if (_tiemposVueltaEnCursoMs[indice] == 0)
        {
            _tiemposVueltaEnCursoMs[indice] = tiempoVueltaActualMs;
            _cantidadSegmentosPoblados++;
            if (_primerIndiceRegistrado == -1)
            {
                _primerIndiceRegistrado = indice;
            }
        }

        _ultimoIndiceRegistrado = indice;
        _ultimoTiempoRegistradoMs = tiempoVueltaActualMs;
    }

    /// <summary>
    /// Consolida la vuelta recién cerrada. Si es récord válido y cuenta con cobertura espacial completa,
    /// construye el mapa interpolado de 1000 puntos como nueva referencia de la sesión.
    /// </summary>
    public bool ConsolidarCierreDeVuelta(int tiempoVueltaCerradaMs, bool esVueltaValida)
    {
        bool seActualizoPerfil = false;

        if (esVueltaValida &&
            tiempoVueltaCerradaMs > 0 &&
            (_mejorTiempoReferenciaMs == 0 || tiempoVueltaCerradaMs < _mejorTiempoReferenciaMs))
        {
            _mejorTiempoReferenciaMs = tiempoVueltaCerradaMs;

            if (_cantidadSegmentosPoblados >= MINIMO_MUESTRAS_PARA_PERFIL_VALIDO &&
                _primerIndiceRegistrado >= 0 &&
                _primerIndiceRegistrado <= INDICE_MAXIMO_PRIMER_REGISTRO)
            {
                _perfilMejorVueltaMs = ConstruirPerfilInterpolado(_tiemposVueltaEnCursoMs, tiempoVueltaCerradaMs);
                seActualizoPerfil = true;
            }
        }

        ReiniciarVueltaEnCurso();
        return seActualizoPerfil;
    }

    /// <summary>
    /// Calcula el delta instantáneo en milisegundos comparando el tiempo actual contra el tiempo exacto
    /// en que la vuelta récord pasó por la misma coordenada normalizada del circuito.
    /// </summary>
    public int CalcularDeltaMs(float progresoNormalizado, int tiempoVueltaActualMs)
    {
        if (_perfilMejorVueltaMs == null ||
            _mejorTiempoReferenciaMs <= 0 ||
            tiempoVueltaActualMs <= 0 ||
            float.IsNaN(progresoNormalizado))
        {
            return 0;
        }

        float progreso = Math.Clamp(progresoNormalizado, 0.0f, 0.9999f);

        // Filtrar el cuadro de transición en línea de sentencia donde progreso y reloj aún no están alineados
        if (progreso < UMBRAL_ZONA_INICIO_PISTA && tiempoVueltaActualMs > TIEMPO_MAXIMO_ZONA_INICIO_MS)
        {
            return 0;
        }

        if (progreso > UMBRAL_ZONA_META_PISTA && tiempoVueltaActualMs < TIEMPO_MINIMO_ZONA_META_MS)
        {
            return _ultimoDeltaCalculadoMs;
        }

        double posicionContinua = progreso * RESOLUCION_SEGMENTOS;
        int indiceBase = Math.Clamp((int)posicionContinua, 0, RESOLUCION_SEGMENTOS - 1);
        double fraccion = posicionContinua - indiceBase;

        int tiempoRefBase = _perfilMejorVueltaMs[indiceBase];
        int tiempoRefSiguiente = (indiceBase + 1 < RESOLUCION_SEGMENTOS)
            ? _perfilMejorVueltaMs[indiceBase + 1]
            : _mejorTiempoReferenciaMs;

        if (tiempoRefSiguiente < tiempoRefBase)
        {
            tiempoRefSiguiente = tiempoRefBase;
        }

        int tiempoReferenciaInterpoladoMs = (int)Math.Round(tiempoRefBase + (tiempoRefSiguiente - tiempoRefBase) * fraccion);
        if (tiempoReferenciaInterpoladoMs <= 0)
        {
            return 0;
        }

        int deltaMs = tiempoVueltaActualMs - tiempoReferenciaInterpoladoMs;
        _ultimoDeltaCalculadoMs = deltaMs;
        return deltaMs;
    }

    private static int[] ConstruirPerfilInterpolado(int[] muestrasCrudas, int tiempoFinalVueltaMs)
    {
        int[] perfil = new int[RESOLUCION_SEGMENTOS];
        int ultimoIndiceValido = 0;
        int ultimoTiempoValido = 0;

        for (int i = 0; i < RESOLUCION_SEGMENTOS; i++)
        {
            int muestra = muestrasCrudas[i];
            if (muestra > 0)
            {
                int muestraAcotada = Math.Min(muestra, tiempoFinalVueltaMs);
                if (muestraAcotada >= ultimoTiempoValido)
                {
                    int distanciaIndices = i - ultimoIndiceValido;
                    if (distanciaIndices > 1)
                    {
                        int diferenciaTiempo = muestraAcotada - ultimoTiempoValido;
                        for (int paso = 1; paso < distanciaIndices; paso++)
                        {
                            perfil[ultimoIndiceValido + paso] = ultimoTiempoValido +
                                (int)Math.Round((double)diferenciaTiempo * paso / distanciaIndices);
                        }
                    }

                    perfil[i] = muestraAcotada;
                    ultimoIndiceValido = i;
                    ultimoTiempoValido = muestraAcotada;
                }
            }
        }

        if (ultimoIndiceValido < RESOLUCION_SEGMENTOS - 1)
        {
            int distanciaFinal = (RESOLUCION_SEGMENTOS - 1) - ultimoIndiceValido;
            int diferenciaFinal = Math.Max(0, tiempoFinalVueltaMs - ultimoTiempoValido);
            for (int paso = 1; paso <= distanciaFinal; paso++)
            {
                perfil[ultimoIndiceValido + paso] = ultimoTiempoValido +
                    (int)Math.Round((double)diferenciaFinal * paso / distanciaFinal);
            }
        }

        return perfil;
    }
}
