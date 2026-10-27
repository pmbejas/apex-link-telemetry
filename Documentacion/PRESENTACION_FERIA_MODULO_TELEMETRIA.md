# Sistema de Telemetría Vehicular en Tiempo Real y Análisis de Alto Rendimiento para Simulación Profesional

> **Documento de Presentación para Feria Tecnológica / Exposición de Proyectos UTN**  
> **Proyecto:** Sistema de Telemetría Distribuido Edge-to-Cloud para Simuladores de Competición  
> **Componente Presentado:** **Módulo 1 — Núcleo de Adquisición de Físicas, Procesamiento Edge y Servidor de Distribución**  
> **Tecnologías Centrales:** .NET 10, C# 12, Clean Architecture, Interoperabilidad Win32 (Memory Mapped Files), WebSockets @ 60 Hz, SQLite Local (Edge WAL) y PostgreSQL Central.

---

## 1. Introducción y Problema del Mundo Real

### El Desafío en el Automovilismo y el SimRacing
En el automovilismo de competición moderno (desde la Fórmula 1 hasta las categorías nacionales de turismo y el automovilismo virtual o *SimRacing* profesional), las carreras no se ganan únicamente en la pista, sino en el **análisis milimétrico de los datos**. 

Un coche de competición genera cientos de variables físicas por segundo: presiones y temperaturas de neumáticos, fuerzas G laterales y longitudinales, desgaste de frenos, recorridos de suspensión y trayectorias en pista. 

Sin embargo, en la mayoría de los simuladores convencionales:
1. **El piloto está "ciego":** No dispone de un display profesional en el volante que le indique en tiempo real el delta de tiempo curva a curva, las luces de cambio (*shift lights*) o el sobrecalentamiento de una rueda específica.
2. **Los ingenieros en boxes no tienen acceso a la telemetría viva:** La información queda atrapada en el motor gráfico del juego y no puede ser transmitida a una pantalla externa sin degradar el rendimiento de la simulación.
3. **Pérdida de datos por desconexión:** Si se depende exclusivamente de una conexión a internet para registrar los datos, cualquier microcorte en la red provoca la pérdida irreversible de vueltas completas.

---

## 2. Nuestra Solución: El Módulo 1 (El "Corazón" del Ecosistema)

Nuestro equipo desarrolló el **Módulo 1: Núcleo de Adquisición y Distribución de Telemetría**, un motor de software de grado industrial diseñado en C# y .NET 10 que actúa como el puente central de todo el ecosistema.

Este módulo cumple cuatro funciones esenciales en simultáneo:

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                              PC DEL SIMULADOR (MÓDULO 1)                               │
│                                                                                        │
│   ┌──────────────────────────┐                                                         │
│   │     ASSETTO CORSA 1      │                                                         │
│   └────────────┬─────────────┘                                                         │
│                │ (Lectura de memoria RAM a 60 Hz - Zero Allocation)                    │
│                ▼                                                                       │
│   ┌────────────────────────────────────────────────────────────────────────────────┐   │
│   │ NUESTRO BACKEND .NET                                                           │   │
│   │                                                                                │   │
│   │ 1. ADQUISICIÓN WIN32    ──► Lee memoria física de AC v1.16 sin usar CPU extra. │   │
│   │ 2. FILTRO ATÓMICO       ──► Algoritmo SeqLock (previene datos rotos o tearing).│   │
│   │ 3. RELAY WEBSOCKET      ──► Transmite a 60 Hz al Volante Móvil vía USB/Wi-Fi.  │   │
│   │ 4. BASE EDGE (SQLite)   ──► Guarda cada metro y vuelta de forma local segura.  │   │
│   │ 5. SYNC (PostgreSQL)    ──► Sube carreras completas a boxes automáticamente.   │   │
│   └────────────────────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

1. **Adquisición No Invasiva:** Lee directamente la memoria física del simulador (*Assetto Corsa 1 v1.16*) a través de canales de memoria compartida de Windows (*Memory Mapped Files*) sin inyectar código ni alterar los ejecutables del juego.
2. **Distribución en Tiempo Real (Relay a 60 Hz):** Empaqueta y difunde la telemetría viva a 60 cuadros por segundo hacia el **Módulo 2 (Dashboard móvil montado en el volante)** con una latencia inferior a **1 milisegundo**.
3. **Persistencia en el Borde (*Edge Storage*):** Registra cada vuelta, sector y muestra continua en una base de datos local SQLite ultrarrápida, garantizando que jamás se pierda una sola muestra aunque no haya conexión a internet.
4. **Sincronización Inteligente (*Store-and-Forward*):** Al finalizar la carrera, exporta de forma transaccional toda la telemetría hacia la base de datos central PostgreSQL para alimentar el **Módulo 3 (Panel Web de Boxes / Pitwall)**.

