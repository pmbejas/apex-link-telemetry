namespace Telemetria.Infraestructura.MemoriaCompartida;

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Microsoft.Extensions.Logging;
using Telemetria.Dominio.Contratos.Hardware;
using Telemetria.Dominio.Modelos;
using Telemetria.Infraestructura.MemoriaCompartida.Estructuras;

public sealed class LectorAssettoCorsaWin32 : ILectorTelemetria
{
    private const int MAXIMO_REINTENTOS_LECTURA_ATOMICA = 3;
    private const long TIEMPO_MAXIMO_PACKET_ID_CONGELADO_MS = 3000;

    private readonly object _cerrojoConexion = new();
    private readonly ILogger<LectorAssettoCorsaWin32> _logger;

    private MemoryMappedFile? _archivoFisica;
    private MemoryMappedViewAccessor? _lectorFisica;
    private MemoryMappedFile? _archivoGrafica;
    private MemoryMappedViewAccessor? _lectorGrafica;
    private MemoryMappedFile? _archivoEstatica;
    private MemoryMappedViewAccessor? _lectorEstatica;

    private MetadatosEstaticos? _metadatosEstaticosCache;
    private CuadroTelemetria? _ultimoCuadroLeido;

    private int _ultimoPacketIdFisica = -1;
    private int _ultimoPacketIdGrafica = -1;
    private long _marcaUltimoCambioPacketIdMs = 0;
    private int _ultimoEstadoSimulador = -1;
    private int _ultimaSesionActiva = -1;
    private int _ultimaVueltaConocida = -1;
    private int _ultimoSectorConocido = 1;
    private bool _vueltaActualEsValida = true;
    private readonly int[] _tiemposSectoresVueltaMs = new int[3];

    public LectorAssettoCorsaWin32(ILogger<LectorAssettoCorsaWin32> logger)
    {
        _logger = logger;
    }

    public bool EstaConectado()
    {
        lock (_cerrojoConexion)
        {
            return TieneDescriptoresAbiertos();
        }
    }

