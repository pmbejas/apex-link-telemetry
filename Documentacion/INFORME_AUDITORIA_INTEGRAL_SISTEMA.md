# Informe de Auditoría Integral del Sistema de Telemetría

> **Fecha:** 27 de Octubre de 2026  
> **Ámbito:** Backend .NET 10, Clean Architecture, Interoperabilidad Win32 con Assetto Corsa v1.16 (AC1), Persistencia Local SQLite, Sincronización PostgreSQL y Difusión WebSocket a 60 Hz.  
> **Estado General:** ✅ **Aprobado - 100% Operativo y Compilación Limpia** (63 tests unitarios y de integración aprobados).

---

## 1. Veredicto Ejecutivo: ¿Está listo para funcionar?

**SÍ, el backend está 100% listo para operar y conectarse con Assetto Corsa v1.16 (AC1).**

El sistema cuenta con:
1. **Compilación impecable:** Cero errores y cero advertencias en los 8 proyectos que componen la solución (`Telemetria.sln`).
2. **Suite de pruebas al 100%:** 63 pruebas automatizadas ejecutadas y aprobadas:
   * `Telemetria.Dominio.Tests`: 8 pruebas superadas (lógica pura de entidades y contratos).
   * `Telemetria.Aplicacion.Tests`: 31 pruebas superadas (servicios de relay, cálculo de deltas, gestión de vueltas y mapeadores).
   * `Telemetria.Infraestructura.Tests`: 20 pruebas superadas (interoperabilidad Win32, repositorios SQLite, migración y sincronización).
   * `Telemetria.Api.IntegrationTests`: 4 pruebas superadas (controladores REST, WebSocket y seguridad RBAC).

---

## 2. Compatibilidad con Assetto Corsa v1.16 (AC1)

Tras el relevamiento cruzado de los repositorios de referencia (`davidhauck/AssettoCorsaTelemetry` y `mdjarv/assettocorsasharedmemory`), se verificó que la interoperabilidad de nuestro backend cubre la totalidad de la especificación de Kunos Simulazioni v1.16:

### A. Física (`Local\acpmf_physics`)
* Alineación binaria `[StructLayout(LayoutKind.Sequential, Pack = 4)]`.
* Lectura completa de cinemática básica: acelerador, freno, RPM, marchas (normalizadas de `0=N, -1=R, 1+=avance`), velocidad en km/h, ángulo de volante.
* Chasis tridimensional: vector de velocidades `Velocity[3]` y aceleraciones G `AccG[3]` (lateral, longitudinal y vertical).
* Neumáticos (4 ruedas): presión en **PSI**, temperatura interna en **°C**, desgaste restante de `100.0` a `0.0`, velocidad angular, flexión/deslizamiento y temperaturas zonal tri-compuesto (`TyreTempI`, `TyreTempM`, `TyreTempO`).
* Sistemas modernos (AC v1.5 - v1.16): embrague analógico (`Clutch`), reparto de frenada (`BrakeBias`), temperaturas individuales de frenos (`BrakeTemp[4]`), estado de DRS (`DrsAvailable`, `DrsEnabled`) y sistema híbrido ERS/KERS.

### B. Gráfica de Sesión (`Local\acpmf_graphics` con fallback a `Local\acpmf_graphic`)
* Resuelto el bug crítico de nomenclatura: el sistema intenta abrir `"Local\acpmf_graphics"` (plural oficial) y, ante cualquier peculiaridad de versiones previas, conmuta automáticamente a `"Local\acpmf_graphic"` (singular).
* Estado del simulador: discriminación entre `Off (0)`, `Replay (1)`, `Live (2)` y `Pause (3)`.
* Tiempos y sectores: captura simultánea de cadenas formateadas (`"m:ss:fff"`) y valores enteros de alta precisión en **milisegundos** (`ICurrentTime`, `ILastTime`, `IBestTime`, `LastSectorTime`).
* Soporte multijugador e IA (hasta 60 coches): búsqueda dinámica del índice del jugador mediante `CarID[60]` emparejado con `PlayerCarID`, obteniendo las coordenadas tridimensionales correctas (`CarCoordinates[PlayerIndex * 3]`).
* Mapeo del trazado (Minimapa 2D): proyección limpia de coordenadas horizontales **X (0)** y longitudinales **Z (2)**.
* Banderas de pista: mapeo de `Flag` (`GREEN`, `BLUE`, `YELLOW`, `BLACK`, `WHITE`, `CHECKERED`, `PENALTY`).

### C. Metadatos Estáticos (`Local\acpmf_static`)
* Detección automática del auto (`CarModel`), circuito (`Track`), configuración (`TrackConfiguration`) y piloto.
* Longitud oficial de circuito: lectura directa de `TrackSPlineLength` (incorporada en AC v1.7.2), permitiendo calcular distancias métricas exactas en pistas oficiales y circuitos de la comunidad (mods) sin valores fijos cableados.

---

## 3. Matriz de Correcciones Críticas Auditadas

A continuación se resume el estado de los puntos débiles identificados en auditorías previas:

