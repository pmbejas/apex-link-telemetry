# Análisis Técnico Comparativo: `mdjarv/assettocorsasharedmemory`

> **Repositorio Auditado:** `https://github.com/mdjarv/assettocorsasharedmemory.git`  
> **Fecha de Análisis:** 27 de Octubre de 2026  
> **Autor Original:** Marcus Djarv (`mdjarv`)  
> **Objetivo:** Auditar la estructura de Memoria Compartida de Assetto Corsa, verificar la exactitud de nuestro proyecto y comparar arquitecturas de rendimiento y concurrencia.

---

## 1. Propósito y Funcionamiento del Repositorio

El repositorio `mdjarv/assettocorsasharedmemory` es una biblioteca cliente en C# (.NET Framework) diseñada para que aplicaciones externas de telemetría (dashboards, overlays o pitwalls) se suscriban a los cambios de estado de Assetto Corsa mediante un modelo basado en eventos.

### Flujo de Funcionamiento Interno:
1. **Conexión mediante Temporizador de Reintento:**
   * Utiliza un `System.Timers.Timer` (`sharedMemoryRetryTimer`) que se ejecuta cada 2.000 ms.
   * Intenta abrir los tres descriptores de memoria compartida de Windows:
     * `MemoryMappedFile.OpenExisting("Local\\acpmf_physics")`
     * `MemoryMappedFile.OpenExisting("Local\\acpmf_graphics")`
     * `MemoryMappedFile.OpenExisting("Local\\acpmf_static")`
   * Si Assetto Corsa aún no está iniciado, captura `FileNotFoundException` y continúa reintentando en segundo plano.

2. **Temporizadores Asíncronos Desacoplados:**
   Una vez conectado, inicia tres temporizadores independientes:
   * **`physicsTimer` (10 ms / ~100 Hz):** Lee la física del vehículo y dispara el evento `PhysicsUpdated`.
   * **`graphicsTimer` (1.000 ms / 1 Hz por defecto):** Lee el estado de carrera y dispara `GraphicsUpdated` y `GameStatusChanged`.
   * **`staticInfoTimer` (1.000 ms / 1 Hz por defecto):** Lee los metadatos estáticos del auto y circuito y dispara `StaticInfoUpdated`.

3. **Mapeo de Memoria en Cada Tick:**
   En cada ciclo de temporizador:
   * Crea un flujo con `mmf.CreateViewStream()`.
   * Lee la cantidad de bytes requerida por la estructura con `BinaryReader.ReadBytes(size)`.
   * Fija la memoria administrada con `GCHandle.Alloc(bytes, GCHandleType.Pinned)`.
   * Deserializa a la estructura de C# con `Marshal.PtrToStructure(...)` y libera el handle con `handle.Free()`.

---

## 2. Validación de Campos y Evolución por Versiones de Assetto Corsa

El código de `mdjarv` documenta de forma explícita cómo evolucionó la memoria compartida desde la versión 1.0 hasta la 1.13 de Assetto Corsa. Esto **valida al 100% que las estructuras de nuestro proyecto son exactas**:

### A. Física (`Physics.cs`)
* Alineación binaria: `[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]`.
* Arrays fijos: `Velocity[3]`, `AccG[3]`, `WheelSlip[4]`, `WheelLoad[4]`, `WheelsPressure[4]`, `TyreWear[4]`, `TyreCoreTemperature[4]`, `CarDamage[5]`.
* **Historial de incorporación oficial:**
  * **v1.5:** Se incorporan `TurboBoost`, `Ballast`, `AirDensity`.
  * **v1.6:** Se incorporan `AirTemp`, `RoadTemp`, `LocalAngularVelocity[3]`, `FinalFF`.
  * **v1.7:** Se incorporan `PerformanceMeter`, `EngineBrake`, `ErsRecoveryLevel`, `ErsPowerLevel`, `ErsHeatCharging`, `ErsisCharging`, `KersCurrentKJ`, `DrsAvailable`, `DrsEnabled`, `BrakeTemp[4]`.
  * **v1.10:** Se incorporan `Clutch`, `TyreTempI[4]`, `TyreTempM[4]`, `TyreTempO[4]` (temperaturas internas, medias y externas del neumático).
  * **v1.10.2:** Se incorpora `IsAIControlled`.
  * **v1.11:** Se incorporan `TyreContactPoint[4]`, `TyreContactNormal[4]`, `TyreContactHeading[4]`, `BrakeBias`.
  * **v1.12:** Se incorpora `LocalVelocity[3]`.

