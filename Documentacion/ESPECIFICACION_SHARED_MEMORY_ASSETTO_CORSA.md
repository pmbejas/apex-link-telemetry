# Especificación Técnica de Memoria Compartida (Shared Memory) - Assetto Corsa

> **Estado:** Verificado y Validado  
> **Fecha de Análisis:** 27 de Octubre de 2026  
> **Fuentes Validadas:** Repositorio Oficial/Comunitario C# (`davidhauck/AssettoCorsaTelemetry`), Ensamblado .NET (`AcSdk.dll`) y Telemetría Real en Pista (`01-05-2014_20-18.csv`).

---

## 1. Introducción y Arquitectura de Acceso

Assetto Corsa 1 (AC1) expone su telemetría de física profunda, gráficos de carrera y metadatos estáticos a través del mecanismo nativo de **Memoria Compartida de Windows** (*Memory Mapped Files* - MMF) a nivel de sesión de usuario (`Local\`).

### Nombres Canónicos de los Archivos Mapeados (MMF)
```csharp
Local\acpmf_physics   // Física de alta frecuencia (suspensión, neumáticos, aceleración G, motor)
Local\acpmf_graphics  // Estado de sesión en pista, tiempos de vuelta, posiciones y sectores
Local\acpmf_static    // Metadatos estáticos del vehículo, circuito, piloto y límites del coche
```

---

## 2. Enumeraciones Oficiales del Simulador

### 2.1 Estado del Simulador (`AcStatus`)
Indica la fase del ciclo de vida en la que se encuentra el simulador:
```csharp
public enum AcStatus
{
    Off = 0,      // Simulador cerrado o fuera de pista
    Replay = 1,   // En modo repetición (Replay)
    Live = 2,     // En pista en tiempo real (activo)
    Pause = 3     // En menú de pausa o boxes esperando
}
```

### 2.2 Tipo de Sesión (`AcSessionType`)
```csharp
public enum AcSessionType
{
    Practice = 0,    // Práctica libre
    Qualify = 1,     // Clasificación
    Race = 2,        // Carrera
    Hotlap = 3,      // Vuelta rápida en solitario
    TimeAttack = 4,  // Contrarreloj
    Drift = 5,       // Drift
    Drag = 6         // Picadas / Drag race
}
```

---

## 3. Estructuras de Memoria y Disposición Binaria

Todas las estructuras de Assetto Corsa se alinean secuencialmente (`LayoutKind.Sequential`). Los arreglos y cadenas de texto embebidos son de longitud fija (`ByValArray` y `ByValTStr` con codificación UTF-16).

### 3.1 Estructura Física (`SPageFilePhysics` / `AcSdk.Data.Physics`)
*Frecuencia nominal de actualización: 60 Hz a 333 Hz.*

| Campo | Tipo | Tamaño / SizeConst | Offset Binario | Unidades / Descripción |
| :--- | :--- | :--- | :--- | :--- |
| `PacketId` | `int` | 4 bytes | 0 | Secuencia de cuadro incremental |
| `Gas` | `float` | 4 bytes | 4 | 0.0 a 1.0 (recorrido acelerador) |
| `Brake` | `float` | 4 bytes | 8 | 0.0 a 1.0 (recorrido freno) |
| `Fuel` | `float` | 4 bytes | 12 | Litros de combustible restantes |
| `Gear` | `int` | 4 bytes | 16 | 0=R, 1=N, 2=1ra, 3=2da... |
| `Rpms` | `int` | 4 bytes | 20 | Revoluciones por minuto del motor |
| `SteerAngle` | `float` | 4 bytes | 24 | Ángulo de giro en radianes |
| `SpeedKmh` | `float` | 4 bytes | 28 | Velocidad lineal en km/h |
| `Velocity` | `float[3]` | 12 bytes | 32 | Velocidad vectorial (m/s) en ejes X, Y, Z |
| `AccG` | `float[3]` | 12 bytes | 44 | Aceleraciones G: X=lateral, Y=vertical, Z=longitudinal |
| `WheelSlip` | `float[4]` | 16 bytes | 56 | Deslizamiento de cada neumático |
| `WheelLoad` | `float[4]` | 16 bytes | 72 | Carga vertical por rueda en Newtons (N) |
| `WheelsPressure` | `float[4]` | 16 bytes | 88 | Presión de inflado en **PSI** |
| `WheelAngularSpeed` | `float[4]` | 16 bytes | 104 | Velocidad angular de ruedas en rad/s |
| `TyreWear` | `float[4]` | 16 bytes | 120 | **Vida útil restante** (100.0% nuevo a 0.0%) |
| `TyreDirtyLevel` | `float[4]` | 16 bytes | 136 | Nivel de suciedad/goma acumulada (0.0 a 1.0) |
| `TyreCoreTemperature`| `float[4]` | 16 bytes | 152 | Temperatura central del neumático en **°C** |
| `CamberRad` | `float[4]` | 16 bytes | 168 | Ángulo de caída en radianes |
| `SuspensionTravel` | `float[4]` | 16 bytes | 184 | Recorrido de suspensión en metros (m) |
| `DRS` | `float` | 4 bytes | 200 | Estado del DRS (0.0 = cerrado, 1.0 = abierto) |
| `TC` | `float` | 4 bytes | 204 | Intervención del control de tracción (0.0 a 1.0) |
| `Heading` | `float` | 4 bytes | 208 | Orientación / guiñada (Yaw) en radianes |
| `Pitch` | `float` | 4 bytes | 212 | Inclinación / cabeceo (Pitch) en radianes |
| `Roll` | `float` | 4 bytes | 216 | Balanceo (Roll) en radianes |
| `CgHeight` | `float` | 4 bytes | 220 | Altura del centro de gravedad en metros |
| `CarDamage` | `float[5]` | 20 bytes | 224 | Daño acumulado en chasis y carrocería |
| `NumberOfTyresOut` | `int` | 4 bytes | 244 | Cantidad de ruedas fuera de pista (0 a 4) |
| `PitLimiterOn` | `int` | 4 bytes | 248 | 1 = Limitador de boxes encendido, 0 = apagado |
| `Abs` | `float` | 4 bytes | 252 | Intervención del sistema ABS (0.0 a 1.0) |

> **Nota:** En versiones modernas de AC (v1.5 a v1.16+), la estructura de física incluye además temperaturas de neumáticos por zona (`TyreTempI`, `TyreTempM`, `TyreTempO`), temperatura de frenos (`BrakeTemp`), embrague (`Clutch`) y variables del sistema híbrido ERS/KERS.

---

### 3.2 Estructura Gráfica (`SPageFileGraphic` / `AcSdk.Data.Graphics`)
*Frecuencia nominal de actualización: 60 Hz.*

| Campo | Tipo | Tamaño / SizeConst | Offset Binario | Unidades / Descripción |
| :--- | :--- | :--- | :--- | :--- |
| `PacketId` | `int` | 4 bytes | 0 | Secuencia de cuadro incremental |
| `Status` | `AcStatus` | 4 bytes | 4 | Estado general del simulador (Off, Replay, Live, Pause) |
| `Session` | `AcSessionType` | 4 bytes | 8 | Tipo de sesión activa |
| `CurrentTime` | `string` | 10 chars (UTF-16) | 12 | Tiempo en vuelta actual formateado (`"m:ss:fff"`) |
| `LastTime` | `string` | 10 chars (UTF-16) | 22 | Tiempo de última vuelta (`"m:ss:fff"`) |
| `BestTime` | `string` | 10 chars (UTF-16) | 32 | Mejor tiempo personal/sesión (`"m:ss:fff"`) |
| `Split` | `string` | 10 chars (UTF-16) | 42 | Tiempo de sector / split formateado |
| `CompletedLaps` | `int` | 4 bytes | 52 | Vueltas finalizadas válidas |
| `Position` | `int` | 4 bytes | 56 | Posición actual en la sesión (1, 2, 3...) |
| `CurrentTimeValue` | `int` | 4 bytes | 60 | Tiempo transcurrido en la vuelta en **milisegundos** |
| `LastTimeValue` | `int` | 4 bytes | 64 | Tiempo de última vuelta cerrada en **milisegundos** |
| `BestTimeValue` | `int` | 4 bytes | 68 | Mejor tiempo de vuelta en **milisegundos** |
| `SessionTimeLeft` | `float` | 4 bytes | 72 | Segundos restantes de la sesión |
| `DistanceTraveled` | `float` | 4 bytes | 76 | Metros totales recorridos en la vuelta |
| `IsInPit` | `int` | 4 bytes | 80 | 1 = Dentro del pit lane, 0 = en pista |
| `CurrentSectorIndex`| `int` | 4 bytes | 84 | Sector en curso (0 = S1, 1 = S2, 2 = S3) |
| `LastSectorTime` | `int` | 4 bytes | 88 | Tiempo del último sector completado en **ms** |
| `NumberOfLaps` | `int` | 4 bytes | 92 | Vueltas totales estipuladas para la carrera |
| `TyreCompound` | `string` | 32 chars (UTF-16) | 96 | Nombre del compuesto (ej. `"Street"`, `"Semislicks"`) |
| `ReplayTimeMultiplier`| `float` | 4 bytes | 128 | Multiplicador de velocidad de reproducción |
| `NormalizedCarPosition`| `float`| 4 bytes | 132 | Progreso en la pista de `0.0f` a `1.0f` |
| `CarCoordinates` | `float[3]` | 12 bytes | 136 | Coordenadas espaciales X, Y, Z del auto del jugador |

---

### 3.3 Estructura Estática (`SPageFileStatic` / `AcSdk.Data.Static`)
*Actualizado una única vez al cargar el circuito/auto.*

| Campo | Tipo | Tamaño / SizeConst | Offset Binario | Unidades / Descripción |
| :--- | :--- | :--- | :--- | :--- |
| `SmVersion` | `string` | 10 chars (UTF-16) | 0 | Versión de memoria compartida (ej. `"1.0"`) |
| `AcVersion` | `string` | 10 chars (UTF-16) | 10 | Versión de Assetto Corsa instalada |
| `NumberOfSessions` | `int` | 4 bytes | 20 | Cantidad de sesiones programadas |
| `NumCars` | `int` | 4 bytes | 24 | Número de coches en el evento |
| `CarModel` | `string` | 32 chars (UTF-16) | 28 | Identificador del auto (ej. `"bmw_m3_e30"`) |
| `Track` | `string` | 32 chars (UTF-16) | 60 | Identificador del circuito (ej. `"monza"`) |
| `PlayerName` | `string` | 32 chars (UTF-16) | 92 | Nombre del piloto |
| `PlayerSurname` | `string` | 32 chars (UTF-16) | 124 | Apellido del piloto |
| `PlayerNick` | `string` | 32 chars (UTF-16) | 156 | Apodo / Gamertag del piloto |
| `SectorCount` | `int` | 4 bytes | 188 | Cantidad de sectores que componen el circuito |
| `MaxTorque` | `float` | 4 bytes | 192 | Torque máximo en Nm |
| `MaxPower` | `float` | 4 bytes | 196 | Potencia máxima en CV / BHP |
| `MaxRpm` | `int` | 4 bytes | 200 | Límite máximo de revoluciones (línea roja) |
| `MaxFuel` | `float` | 4 bytes | 204 | Capacidad máxima del tanque en litros |
| `SuspensionMaxTravel`| `float[4]` | 16 bytes | 208 | Recorrido tope de suspensión por rueda (m) |
| `TyreRadius` | `float[4]` | 16 bytes | 224 | Radio exterior del neumático por rueda (m) |

---

## 4. Hallazgos Cruciales Validados con Datos Reales (CSV)

Del análisis del archivo de telemetría real `01-05-2014_20-18.csv`, se confirmaron empíricamente las siguientes reglas de negocio y representación técnica:

1. **Semántica de `TyreWear`:**
   * El valor arranca en `100.0` y va descendiendo (ej. `99.46696`).
   * **Regla:** Es el **porcentaje de vida útil remanente** del neumático.
2. **Unidades de Presión (`WheelsPressure`):**
   * Los valores observados están en el rango de `32.0` a `38.0`.
   * **Regla:** Están expresados directamente en **PSI**.
3. **Unidades de Temperatura (`TyreCoreTemperature`):**
   * Los valores oscilan entre `60.0` y `100.0`.
   * **Regla:** Están expresados en **grados Celsius (°C)**.
4. **Coordenadas 3D y Generación de Minimapa 2D:**
   * En el espacio tridimensional de Assetto Corsa:
     * `CarCoordinates[0]` representa el eje horizontal **X**.
     * `CarCoordinates[1]` representa la elevación/altura **Y**.
     * `CarCoordinates[2]` representa el eje longitudinal de profundidad **Z**.
   * **Regla para la UI:** Para proyectar el mapa del circuito en 2D sobre la pantalla de Boxes o del Volante, se grafican los ejes **X (0)** y **Z (2)**, descartando la altura Y.
5. **Representación Temporal Dual:**
   * El juego entrega simultáneamente la cadena formateada (ej. `CurrentTime = "1:20:868"`) y el valor entero en milisegundos (`CurrentTimeValue = 80868`).
   * **Regla:** En la base de datos y cálculos internos siempre se debe persistir el valor entero en **milisegundos**, reservando la cadena de texto para visualización directa en pantalla.

---

## 5. Recomendación de Arquitectura en C# (.NET 8/9)

Para garantizar 60 Hz estables y evitar pausas de *Garbage Collection* (GC pauses):
* **No usar** `GCHandle.Alloc` ni `BinaryReader.ReadBytes` en cada cuadro (patrón obsoleto de 2014 que satura la memoria).
* **Usar** `MemoryMappedViewAccessor` con lectura directa por punteros o estructuras `fixed` con `Unsafe.Read<T>()`.
* Proteger la lectura concurrente frente a escrituras parciales del simulador implementando el algoritmo **SeqLock** con barreras de memoria (`Thread.MemoryBarrier()`).