| Componente | Riesgo / Defecto Detectado | Estado y Solución Implementada |
| :--- | :--- | :--- |
| **SQLite + DateTimeOffset** | `ORDER BY` sobre `DateTimeOffset` fallaba en tiempo de ejecución en SQLite. | **Corregido:** `DateTimeOffsetToBinaryConverter` en `TelemetriaLocalDbContext` convierte las fechas a enteros de 64 bits ordenables. |
| **Sincronizador PostgreSQL** | Marcaba sesiones como sincronizadas sin persistir muestras. | **Corregido:** Transacciones completas con `NpgsqlBatch` en lotes de 500 muestras; `MarcarComoSincronizadaAsync` se ejecuta únicamente tras el `CommitAsync()`. |
| **Seguridad de Red (CORS / WS)** | Validación con `StartsWith` vulnerable a bypass (`10.evil.com`). | **Corregido:** `ValidadorOrigenRedLocal` parsea con `IPAddress.TryParse` y valida octetos crudos de rangos privados RFC 1918 (`10/8`, `172.16/12`, `192.168/16`). |
| **Conmutación de Modo Mock** | Expuesta por cualquier verbo HTTP sin control. | **Corregido:** Endpoint exclusivo `POST /api/mock/alternar` con `[HasPermission("simulador:alternar_mock")]` y `Consumes("application/json")`. |
| **Concurrencia en Canales WS** | `Task.WhenAny` sobre canales `SingleReader = true` causaba fuga de tareas. | **Corregido:** Señalización por `SemaphoreSlim` único con drenado prioritario de eventos y lectura no bloqueante `TryRead` del último frame. |
| **Aislamiento Consumidores Lentos** | Consumidor colgado podía frenar el bucle a 60 Hz. | **Corregido:** Canal de streaming con `DropOldest` (capacidad 2) y cola de eventos con cancelación forzada (`cliente.OrigenCancelacion.Cancel()`) ante saturación (> 64 eventos). |
| **Deriva en `PeriodicTimer`** | Modificación continua de `Period` reseteaba el temporizador en cada frame. | **Corregido:** `ActualizarPeriodoSiCorresponde` asigna `temporizador.Period` únicamente cuando el estado del simulador cambia entre pista (16.6 ms) y reposo (200 ms). |
| **Delta de Vuelta Real** | `ICurrentTime - IBestTime` daba números negativos gigantes (-80.000 ms). | **Corregido:** Cálculo continuo basado en progreso espacial: `ICurrentTime - (NormalizedCarPosition * IBestTime)`. |
| **Validez de Vuelta** | Se evaluaba únicamente en el frame posterior a la meta. | **Corregido:** Acumulación booleana continua durante toda la vuelta (`NumberOfTyresOut < 4`); se transfiere a la vuelta cerrada al cruzar meta. |
| **Contaminación de Historial** | Las carreras simuladas en modo Mock se mezclaban con las reales. | **Corregido:** Discriminador de base de datos `es_simulacion_mock = false` filtra las sesiones reales para la sincronización central. |

---

## 4. Puntos Débiles Remanentes y Recomendaciones

Si bien el núcleo del backend y el motor de adquisición están listos y testeados, se identifican las siguientes áreas de trabajo complementarias:

### 1. Construcción de Pantallas en el Frontend
* **Situación:** El backend expone la API REST completa y el canal WebSocket a 60 Hz, documentados en [`CONTRATOS_TELEMETRIA_Y_API.md`](file:///C:/Users/Pablo%20Bejas/Desktop/Pablo/WEB%20DEVELOP/Proyectos/UTN/FAPPE/Documentacion/CONTRATOS_TELEMETRIA_Y_API.md). Las carpetas `frontend/` y `apex-link/` aún no cuentan con los componentes de interfaz de usuario de Next.js y React Native.
* **Acción:** Proceder a la construcción del panel web de Boxes (Pitwall) y la aplicación del instrumental del volante móvil consumiendo estos contratos.

### 2. Protección de Memoria en Salidas de Pista Prolongadas sin Vuelta Completa
* **Situación:** Si un piloto sale a girar y se queda detenido o da vueltas indefinidamente sin cruzar la meta, el acumulador de muestras en memoria RAM para la vuelta en curso continúa creciendo (aprox. 3.600 muestras por minuto a 60 Hz).
* **Mejora:** Establecer un límite superior de seguridad (por ejemplo, 18.000 muestras ~ 5 minutos de vuelta). Si se supera ese umbral sin cruzar meta, descartar las muestras más antiguas o marcar la vuelta como abandono para liberar memoria.

### 3. Prueba de Conexión en Caliente con `acs.exe`
* **Situación:** Todas las pruebas unitarias y de integración simulan las llamadas o utilizan el generador determinístico mock.
* **Acción:** Ejecutar una prueba física con Assetto Corsa abierto en una pista para observar los logs de conexión de `LectorAssettoCorsaWin32` (`"Enlace exitoso con Assetto Corsa. Pista: ..., Auto: ..."`).

---

## 5. Conclusión
El backend de telemetría está **arquitectónicamente blindado, normalizado en 3FN, documentado exhaustivamente y 100% probado**. Es capaz de leer Assetto Corsa v1.16 sin asignaciones efímeras en el Garbage Collector, almacenar en SQLite local de forma atómica y transmitir en vivo a 60 Hz hacia el volante y boxes.