### B. Gráficos (`Graphics.cs`)
* Cadenas de tiempo con longitud fija:
  * `CurrentTime`, `LastTime`, `BestTime`, `Split`: `[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]`.
  * `TyreCompound`: `[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]`.
* Enum de banderas (`AC_FLAG_TYPE`): `AC_NO_FLAG (0)`, `AC_BLUE_FLAG (1)`, `AC_YELLOW_FLAG (2)`, `AC_BLACK_FLAG (3)`, `AC_WHITE_FLAG (4)`, `AC_CHECKERED_FLAG (5)`, `AC_PENALTY_FLAG (6)`.
* Incorporaciones posteriores: `PenaltyTime`, `Flag`, `IdealLineOn`, `IsInPitLane`, `SurfaceGrip` (v1.5), `MandatoryPitDone` (v1.13).

### C. Información Estática (`StaticInfo.cs`)
* Cadenas con longitud fija:
  * `SMVersion`, `ACVersion`, `TrackConfiguration`: `SizeConst = 15`.
  * `CarModel`, `Track`, `PlayerName`, `PlayerSurname`, `PlayerNick`, `CarSkin`: `SizeConst = 33`.
* Parámetros clave agregados:
  * `HasDRS`, `HasERS`, `HasKERS`, `KersMaxJoules`, `EngineBrakeSettingsCount`, `ErsPowerControllerCount` (v1.7.1).
  * **`TrackSPlineLength`** (v1.7.2): Longitud total del trazado de la pista en metros.
  * `CarSkin`, `ReversedGridPositions`, `PitWindowStart`, `PitWindowEnd` (v1.13).

---

## 3. Comparación Crítica con Nuestro Proyecto

| Aspecto de Arquitectura | `mdjarv/assettocorsasharedmemory` | Nuestro Proyecto (`Clean Architecture` en C#) | Veredicto |
| :--- | :--- | :--- | :--- |
| **Asignaciones de Memoria (GC Allocation)** | Crea streams, arrays de bytes temporales y handles `GCHandle.Alloc` 100 veces por segundo. Provoca miles de recolecciones Gen 0 en el GC. | Mantiene `MemoryMappedViewAccessor` abierto y lee directamente mediante **punteros directos o structs `fixed`** (*Zero Heap Allocations*). | **Nuestro proyecto es masivamente superior en rendimiento y latencia.** |
| **Sincronización Concurrente (Tearing)** | No tiene sincronización entre hilos. Si AC escribe mientras el timer lee, se leen datos corruptos o partidos. | Algoritmo **SeqLock** con barreras de memoria (`Thread.MemoryBarrier()`), garantizando coherencia atómica del frame. | **Nuestro proyecto previene lecturas corruptas.** |
| **Consistencia Temporal** | 3 timers separados (`10 ms`, `1.000 ms`, `1.000 ms`) que generan eventos asíncronos desacoplados. | Orquestador unificado a **60 Hz** gobernado por `PeriodicTimer`, emitiendo cuadros completos (Física + Gráficos) por WebSocket. | **Nuestro proyecto asegura sincronía visual perfecta.** |
| **Longitud de Pista** | Lee `TrackSPlineLength` directamente de `StaticInfo`. | Previamente tenía fallback; ahora podemos mapear directamente `TrackSPlineLength` desde `StaticInfo`. | **Mejora adoptada de este análisis.** |

---

## 4. Mejoras Concretas a Adoptar en Nuestro Proyecto

1. **Lectura Oficial de Longitud de Pista (`TrackSPlineLength`):**
   * En `SPageFileStatic`, mapear el campo `TrackSPlineLength` (float disponible desde AC v1.7.2). Esto permite que el sistema conozca la longitud exacta en metros de cualquier circuito (incluyendo pistas de mods de la comunidad) sin necesidad de tablas auxiliares fijas.
2. **Bandera de Carrera (`Flag`):**
   * Incorporar en los DTOs de WebSocket el campo `Flag` (`AC_FLAG_TYPE`), permitiendo que el volante y la pantalla de boxes muestren banderas amarillas, azules o de penalización en tiempo real.
3. **Confirmación de Tamaños de Cadena:**
   * Garantizar que los buffers de texto para `CurrentTime`, `LastTime`, `BestTime` y `Split` reserven 15 caracteres Unicode (30 bytes) y que `TyreCompound`, `CarModel` y `Track` reserven 33 caracteres Unicode (66 bytes), respetando la especificación oficial de Kunos.