    public bool IntentarConectar()
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("La lectura de memoria compartida Win32 ('Local\\acpmf_...') requiere Windows.");
            return false;
        }

        lock (_cerrojoConexion)
        {
            if (TieneDescriptoresAbiertos()) return true;

            try
            {
                _archivoFisica = MemoryMappedFile.OpenExisting("Local\\acpmf_physics", MemoryMappedFileRights.Read);
                _lectorFisica = _archivoFisica.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

                _archivoGrafica = AbrirArchivoGrafica();
                _lectorGrafica = _archivoGrafica.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

                _archivoEstatica = MemoryMappedFile.OpenExisting("Local\\acpmf_static", MemoryMappedFileRights.Read);
                _lectorEstatica = _archivoEstatica.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

                // Si Assetto Corsa está en el launcher (Status == 0 en offset 4), liberar descriptores sin iniciar sesión activa
                if (_lectorGrafica.ReadInt32(4) == 0)
                {
                    LiberarDescriptoresInterno();
                    return false;
                }

                _metadatosEstaticosCache = LeerMetadatosEstaticosInterno();
                _marcaUltimoCambioPacketIdMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _logger.LogInformation("Enlace exitoso con Assetto Corsa. Pista: {Pista}, Auto: {Auto}",
                    _metadatosEstaticosCache.NombrePista, _metadatosEstaticosCache.NombreAuto);

                return true;
            }
            catch (FileNotFoundException)
            {
                LiberarDescriptoresInterno();
                return false;
            }
            catch (Exception excepcion)
            {
                _logger.LogWarning(excepcion, "Excepción al intentar abrir memoria compartida de Assetto Corsa");
                LiberarDescriptoresInterno();
                return false;
            }
        }
    }

    public CuadroTelemetria ObtenerCuadroActual()
    {
        lock (_cerrojoConexion)
        {
            if (!TieneDescriptoresAbiertos())
            {
                throw new InvalidOperationException("No hay conexión activa con la memoria compartida de Assetto Corsa");
            }

            long ahoraUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!IntentarLeerEstructurasConsistentes(out var fisica, out var grafica))
            {
                _logger.LogWarning("Se descartó un cuadro por lectura desgarrada tras {Intentos} intentos.", MAXIMO_REINTENTOS_LECTURA_ATOMICA);
                return _ultimoCuadroLeido ?? new CuadroTelemetria { MarcaTemporalUnixMs = ahoraUnixMs, EstadoSimulador = 0 };
            }

            if (!SimuladorSigueActivo(in fisica, in grafica, ahoraUnixMs))
            {
                LiberarDescriptoresInterno();
                return new CuadroTelemetria { MarcaTemporalUnixMs = ahoraUnixMs, EstadoSimulador = 0 };
            }

            ActualizarMetadatosSiCorresponde(in grafica);
            var (sectorActualBase1, esVueltaValida, sectoresReportados) = ProcesarSectoresYValidezVuelta(in fisica, in grafica);

            CuadroTelemetria cuadro = ConstruirCuadroTelemetria(
                in fisica, in grafica, ahoraUnixMs, sectorActualBase1, esVueltaValida, sectoresReportados);

            _ultimoCuadroLeido = cuadro;
            return cuadro;
        }
    }

    public CuadroTelemetria ObtenerUltimoCuadroConocido()
    {
        lock (_cerrojoConexion)
        {
            if (_ultimoCuadroLeido != null) return _ultimoCuadroLeido;
            return TieneDescriptoresAbiertos() ? ObtenerCuadroActual() : new CuadroTelemetria { EstadoSimulador = 0 };
        }
    }

    public MetadatosEstaticos ObtenerMetadatosEstaticos()
    {
        lock (_cerrojoConexion)
        {
            if (!TieneDescriptoresAbiertos()) return new MetadatosEstaticos();
            return _metadatosEstaticosCache ??= LeerMetadatosEstaticosInterno();
        }
    }

    public void Desconectar()
    {
        lock (_cerrojoConexion)
        {
            LiberarDescriptoresInterno();
        }
    }

    public void Dispose() => Desconectar();

    private bool TieneDescriptoresAbiertos() =>
        _lectorFisica != null && _lectorGrafica != null && _lectorEstatica != null;

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static MemoryMappedFile AbrirArchivoGrafica()
    {
        try
        {
            return MemoryMappedFile.OpenExisting("Local\\acpmf_graphics", MemoryMappedFileRights.Read);
        }
        catch (FileNotFoundException)
        {
            return MemoryMappedFile.OpenExisting("Local\\acpmf_graphic", MemoryMappedFileRights.Read);
        }
    }

    private bool IntentarLeerEstructurasConsistentes(
        out EstructuraFisicaAC fisica,
        out EstructuraGraficaAC grafica)
    {
        fisica = default;
        grafica = default;

        for (int intento = 0; intento < MAXIMO_REINTENTOS_LECTURA_ATOMICA; intento++)
        {
            int packetIdAntesFisica = _lectorFisica!.ReadInt32(0);
            Thread.MemoryBarrier();
            _lectorFisica.Read(0, out fisica);
            Thread.MemoryBarrier();
            int packetIdDespuesFisica = _lectorFisica.ReadInt32(0);

            int packetIdAntesGrafica = _lectorGrafica!.ReadInt32(0);
            Thread.MemoryBarrier();
            _lectorGrafica.Read(0, out grafica);
            Thread.MemoryBarrier();
            int packetIdDespuesGrafica = _lectorGrafica.ReadInt32(0);

            bool fisicaIntegra = (fisica.PacketId == packetIdAntesFisica) && (packetIdAntesFisica == packetIdDespuesFisica);
            bool graficaIntegra = (grafica.PacketId == packetIdAntesGrafica) && (packetIdAntesGrafica == packetIdDespuesGrafica);

            if (fisicaIntegra && graficaIntegra) return true;
        }

        return false;
    }

    private bool SimuladorSigueActivo(
        in EstructuraFisicaAC fisica,
        in EstructuraGraficaAC grafica,
        long ahoraUnixMs)
    {
        // Si Assetto Corsa pasó a Status == 0 (AC_OFF), el simulador cerró la sesión hacia el launcher
        if (grafica.Status == 0)
        {
            _logger.LogInformation("Assetto Corsa reportó Status=0 (AC_OFF). Liberando descriptores MMF.");
            return false;
        }

        if (fisica.PacketId != _ultimoPacketIdFisica || grafica.PacketId != _ultimoPacketIdGrafica)
        {
            _ultimoPacketIdFisica = fisica.PacketId;
            _ultimoPacketIdGrafica = grafica.PacketId;
            _marcaUltimoCambioPacketIdMs = ahoraUnixMs;
            return true;
        }

        if ((ahoraUnixMs - _marcaUltimoCambioPacketIdMs) > TIEMPO_MAXIMO_PACKET_ID_CONGELADO_MS)
        {
            _logger.LogInformation("PacketId sin cambios por más de {TiempoMs} ms. Liberando descriptores MMF.", TIEMPO_MAXIMO_PACKET_ID_CONGELADO_MS);
            return false;
        }

        return true;
    }

    private void ActualizarMetadatosSiCorresponde(in EstructuraGraficaAC grafica)
    {
        bool cambioSesionOEstado = (grafica.Status == 2 && _ultimoEstadoSimulador != 2) ||
                                   (grafica.Session != _ultimaSesionActiva);
        _ultimoEstadoSimulador = grafica.Status;
        _ultimaSesionActiva = grafica.Session;

        if (_metadatosEstaticosCache == null || _metadatosEstaticosCache.LongitudPistaMetros <= 0.0f || cambioSesionOEstado)
        {
            _metadatosEstaticosCache = LeerMetadatosEstaticosInterno();
        }
    }

    private (int SectorActualBase1, bool EsVueltaValida, int[] SectoresMs) ProcesarSectoresYValidezVuelta(
        in EstructuraFisicaAC fisica,
        in EstructuraGraficaAC grafica)
    {
        int sectorActualBase1 = Math.Clamp(grafica.CurrentSectorIndex + 1, 1, 3);
        bool fueraDePistaEnCuadro = fisica.NumberOfTyresOut >= 4;

        // 1. Detectar reinicio de sesión (el contador de vueltas retrocede)
        if (_ultimaVueltaConocida != -1 && grafica.CompletedLaps < _ultimaVueltaConocida)
        {
            Array.Clear(_tiemposSectoresVueltaMs, 0, _tiemposSectoresVueltaMs.Length);
            _ultimoSectorConocido = sectorActualBase1;
            _vueltaActualEsValida = !fueraDePistaEnCuadro;
            _ultimaVueltaConocida = grafica.CompletedLaps;
            return (sectorActualBase1, _vueltaActualEsValida, (int[])_tiemposSectoresVueltaMs.Clone());
        }

        // 2. Detectar cruce de línea de meta (avance de vuelta completada)
        if (_ultimaVueltaConocida != -1 && grafica.CompletedLaps > _ultimaVueltaConocida)
        {
            int sumaSectoresPrevios = _tiemposSectoresVueltaMs[0] + _tiemposSectoresVueltaMs[1];
            _tiemposSectoresVueltaMs[2] = (_tiemposSectoresVueltaMs[0] > 0 && _tiemposSectoresVueltaMs[1] > 0 && grafica.ILastTime > sumaSectoresPrevios)
                ? (grafica.ILastTime - sumaSectoresPrevios)
                : Math.Max(0, grafica.LastSectorTime);

            bool validezVueltaCerrada = _vueltaActualEsValida;
            int[] sectoresCerrados = (int[])_tiemposSectoresVueltaMs.Clone();

            Array.Clear(_tiemposSectoresVueltaMs, 0, _tiemposSectoresVueltaMs.Length);
            _vueltaActualEsValida = !fueraDePistaEnCuadro;
            _ultimaVueltaConocida = grafica.CompletedLaps;
            _ultimoSectorConocido = sectorActualBase1;
            return (sectorActualBase1, validezVueltaCerrada, sectoresCerrados);
        }

        // 3. Progresión normal dentro de la misma vuelta
        if (_ultimaVueltaConocida == -1) _ultimaVueltaConocida = grafica.CompletedLaps;
        if (sectorActualBase1 != _ultimoSectorConocido && grafica.LastSectorTime > 0)
        {
            int indiceSectorCerrado = Math.Clamp(_ultimoSectorConocido - 1, 0, 2);
            _tiemposSectoresVueltaMs[indiceSectorCerrado] = grafica.LastSectorTime;
            _ultimoSectorConocido = sectorActualBase1;
        }

        if (fueraDePistaEnCuadro) _vueltaActualEsValida = false;
        return (sectorActualBase1, _vueltaActualEsValida, (int[])_tiemposSectoresVueltaMs.Clone());
    }

    private static unsafe (float X, float Y, float Z) ObtenerCoordenadasAutoJugador(
        in EstructuraGraficaAC grafica,
        int cantidadAutos)
    {
        int indiceAutoJugador = 0;
        for (int indice = 0; indice < cantidadAutos; indice++)
        {
            if (grafica.CarID[indice] == grafica.PlayerCarID)
            {
                indiceAutoJugador = indice;
                break;
            }
        }

        int baseOffset = indiceAutoJugador * 3;
        return (
            grafica.CarCoordinates[baseOffset + 0],
            grafica.CarCoordinates[baseOffset + 1],
            grafica.CarCoordinates[baseOffset + 2]
        );
    }

    private unsafe CuadroTelemetria ConstruirCuadroTelemetria(
        in EstructuraFisicaAC fisica,
        in EstructuraGraficaAC grafica,
        long ahoraUnixMs,
        int sectorActualBase1,
        bool esVueltaValida,
        int[] sectoresReportados)
    {
        int cantidadAutos = Math.Clamp(grafica.ActiveCars, 1, 60);
        var (posicionX, posicionY, posicionZ) = ObtenerCoordenadasAutoJugador(in grafica, cantidadAutos);

        float longitudCircuito = (_metadatosEstaticosCache is { LongitudPistaMetros: > 0.0f })
            ? _metadatosEstaticosCache.LongitudPistaMetros
            : 5793.0f;

        int tiempoEsperadoPosicionMs = (int)Math.Round(grafica.NormalizedCarPosition * grafica.IBestTime);
        int deltaInstantaneoMs = (grafica.IBestTime > 0 && grafica.ICurrentTime > 0)
            ? (grafica.ICurrentTime - tiempoEsperadoPosicionMs)
            : 0;

        fixed (char* punteroActual = grafica.CurrentTime, punteroUltimo = grafica.LastTime, punteroMejor = grafica.BestTime)
        {
            return new CuadroTelemetria
            {
                MarcaTemporalUnixMs = ahoraUnixMs,
                EstadoSimulador = grafica.Status,
                TipoSesion = grafica.Session,
                EsSimulacionMock = false,
                EstaEnBoxes = grafica.IsInPit == 1 || grafica.IsInPitLane == 1,
                EsVueltaValida = esVueltaValida,
                VelocidadKmh = fisica.SpeedKmh,
                Rpms = fisica.Rpms,
                MaxRpm = _metadatosEstaticosCache?.MaxRpm > 0 ? _metadatosEstaticosCache.MaxRpm : 13000,
                Marcha = UtilitariosInteroperabilidadAC.NormalizarMarcha(fisica.Gear),
                Acelerador = fisica.Gas,
                Freno = fisica.Brake,
                Direccion = Math.Clamp(fisica.SteerAngle, -1.0f, 1.0f),
                DrsDisponible = fisica.DrsAvailable == 1,
                DrsHabilitado = fisica.DrsEnabled == 1,
                PresionNeumaticosPsi = new[] { fisica.WheelsPressure[0], fisica.WheelsPressure[1], fisica.WheelsPressure[2], fisica.WheelsPressure[3] },
                TemperaturaNeumaticosC = new[] { fisica.TyreCoreTemperature[0], fisica.TyreCoreTemperature[1], fisica.TyreCoreTemperature[2], fisica.TyreCoreTemperature[3] },
                DesgasteNeumaticos = new[] { fisica.TyreWear[0], fisica.TyreWear[1], fisica.TyreWear[2], fisica.TyreWear[3] },
                TiempoVueltaActualMs = grafica.ICurrentTime,
                TiempoVueltaActualTexto = UtilitariosInteroperabilidadAC.LeerTiempoTextoNormalizado(punteroActual, 15, "00:00.000"),
                UltimoTiempoVueltaMs = grafica.ILastTime,
                UltimoTiempoVueltaTexto = UtilitariosInteroperabilidadAC.LeerTiempoTextoNormalizado(punteroUltimo, 15, "00:00.000"),
                MejorTiempoMs = grafica.IBestTime,
                MejorTiempoTexto = UtilitariosInteroperabilidadAC.LeerTiempoTextoNormalizado(punteroMejor, 15, "--:--.---"),
                DeltaMs = deltaInstantaneoMs,
                VueltasCompletadas = grafica.CompletedLaps,
                SectorActual = sectorActualBase1,
                UltimoTiempoSectorMs = grafica.LastSectorTime,
                TiemposSectoresMs = sectoresReportados,
                ProgresoPistaNormalizado = grafica.NormalizedCarPosition,
                DistanciaMetros = grafica.NormalizedCarPosition * longitudCircuito,
                FuerzaGLat = fisica.AccG[0],
                FuerzaGLon = fisica.AccG[2],
                FuerzaGVert = fisica.AccG[1],
                PosicionX = posicionX,
                PosicionY = posicionY,
                PosicionZ = posicionZ,
                Bandera = UtilitariosInteroperabilidadAC.MapearBanderaTexto(grafica.Flag),
                PosicionCarrera = grafica.Position,
                CantidadAutosActivos = cantidadAutos,
                Circuito = _metadatosEstaticosCache?.NombrePista ?? string.Empty,
                ConfiguracionCircuito = _metadatosEstaticosCache?.ConfiguracionPista ?? string.Empty,
                ModeloAuto = _metadatosEstaticosCache?.NombreAuto ?? string.Empty
            };
        }
    }

    private unsafe MetadatosEstaticos LeerMetadatosEstaticosInterno()
    {
        if (!TieneDescriptoresAbiertos()) return new MetadatosEstaticos();

        _lectorEstatica!.Read(0, out EstructuraEstaticaAC estatica);

        return new MetadatosEstaticos
        {
            VersionSm = UtilitariosInteroperabilidadAC.LeerCadenaFija(estatica.SmVersion, 15),
            VersionAc = UtilitariosInteroperabilidadAC.LeerCadenaFija(estatica.AcVersion, 15),
            NombreAuto = UtilitariosInteroperabilidadAC.LeerCadenaFija(estatica.CarModel, 33),
            NombrePista = UtilitariosInteroperabilidadAC.LeerCadenaFija(estatica.Track, 33),
            ConfiguracionPista = UtilitariosInteroperabilidadAC.LeerCadenaFija(estatica.TrackConfiguration, 33),
            MaxRpm = estatica.MaxRpm,
            MaxCombustible = estatica.MaxFuel,
            CantidadSectores = estatica.SectorCount,
            LongitudPistaMetros = estatica.TrackSPlineLength
        };
    }

    private void LiberarDescriptoresInterno()
    {
        _lectorFisica?.Dispose();
        _archivoFisica?.Dispose();
        _lectorGrafica?.Dispose();
        _archivoGrafica?.Dispose();
        _lectorEstatica?.Dispose();
        _archivoEstatica?.Dispose();

        _lectorFisica = null;
        _archivoFisica = null;
        _lectorGrafica = null;
        _archivoGrafica = null;
        _lectorEstatica = null;
        _archivoEstatica = null;
        _metadatosEstaticosCache = null;
        _ultimoCuadroLeido = null;
        _ultimoPacketIdFisica = -1;
        _ultimoPacketIdGrafica = -1;
        _ultimoEstadoSimulador = -1;
        _ultimaSesionActiva = -1;
        _ultimaVueltaConocida = -1;
    }
}