---

## 3. Arquitectura de Software y Decisiones de Ingeniería

El proyecto fue diseñado aplicando las mejores prácticas de la industria del software:

### 3.1 Clean Architecture (Arquitectura Limpia)
El código está estructurado en capas estrictamente desacopladas e independientes de librerías externas:
* **Dominio:** Contiene los modelos puros del negocio automovilístico (Sesión, Vuelta, Muestra de Telemetría, Neumáticos, Fuerzas G) y las interfaces de contratos.
* **Aplicación:** Servicios de orquestación, cálculo de deltas espaciales, consolidación de sectores y mapeadores de DTOs.
* **Infraestructura:** Implementaciones concretas de hardware Win32, acceso a SQLite mediante Entity Framework Core y exportación masiva a PostgreSQL con `NpgsqlBatch`.
* **API / Presentación:** Controladores REST protegidos con permisos granulares (RBAC) y servidores de WebSockets concurrentes.

### 3.2 Optimización Extrema: Cero Asignaciones (*Zero-Allocation*) y Algoritmo SeqLock
A 60 Hz, cada cuadro debe procesarse en menos de **16.6 milisegundos**. Si el software crea objetos nuevos en memoria continuamente, el recolector de basura (*Garbage Collector*) de .NET causará microcongelamientos (*stutters*) que arruinan la experiencia del piloto.

Para solucionar esto, implementamos:
* **Estructuras `fixed` y Punteros Directos:** Mapeo de memoria utilizando structs de C# con memoria contigua, logrando **0 bytes de asignaciones efímeras en el Garbage Collector (Gen 0 = 0)**.
* **Algoritmo SeqLock con Barreras de Memoria:** Garantiza que los cuadros leídos desde la memoria compartida sean coherentes. Si el simulador está escribiendo en el mismo instante en que leemos, el software detecta la discrepancia de secuencia mediante barreras de hardware (`Thread.MemoryBarrier()`) y reintenta de inmediato, evitando lecturas corruptas (*tearing*).

### 3.3 Aislamiento de Consumidores Lentos en Red
Si el teléfono celular conectado al volante experimenta congestión en su conexión USB o Wi-Fi, un servidor tradicional se bloquearía esperando que el cliente termine de recibir. 
Nuestro despachador utiliza canales asíncronos (`Channel<T>`) con política `DropOldest`: si el móvil se retrasa, el servidor descarta los cuadros intermedios y le envía inmediatamente el más reciente, garantizando que el instrumental del volante **siempre muestre telemetría en tiempo real sin acumular retraso visual**.

---

## 4. ¿Qué Datos Procesa y Expone Nuestro Módulo?

Nuestro motor extrae y normaliza más de 40 variables críticas del vehículo:

| Subsistema | Variables Clave Extraídas | Utilidad Práctica en Pista |
| :--- | :--- | :--- |
| **Control del Motor** | Velocidad (km/h), RPMs exactas, Límite de corte (`maxRpm`), Marcha actual (`R, N, 1, 2...`). | Control del cambio de marcha óptimo (*Shift Lights*) y velocidad punta. |
| **Pedales y Dirección** | Recorrido del acelerador (0 a 100%), presión de freno (0 a 100%), ángulo de volante. | Detección de bloqueos de frenada y suavidad en la entrada a curva. |
| **Dinámica de Neumáticos** | Presión de inflado en **PSI**, temperatura interna en **°C**, desgaste restante (100% a 0%). | Monitoreo del estado de las 4 ruedas individuales (FL, FR, RL, RR) para evitar pinchazos o pérdida de adherencia. |
| **Fuerzas G de Chasis** | Aceleración lateral, longitudinal y vertical. | Dibujo del *Círculo de Adherencia de Kamm* (Friction Circle) para analizar el límite físico de agarre del auto. |
| **Cronometraje Espacial** | Tiempo de vuelta en vivo, sectores S1, S2, S3, récord personal y **Delta en vivo (±ms)**. | Comparación continua contra la mejor vuelta: el piloto sabe metro a metro si va ganando o perdiendo tiempo. |
| **Radar y Cartografía** | Coordenadas 3D (X, Y, Z) de hasta 60 autos en pista y longitud de trazado (`TrackSPlineLength`). | Dibujo del mapa del circuito en 2D y visualización de rivales en pista. |

