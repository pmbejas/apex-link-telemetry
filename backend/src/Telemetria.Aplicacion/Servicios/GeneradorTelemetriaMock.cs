namespace Telemetria.Aplicacion.Servicios;

using System;
using Telemetria.Dominio.Contratos.Hardware;
using Telemetria.Dominio.Modelos;

public sealed class GeneradorTelemetriaMock : ILectorTelemetria
{
    private const double LONGITUD_MONZA_METROS = 5793.0;
    private const double VELOCIDAD_MAXIMA_MS = 325.0 / 3.6;
    private const double VELOCIDAD_MINIMA_MS = 72.0 / 3.6;
    private const double PASO_TIEMPO_DELTA = 1.0 / 60.0;
    private const double ACELERACION_GRAVEDAD_MS2 = 9.80665;
    private const double BATALLA_MONOPLAZA_METROS = 3.6;
    private const double GANANCIA_DIRECCION_ACKERMANN = 4.0;
    private const double LIMITE_ADHERENCIA_KAMM_G = 2.8;
    private const double LIMITE_FUERZA_LATERAL_G = 2.4;
    private const double ACELERACION_TRACCION_MAXIMA_G = 1.25;
    private const double DESACELERACION_FRENADO_MAXIMA_G = 2.8;
    private const double DESACELERACION_CORTE_ENCENDIDO_G = -0.05;

    private const int TIEMPO_REFERENCIA_MONZA_MS = 81240;
    private const int RPM_RALENTI = 4200;
    private const int RPM_MAXIMAS = 13000;
    private const int RPM_CAMBIO_ASCENDENTE = 12750;
    private const int RPM_CAMBIO_DESCENDENTE = 8200;
    private const double INCREMENTO_RPM_BLIP_REBAJE = 650.0;
    private const int CUADROS_CORTE_ENCENDIDO_UPSHIFT = 3;
    private const int CUADROS_BLIP_REBAJE_DOWNSHIFT = 4;

    private const double LIMITE_PROGRESO_SECTOR_1 = 0.35;
    private const double LIMITE_PROGRESO_SECTOR_2 = 0.70;
    private const float UMBRAL_VELOCIDAD_DRS_DISPONIBLE_KMH = 260.0f;
    private const float UMBRAL_VELOCIDAD_DRS_HABILITADO_KMH = 285.0f;

    private const float TEMPERATURA_AMBIENTE_NEUMATICO_C = 88.0f;
    private const float TEMPERATURA_MINIMA_NEUMATICO_C = 85.0f;
    private const float TEMPERATURA_MAXIMA_NEUMATICO_C = 105.0f;
    private const float PRESION_BASE_NEUMATICO_PSI = 20.5f;
    private const float TEMPERATURA_REFERENCIA_PRESION_C = 75.0f;
    private const float COEFICIENTE_TERMICO_PRESION_PSI = 0.095f;

    // Relaciones de transmisión efectivas K_marcha (RPM por km/h) para las 7 marchas (R1..R7)
    private static readonly double[] FACTORES_MARCHA_RPM_POR_KMH = { 0.0, 165.0, 118.0, 88.0, 68.0, 54.0, 44.5, 39.8 };

    private readonly object _cerrojoSimulacion = new();
    private readonly MetadatosEstaticos _metadatosEstaticos;
    private readonly int[] _tiemposSectoresMs = new int[3];
    private readonly float[] _temperaturasNeumaticosC = { 92.0f, 92.5f, 88.5f, 89.0f };
    private readonly float[] _desgasteNeumaticosPorcentaje = { 100.0f, 100.0f, 100.0f, 100.0f };

    private double _progresoCircuito = 0.0;
    private double _velocidadMetrosPorSegundo = 75.5;
    private int _numeroVueltaActual = 0;
    private int _cuadroVueltaActual = 0;
    private int _ultimoTiempoVueltaMs = TIEMPO_REFERENCIA_MONZA_MS;
    private int _mejorTiempoVueltaMs = TIEMPO_REFERENCIA_MONZA_MS;
    private int _ultimoTiempoSectorCerradoMs = 25690;
    private int _marchaActual = 6;
    private int _cuadrosCorteEncendido = 0;
    private int _cuadrosBlipRebaje = 0;
    private int _ultimoSectorRegistrado = 1;
    private int _tiempoInicioSectorMs = 0;
    private CuadroTelemetria? _ultimoCuadroGenerado;

    public GeneradorTelemetriaMock()
    {
        _metadatosEstaticos = new MetadatosEstaticos
        {
            NombreAuto = "ks_ferrari_sf70h",
            NombrePista = "monza",
            ConfiguracionPista = "grand_prix",
            MaxRpm = RPM_MAXIMAS,
            MaxCombustible = 105.0f,
            CantidadSectores = 3,
            LongitudPistaMetros = (float)LONGITUD_MONZA_METROS,
            VersionSm = "1.7",
            VersionAc = "1.16.4"
        };
    }

    public bool EstaConectado() => true;

    public bool IntentarConectar() => true;

    public void Desconectar() { }

    public void Dispose() { }

    public MetadatosEstaticos ObtenerMetadatosEstaticos() => _metadatosEstaticos;

    public CuadroTelemetria ObtenerUltimoCuadroConocido()
    {
        lock (_cerrojoSimulacion)
        {
            return _ultimoCuadroGenerado ?? CrearCuadroInicialPorDefecto();
        }
    }

    public CuadroTelemetria ObtenerCuadroActual()
    {
        lock (_cerrojoSimulacion)
        {
            _cuadroVueltaActual++;

            double velocidadActualKmh = _velocidadMetrosPorSegundo * 3.6;
            var perfilTramo = EvaluarPerfilSector(_progresoCircuito, velocidadActualKmh);
            var dinamicaChasis = IntegrarDinamicaChasis(perfilTramo.acelerador, perfilTramo.freno, perfilTramo.radioCurva);

            double velocidadActualizadaKmh = _velocidadMetrosPorSegundo * 3.6;
            float[] presionesPsi = ActualizarTermodinamicaNeumaticos(
                dinamicaChasis.aceleracionLateralG,
                dinamicaChasis.aceleracionLongitudinalG,
                perfilTramo.freno);

            var (marcha, revolucionesPorMinuto) = ComputarTrenMotriz(velocidadActualizadaKmh);
            var cronometraje = AvanzarProgresionYCronometraje();
            var coordenadas = ComputarCoordenadasMonza(_progresoCircuito);

            var cuadro = ConstruirCuadroDominio(
                velocidadActualizadaKmh,
                marcha,
                revolucionesPorMinuto,
                perfilTramo.acelerador,
                perfilTramo.freno,
                dinamicaChasis,
                presionesPsi,
                cronometraje,
                coordenadas);

            _ultimoCuadroGenerado = cuadro;
            return cuadro;
        }
    }

    private (double aceleracionLateralG, double aceleracionLongitudinalG, double anguloDireccion) IntegrarDinamicaChasis(
        double acelerador,
        double freno,
        double radioCurva)
    {
        double aceleracionLateralG = 0.0;
        double anguloDireccion = 0.0;

        if (Math.Abs(radioCurva) > 1.0)
        {
            aceleracionLateralG = (Math.Pow(_velocidadMetrosPorSegundo, 2.0) / radioCurva) / ACELERACION_GRAVEDAD_MS2;
            aceleracionLateralG = Math.Clamp(aceleracionLateralG, -LIMITE_FUERZA_LATERAL_G, LIMITE_FUERZA_LATERAL_G);
            anguloDireccion = Math.Clamp(
                Math.Atan(BATALLA_MONOPLAZA_METROS / radioCurva) * GANANCIA_DIRECCION_ACKERMANN,
                -1.0,
                1.0);
        }

        double adherenciaLongitudinalMaximaG = Math.Sqrt(
            Math.Max(0.25, Math.Pow(LIMITE_ADHERENCIA_KAMM_G, 2.0) - Math.Pow(aceleracionLateralG, 2.0)));

        double aceleracionLongitudinalG = CalcularAceleracionLongitudinalG(acelerador, freno, adherenciaLongitudinalMaximaG);
        _velocidadMetrosPorSegundo += (aceleracionLongitudinalG * ACELERACION_GRAVEDAD_MS2) * PASO_TIEMPO_DELTA;
        _velocidadMetrosPorSegundo = Math.Clamp(_velocidadMetrosPorSegundo, VELOCIDAD_MINIMA_MS, VELOCIDAD_MAXIMA_MS);

        return (aceleracionLateralG, aceleracionLongitudinalG, anguloDireccion);
    }