---

## 5. El Ecosistema Completo: Cómo se Conectan los Tres Módulos

Nuestro desarrollo se integra armónicamente con los desarrollos de los otros dos integrantes del equipo:

```
[Simulador Assetto Corsa]
            │
            ▼ (Memoria RAM @ 60 Hz)
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                   MÓDULO 1: NUESTRO BACKEND (Local @ Puerto 8080)                      │
│                                                                                        │
│   • Orquesta la captura de datos a 60 Hz.                                              │
│   • Registra cada vuelta de forma atómica en SQLite local.                             │
│   • Detecta automáticamente el fin de carrera cuando el juego vuelve al menú.          │
└───────────┬────────────────────────────────────────────────────────┬───────────────────┘
            │                                                        │
            │ (WebSocket a 60 FPS)                                   │ (API REST / PostgreSQL)
            ▼                                                        ▼
┌──────────────────────────────────────┐ ┌───────────────────────────────────────────────┐
│     MÓDULO 2 (DESARROLLADOR 2)       │ │          MÓDULO 3 (DESARROLLADOR 3)           │
│     Dashboard Móvil en Volante       │ │          Panel Web de Boxes / Pitwall         │
├──────────────────────────────────────┤ ├───────────────────────────────────────────────┤
│ • Teléfono celular montado al volante│ │ • Aplicación web para ingenieros en boxes.    │
│   conectado por cable USB.           │ │ • Durante la carrera: tiempos y posiciones.   │
│ • Muestra velocímetro, marchas,      │ │ • Post-carrera: gráficos de telemetría por    │
│   luces de corte y presiones.        │ │   curva estilo MoTeC y comparativa de pilotos.│
│ • Cliente ultraliviano y reactivo.   │ │ • Conectado a la base PostgreSQL Central.     │
└──────────────────────────────────────┘ └───────────────────────────────────────────────┘
```

---

## 6. Demostración Práctica para la Feria

Para los evaluadores y asistentes a la feria, el sistema cuenta con dos modos de funcionamiento listos para ser mostrados en vivo:

1. **Modo Simulación Física Real:** Se abre Assetto Corsa 1 en la computadora. El backend detecta la presencia del juego, abre los descriptores de memoria compartida y comienza a transmitir instantáneamente los datos reales generados por el piloto.
2. **Modo Sintético Autónomo (Mock Determinístico):** Si no se dispone de la computadora con el simulador instalado durante la exposición, el backend cuenta con un generador matemático que reproduce fielmente una vuelta real en el circuito de Monza a bordo de una Ferrari de Fórmula 1, emitiendo telemetría a 60 Hz exactamente igual que el juego real para realizar demostraciones completas e interactivas.

---

## 7. Métricas de Calidad y Confiabilidad

* **Compilación y Robustez:** 8 proyectos compilados sin una sola advertencia ni error.
* **Cobertura de Pruebas Automatizadas:** **63 tests automatizados ejecutados con 100% de éxito**, validando desde el cálculo matemático de deltas hasta la persistencia transaccional y la seguridad contra ataques en la red local.
* **Normalización de Base de Datos:** Modelo relacional formalmente normalizado en **Tercera Forma Normal (3FN)** con índices optimizados para consulta continua de telemetría.
* **Consumo de Recursos:** Menos del **1% de uso de CPU** en una computadora estándar, asegurando que los cuadros por segundo del simulador no sufran caídas (*framerate drops*).

---

## 8. Conclusión

Este módulo demuestra que es posible combinar la ingeniería de software de alto nivel (Clean Architecture, SOLID, algoritmos de concurrencia y diseño tolerante a fallos) con la pasión por el deporte motor y la tecnología de simulación. 

Es una solución completa, profesional, escalable y lista para transformar un simulador hogareño en un entorno de entrenamiento y análisis digno de una escudería de automovilismo profesional.