    private double CalcularAceleracionLongitudinalG(
        double acelerador,
        double freno,
        double adherenciaLongitudinalMaximaG)
    {
        if (freno > 0.05)
        {
            return -Math.Min(DESACELERACION_FRENADO_MAXIMA_G * freno, adherenciaLongitudinalMaximaG);
        }

        if (_cuadrosCorteEncendido > 0)
        {
            _cuadrosCorteEncendido--;
            return DESACELERACION_CORTE_ENCENDIDO_G;
        }

        double factorArrastre = Math.Pow(_velocidadMetrosPorSegundo / VELOCIDAD_MAXIMA_MS, 2.0);
        return Math.Min(ACELERACION_TRACCION_MAXIMA_G * acelerador * (1.0 - factorArrastre), adherenciaLongitudinalMaximaG);
    }

    private float[] ActualizarTermodinamicaNeumaticos(
        double aceleracionLateralG,
        double aceleracionLongitudinalG,
        double freno)
    {
        float[] presionesPsi = new float[4];

        for (int indiceRueda = 0; indiceRueda < 4; indiceRueda++)
        {
            float factorEjeDelantero = indiceRueda < 2 ? 1.15f : 0.90f;
            float aporteCalor = (float)((Math.Abs(aceleracionLateralG) * 1.8 + freno * 3.2) * factorEjeDelantero * PASO_TIEMPO_DELTA);
            float disipacion = (_temperaturasNeumaticosC[indiceRueda] - TEMPERATURA_AMBIENTE_NEUMATICO_C) * 0.08f * (float)PASO_TIEMPO_DELTA;

            _temperaturasNeumaticosC[indiceRueda] = Math.Clamp(
                _temperaturasNeumaticosC[indiceRueda] + aporteCalor - disipacion,
                TEMPERATURA_MINIMA_NEUMATICO_C,
                TEMPERATURA_MAXIMA_NEUMATICO_C);

            presionesPsi[indiceRueda] = (float)Math.Round(
                PRESION_BASE_NEUMATICO_PSI + (_temperaturasNeumaticosC[indiceRueda] - TEMPERATURA_REFERENCIA_PRESION_C) * COEFICIENTE_TERMICO_PRESION_PSI,
                2);

            float tasaDesgaste = (float)((Math.Abs(aceleracionLateralG) + Math.Abs(aceleracionLongitudinalG)) * 0.00025 * factorEjeDelantero * PASO_TIEMPO_DELTA);
            _desgasteNeumaticosPorcentaje[indiceRueda] = Math.Max(0.0f, _desgasteNeumaticosPorcentaje[indiceRueda] - tasaDesgaste);
        }

        return presionesPsi;
    }

    private (int marcha, int revolucionesPorMinuto) ComputarTrenMotriz(double velocidadKmh)
    {
        double rpmEnMarchaActual = velocidadKmh * FACTORES_MARCHA_RPM_POR_KMH[_marchaActual];

        if (rpmEnMarchaActual >= RPM_CAMBIO_ASCENDENTE && _marchaActual < 7)
        {
            _marchaActual++;
            _cuadrosCorteEncendido = CUADROS_CORTE_ENCENDIDO_UPSHIFT;
        }
        else if (rpmEnMarchaActual <= RPM_CAMBIO_DESCENDENTE && _marchaActual > 1)
        {
            _marchaActual--;
            _cuadrosBlipRebaje = CUADROS_BLIP_REBAJE_DOWNSHIFT;
        }

        double rpmBruto = velocidadKmh * FACTORES_MARCHA_RPM_POR_KMH[_marchaActual];
        if (_cuadrosBlipRebaje > 0)
        {
            _cuadrosBlipRebaje--;
            rpmBruto += INCREMENTO_RPM_BLIP_REBAJE;
        }

        int revolucionesPorMinuto = Math.Clamp((int)Math.Round(rpmBruto), RPM_RALENTI, RPM_MAXIMAS);
        return (_marchaActual, revolucionesPorMinuto);
    }

    private (int tiempoVueltaMs, int sectorActual, int ultimoSectorMs, int[] sectoresReportados, int deltaMs) AvanzarProgresionYCronometraje()
    {
        double factorRitmoVuelta = 1.0 + Math.Sin(_numeroVueltaActual * 1.7) * 0.0035;
        double distanciaAvanzada = (_velocidadMetrosPorSegundo * factorRitmoVuelta) * PASO_TIEMPO_DELTA;
        _progresoCircuito += distanciaAvanzada / LONGITUD_MONZA_METROS;

        int tiempoTranscurridoVueltaMs = (int)Math.Round(_cuadroVueltaActual * (1000.0 * PASO_TIEMPO_DELTA));
        int sectorActual = _progresoCircuito < LIMITE_PROGRESO_SECTOR_1
            ? 1
            : (_progresoCircuito < LIMITE_PROGRESO_SECTOR_2 ? 2 : 3);

        if (sectorActual != _ultimoSectorRegistrado && _progresoCircuito < 1.0)
        {
            int indiceSectorCerrado = Math.Clamp(_ultimoSectorRegistrado - 1, 0, 2);
            int duracionSectorMs = Math.Max(1, tiempoTranscurridoVueltaMs - _tiempoInicioSectorMs);
            _tiemposSectoresMs[indiceSectorCerrado] = duracionSectorMs;
            _ultimoTiempoSectorCerradoMs = duracionSectorMs;
            _tiempoInicioSectorMs = tiempoTranscurridoVueltaMs;
            _ultimoSectorRegistrado = sectorActual;
        }

        if (_progresoCircuito >= 1.0)
        {
            return ProcesarCruceLineaMeta(tiempoTranscurridoVueltaMs);
        }

        int[] sectoresEnCurso = (int[])_tiemposSectoresMs.Clone();
        int deltaMs = CalcularDeltaTiempoRealMs(_progresoCircuito);
        return (tiempoTranscurridoVueltaMs, sectorActual, _ultimoTiempoSectorCerradoMs, sectoresEnCurso, deltaMs);
    }

    private (int tiempoVueltaMs, int sectorActual, int ultimoSectorMs, int[] sectoresReportados, int deltaMs) ProcesarCruceLineaMeta(
        int tiempoTranscurridoVueltaMs)
    {
        _progresoCircuito -= 1.0;
        int duracionSectorFinalMs = Math.Max(1, tiempoTranscurridoVueltaMs - _tiempoInicioSectorMs);
        _tiemposSectoresMs[2] = duracionSectorFinalMs;
        _ultimoTiempoSectorCerradoMs = duracionSectorFinalMs;

        int[] sectoresCerrados = (int[])_tiemposSectoresMs.Clone();
        _ultimoTiempoVueltaMs = tiempoTranscurridoVueltaMs;

        if (_ultimoTiempoVueltaMs < _mejorTiempoVueltaMs)
        {
            _mejorTiempoVueltaMs = _ultimoTiempoVueltaMs;
        }

        _cuadroVueltaActual = 0;
        _numeroVueltaActual++;
        _tiempoInicioSectorMs = 0;
        _ultimoSectorRegistrado = 1;
        Array.Clear(_tiemposSectoresMs, 0, _tiemposSectoresMs.Length);

        int deltaMs = CalcularDeltaTiempoRealMs(_progresoCircuito);
        return (0, 1, _ultimoTiempoSectorCerradoMs, sectoresCerrados, deltaMs);
    }

    private int CalcularDeltaTiempoRealMs(double progresoNormalizado)
    {
        double oscilacionArmonica = Math.Sin((progresoNormalizado + _numeroVueltaActual * 0.25) * 2.0 * Math.PI) * 350.0;
        return (int)Math.Round(oscilacionArmonica);
    }

    private static (double acelerador, double freno, double radioCurva) EvaluarPerfilSector(
        double progreso,
        double velocidadActualKmh)
    {
        var (velocidadObjetivoKmh, aceleradorBase, frenoBase, radioCurva) = ObtenerParametrosTramoMonza(progreso);

        if (velocidadActualKmh > velocidadObjetivoKmh + 3.0)
        {
            double excesoVelocidad = Math.Clamp((velocidadActualKmh - velocidadObjetivoKmh) / 40.0, 0.25, 1.0);
            return (0.0, Math.Max(frenoBase, excesoVelocidad), radioCurva);
        }

        if (velocidadActualKmh < velocidadObjetivoKmh - 2.0)
        {
            return (aceleradorBase, 0.0, radioCurva);
        }

        return (0.35, 0.0, radioCurva);
    }

    private static (double velocidadObjetivoKmh, double aceleradorBase, double frenoBase, double radioCurva) ObtenerParametrosTramoMonza(
        double progreso)
    {
        // Tramo 2: Variante del Rettifilo (Chicana 1: derecha -> izquierda, vértice a 75 km/h en 2da)
        if (progreso is >= 0.14 and < 0.16) return (75.0, 0.35, 1.0, 32.0);
        if (progreso is >= 0.16 and < 0.20) return (185.0, 0.85, 0.0, -35.0);

        // Tramo 3: Curva Biassono / Curva Grande (derecha rápida a fondo, hasta 290 km/h en 6ta)
        if (progreso is >= 0.20 and < 0.34) return (292.0, 1.0, 0.0, 340.0);

        // Tramo 4: Variante della Roggia (Chicana 2: izquierda -> derecha, vértice a 95 km/h en 3ra)
        if (progreso is >= 0.34 and < 0.37) return (95.0, 0.40, 0.95, -45.0);
        if (progreso is >= 0.37 and < 0.44) return (220.0, 0.90, 0.0, 48.0);

        // Tramo 5: Curvas di Lesmo 1 (160 km/h, 4ta) y Lesmo 2 (210 km/h, 5ta)
        if (progreso is >= 0.44 and < 0.49) return (160.0, 0.75, 0.75, 105.0);
        if (progreso is >= 0.49 and < 0.58) return (225.0, 0.92, 0.40, 145.0);

        // Tramo 6: Curva del Serraglio y Recta hacia Ascari (295 km/h en 6ta)
        if (progreso is >= 0.58 and < 0.70) return (295.0, 1.0, 0.0, 0.0);

        // Tramo 7: Variante Ascari (izquierda -> derecha -> izquierda, 160 a 245 km/h, 4ta -> 5ta)
        if (progreso is >= 0.70 and < 0.73) return (160.0, 0.65, 0.85, -105.0);
        if (progreso is >= 0.73 and < 0.77) return (195.0, 0.88, 0.0, 115.0);
        if (progreso is >= 0.77 and < 0.82) return (245.0, 0.95, 0.0, -160.0);

        // Tramo 8: Curva Alboreto / Parabólica (entrada a 170 km/h en 4ta y salida a 255 km/h en 6ta)
        if (progreso is >= 0.82 and < 0.85) return (170.0, 0.65, 0.80, 130.0);
        if (progreso is >= 0.85 and < 0.94) return (255.0, 0.95, 0.0, 210.0);

        // Tramos 1 y 9: Recta Principal Rettifilo Tribune y Enlace de Meta (270 a 325 km/h, 6ta -> 7ma)
        return (325.0, 1.0, 0.0, 0.0);
    }

    private static (double coordenadaX, double coordenadaZ) ComputarCoordenadasMonza(double progreso)
    {
        double anguloRadianes = progreso * 2.0 * Math.PI;
        double radioX = 750.0 + 280.0 * Math.Sin(anguloRadianes * 2.0);
        double radioZ = 350.0 + 120.0 * Math.Cos(anguloRadianes * 3.0);

        double coordenadaX = Math.Round(Math.Cos(anguloRadianes) * radioX, 2);
        double coordenadaZ = Math.Round(Math.Sin(anguloRadianes) * radioZ, 2);

        return (coordenadaX, coordenadaZ);
    }

    private CuadroTelemetria ConstruirCuadroDominio(
        double velocidadKmh,
        int marcha,
        int revolucionesPorMinuto,
        double acelerador,
        double freno,
        (double aceleracionLateralG, double aceleracionLongitudinalG, double anguloDireccion) dinamicaChasis,
        float[] presionesPsi,
        (int tiempoVueltaMs, int sectorActual, int ultimoSectorMs, int[] sectoresReportados, int deltaMs) cronometraje,
        (double coordenadaX, double coordenadaZ) coordenadas)
    {
        return new CuadroTelemetria
        {
            MarcaTemporalUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            EstadoSimulador = 2,
            TipoSesion = 0,
            EsSimulacionMock = true,
            EstaEnBoxes = false,
            EsVueltaValida = true,
            VelocidadKmh = (float)Math.Round(velocidadKmh, 1),
            Rpms = revolucionesPorMinuto,
            MaxRpm = _metadatosEstaticos.MaxRpm,
            Marcha = marcha,
            Acelerador = (float)Math.Round(acelerador, 3),
            Freno = (float)Math.Round(freno, 3),
            Embrague = 0.0f,
            ControlTraccion = 2.0f,
            Abs = 3.0f,
            RepartoFrenada = 58.5f,
            LimitadorBoxesActivo = false,
            Direccion = (float)Math.Round(dinamicaChasis.anguloDireccion, 3),
            DrsDisponible = velocidadKmh > UMBRAL_VELOCIDAD_DRS_DISPONIBLE_KMH,
            DrsHabilitado = velocidadKmh > UMBRAL_VELOCIDAD_DRS_HABILITADO_KMH && marcha == 7,
            PresionNeumaticosPsi = presionesPsi,
            TemperaturaNeumaticosC = (float[])_temperaturasNeumaticosC.Clone(),
            DesgasteNeumaticos = (float[])_desgasteNeumaticosPorcentaje.Clone(),
            TemperaturaFrenosC = new float[] { 623.0f, 648.0f, 634.0f, 701.0f },
            CompuestoNeumatico = "SOFT",
            CombustibleLitros = Math.Max(5.0f, 46.2f - (_numeroVueltaActual * 2.45f)),
            CapacidadCombustibleLitros = _metadatosEstaticos.MaxCombustible,
            TiempoVueltaActualMs = cronometraje.tiempoVueltaMs,
            TiempoVueltaActualTexto = FormatearTiempo(cronometraje.tiempoVueltaMs),
            UltimoTiempoVueltaMs = _ultimoTiempoVueltaMs,
            UltimoTiempoVueltaTexto = FormatearTiempo(_ultimoTiempoVueltaMs),
            MejorTiempoMs = _mejorTiempoVueltaMs,
            MejorTiempoTexto = FormatearTiempo(_mejorTiempoVueltaMs),
            DeltaMs = cronometraje.deltaMs,
            VueltasCompletadas = _numeroVueltaActual,
            SectorActual = cronometraje.sectorActual,
            UltimoTiempoSectorMs = cronometraje.ultimoSectorMs,
            TiemposSectoresMs = cronometraje.sectoresReportados,
            ProgresoPistaNormalizado = (float)Math.Round(_progresoCircuito, 4),
            DistanciaMetros = (float)(_progresoCircuito * LONGITUD_MONZA_METROS),
            FuerzaGLat = (float)Math.Round(dinamicaChasis.aceleracionLateralG, 2),
            FuerzaGLon = (float)Math.Round(dinamicaChasis.aceleracionLongitudinalG, 2),
            FuerzaGVert = 0.05f,
            PosicionX = (float)coordenadas.coordenadaX,
            PosicionY = 12.0f,
            PosicionZ = (float)coordenadas.coordenadaZ,
            Bandera = "GREEN",
            PosicionCarrera = 1,
            CantidadAutosActivos = 1,
            Circuito = _metadatosEstaticos.NombrePista,
            ConfiguracionCircuito = _metadatosEstaticos.ConfiguracionPista,
            ModeloAuto = _metadatosEstaticos.NombreAuto
        };
    }

    private CuadroTelemetria CrearCuadroInicialPorDefecto()
    {
        return new CuadroTelemetria
        {
            EstadoSimulador = 2,
            TipoSesion = 0,
            EsSimulacionMock = true,
            Circuito = _metadatosEstaticos.NombrePista,
            ConfiguracionCircuito = _metadatosEstaticos.ConfiguracionPista,
            ModeloAuto = _metadatosEstaticos.NombreAuto,
            MaxRpm = _metadatosEstaticos.MaxRpm,
            CantidadAutosActivos = 1
        };
    }

    private static string FormatearTiempo(long milisegundos)
    {
        var tiempo = TimeSpan.FromMilliseconds(milisegundos);
        return $"{tiempo.Minutes:D2}:{tiempo.Seconds:D2}.{tiempo.Milliseconds:D3}";
    }
}
