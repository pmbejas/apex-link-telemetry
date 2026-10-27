# 📡 Catálogo de Contratos y DTOs de Telemetría: Tiempo Real y Post-Sesión

> **Destinatarios:** Desarrollador 2 (Display de Cabina Android / PWA) y Desarrollador 3 (Plataforma Web de Boxes en Next.js).  
> **Servidor Host:** Backend ASP.NET Core Kestrel (`http://localhost:8080` y `ws://localhost:8080`).  
> **Enlace Físico Móvil:** Túnel USB determinístico mediante `adb reverse tcp:8080 tcp:8080`.

---

## 🧭 1. Resumen de Canales y Arquitectura de Consumo

La transmisión de datos se divide bajo el principio de **Segregación de Responsabilidades (CQRS Conceptual)** en dos canales independientes:

1. **Canal en Tiempo Real (WebSocket Push - `ws://localhost:8080/ws/telemetry`):**
   - Transmisión continua unidireccional de baja sobrecarga a **60 Hz** en pista activa y **5 Hz** en menú/pausa.
   - Diseñado para el **display del volante (Dev 2)** y el instrumental instantáneo de boxes.
   - Emite dos tipos de mensajes JSON: telemetría continua (`FRAME`) y eventos de cierre de vuelta (`LAP_COMPLETED`).

2. **Canal Analítico Post-Sesión (API REST Pull - `http://localhost:8080/api/...`):**
   - Interfaz HTTP sin estado orientada a recursos, normalizada en el **Dominio de Distancia Métrica ($0$ a $L_{\text{circuito}}$ metros)**.
   - Diseñado para la **estación web de telemetría de boxes (Dev 3)** para consultar historiales de tandas finalizadas, vueltas individuales, telemetría metro a metro y gráficos comparativos con deltas interpolados.

---

## ⚡ 2. Datos Expuestos en Tiempo Real (WebSocket: `/ws/telemetry`)

El socket opera sin necesidad de que el cliente envíe comandos de suscripción; tras completar el apretón de manos (*Handshake* RFC 6455), el servidor comienza el envío continuo. Los mensajes son objetos JSON tipados discriminados por la propiedad `"type"`.

### A. Mensaje Continuo: `FRAME` (Frecuencia: 60 Hz en Pista / 5 Hz en Pausa)

Contiene la totalidad de las variables cinemáticas, controles del monoplaza, dinámicas de neumáticos, cronometraje y fuerzas G del cuadro instantáneo.

#### 1. Carga Útil JSON de Ejemplo
```json
{
  "type": "FRAME",
  "timestamp": 1729705824120,
  "car": {
    "speedKmh": 312.4,
    "rpms": 12450,
    "maxRpm": 13000,
    "gear": 7,
    "throttle": 1.0,
    "brake": 0.0,
    "steer": 0.02,
    "drsAvailable": true,
    "drsEnabled": true
  },
  "tyres": {
    "pressurePsi": [22.4, 22.8, 21.9, 22.1],
    "tempCoreC": [94.2, 96.1, 89.5, 90.3],
    "wear": [98.4, 97.9, 99.1, 98.8]
  },
  "timing": {
    "lapTimeMs": 42350,
    "lapTimeStr": "00:42.350",
    "bestTimeMs": 81240,
    "bestTimeStr": "01:21.240",
    "deltaMs": -180,
    "currentLap": 3,
    "sector": 2,
    "lapProgress": 0.5215
  },
  "dynamics": {
    "gForce": {
      "lat": 0.45,
      "lon": 0.85,
      "vert": 0.05
    },
    "worldPos": {
      "x": 245.32,
      "y": 12.0,
      "z": -412.18
    }
  },
  "session": {
    "flag": "GREEN",
    "position": 1,
    "track": "monza",
    "carModel": "ks_ferrari_sf70h"
  }
}
```

#### 2. Diccionario de Campos, Tipos y Unidades

| Objeto / Propiedad | Tipo | Unidad | Rango / Valores | Significado Técnico y Convenciones |
| :--- | :--- | :--- | :--- | :--- |
| `type` | `string` | Literal | `"FRAME"` | Discriminador de tipo de paquete WebSocket. |
| `timestamp` | `long` | Milisegundos | Unix Epoch UTC | Marca de tiempo del cuadro en el servidor. |
| **`car.speedKmh`** | `number` | km/h | `[0.0, 400.0]` | Velocidad escalar instantánea del vehículo. |
| **`car.rpms`** | `number` | RPM | `[0, 20000]` | Régimen de giro actual del cigüeñal. |
| **`car.maxRpm`** | `number` | RPM | `[0, 20000]` | Límite superior para cálculo del tacómetro / Rev LEDs. |
| **`car.gear`** | `number` | Entero | `[-1, 8]` | Marcha activa: `-1` = Reversa, `0` = Neutral, `1..8` = 1ra a 8va. |
| **`car.throttle`** | `number` | Normalizado | `[0.0, 1.0]` | Recorrido del pedal de acelerador (`0.0` = suelto, `1.0` = fondo). |
| **`car.brake`** | `number` | Normalizado | `[0.0, 1.0]` | Recorrido del pedal de freno (`0.0` = suelto, `1.0` = bloqueo). |
| **`car.steer`** | `number` | Normalizado | `[-1.0, 1.0]` | Ángulo de volante (`-1.0` = tope izq, `0.0` = centro, `1.0` = tope der). |
| **`car.drsAvailable`**| `boolean`| Lógico | `true` / `false` | Indica si el alerón móvil DRS puede ser accionado. |
| **`car.drsEnabled`**  | `boolean`| Lógico | `true` / `false` | Indica si el ala trasera DRS se encuentra abierta en el cuadro. |
| **`tyres.*`** | `number[]`| - | Vector de 4 elementos | **Orden canónico invariable:** `[0]` = Delantero Izquierdo (FL), `[1]` = Delantero Derecho (FR), `[2]` = Trasero Izquierdo (RL), `[3]` = Trasero Derecho (RR). |
| `tyres.pressurePsi` | `number[]`| PSI | `[10.0, 40.0]` | Presión de inflado de las 4 ruedas. |
| `tyres.tempCoreC` | `number[]`| °C | `[20.0, 160.0]` | Temperatura del núcleo del compuesto de cada neumático. |
| `tyres.wear` | `number[]`| Porcentaje | `[0.0, 100.0]` | **Vida útil restante** del neumático (`100.0` = compuesto nuevo). |
| **`timing.lapTimeMs`**| `number` | Milisegundos | $\ge 0$ | Tiempo transcurrido en la vuelta activa en curso. |
| `timing.lapTimeStr` | `string` | Texto | `"mm:ss.fff"` | Formato oficial internacional de cronometraje en vivo. |
| `timing.bestTimeMs` | `number` | Milisegundos | $\ge 0$ | Mejor tiempo histórico de la sesión (`0` si es vuelta 1). |
| `timing.bestTimeStr`| `string` | Texto | `"--:--.---"` | Formato texto del mejor registro (`"--:--.---"` si aún no cerró vuelta). |
| `timing.deltaMs` | `number` | Milisegundos | $[-\infty, +\infty]$ | Comparativa contra referencia: **negativo (< 0) = más rápido (Verde)**, positivo (> 0) = más lento (Rojo). |
| `timing.currentLap` | `number` | Entero | $\ge 0$ (Base 0) | Vueltas cerradas previas (`0` indica la vuelta de salida o calentamiento). |
| `timing.sector` | `number` | Entero | `1`, `2` o `3` | Sector del circuito transitado actualmente (base 1). |
| `timing.lapProgress`| `number` | Normalizado | `[0.0, 1.0]` | Progreso métrico acumulado sobre el perímetro de la pista. |
| `dynamics.gForce` | `object` | G ($9.81\text{ m/s}^2$) | `[-5.0, 5.0]` | Aceleraciones del chasis (`lat`: lateral, `lon`: longitudinal, `vert`: vertical). |
| `dynamics.worldPos` | `object` | Metros | Cartesiano | Coordenadas espaciales del auto en pista (`x`, `y`, `z`). |
| `session.flag` | `string` | Texto | Banderas FIA | `"GREEN"`, `"YELLOW"`, `"BLUE"`, `"BLACK"`, `"WHITE"`, `"CHECKERED"`, `"PENALTY"`. |
| `session.position` | `number` | Entero | $\ge 1$ | Posición de carrera del monoplaza en la grilla. |
| `session.track` | `string` | Texto | Identificador | Nombre canónico de la pista (ej. `"monza"`, `"spa"`). |
| `session.carModel` | `string` | Texto | Identificador | Modelo del vehículo (ej. `"ks_ferrari_sf70h"`). |

---

### B. Mensaje de Evento: `LAP_COMPLETED` (Prioritario, sin pérdida)

Se despacha en el instante exacto en que el monoplaza cruza la línea de meta confirmando el cierre de una vuelta oficial.

#### 1. Carga Útil JSON de Ejemplo
```json
{
  "type": "LAP_COMPLETED",
  "lapNumber": 3,
  "lapTimeMs": 81240,
  "lapTimeStr": "01:21.240",
  "isPersonalBest": true,
  "isValidLap": true,
  "sectors": [28100, 27450, 25690],
  "telemetrySamplesCount": 4872,
  "sessionLapId": "a3b8c9d0-1234-5678-9abc-def012345678"
}
```

#### 2. Propósito y Correlación con la Base de Datos
- **`sessionLapId`:** Es el `Guid` unívoco con el que la vuelta y sus ~4.800 muestras quedaron persistidas en la base SQLite local.
- **Acción Inmediata en Boxes (Dev 3):** La plataforma web utiliza este identificador para invocar automáticamente `GET /api/vueltas/{sessionLapId}/telemetria` y graficar la telemetría completa de la vuelta recién culminada sin riesgo de obtener un error 404 (*Garantía Read-After-Write*).
- **Acción en el Volante (Dev 2):** Permite presentar la pantalla de felicitación o banner de vuelta ("P.B. 01:21.240" en verde si `isPersonalBest == true`).

---

## 🏁 3. Datos Devueltos al Finalizar la Carrera / Sesión (API REST)

Los endpoints analíticos operan sobre el puerto `8080`. Todas las respuestas exitosas o fallidas se encapsulan en el envoltorio unificado `RespuestaApi<T>`.

### A. Estructura de Respuesta Estándar

```json
{
  "exitoso": true,
  "mensaje": "Operación completada satisfactoriamente",
  "datos": { ... },
  "errores": []
}
```

Si la petición falla, `exitoso` es `false`, `datos` es `null`, y `errores` contiene el detalle legible en español para la interfaz de usuario.

---

### B. Catálogo de Endpoints Analíticos

#### 1. Estado General del Servicio y Circuito
* **Método:** `GET`
* **Ruta:** `/api/estado`
* **Propósito:** Conocer si el simulador está activo, el circuito cargado y su longitud métrica total.
* **Respuesta (`EstadoServicioDto`):**
```json
{
  "exitoso": true,
  "mensaje": "Operación completada satisfactoriamente",
  "datos": {
    "estaConectado": true,
    "modoMock": false,
    "circuito": "monza",
    "modeloVehiculo": "ks_ferrari_sf70h",
    "longitudPistaMetros": 5793.0,
    "autosActivos": 1
  },
  "errores": []
}
```

---

#### 2. Historial de Sesiones / Tandas Registradas (Paginado)
* **Método:** `GET`
* **Ruta:** `/api/sesiones?numeroPagina=1&tamanioPagina=10`
* **Propósito:** Mostrar en la web de boxes el selector histórico de tandas completadas, ordenadas descendentemente por fecha de inicio.
* **Respuesta (`PaginacionRespuesta<SesionResumenDto>`):**
```json
{
  "exitoso": true,
  "mensaje": "Operación completada satisfactoriamente",
  "datos": {
    "elementos": [
      {
        "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
        "circuito": "monza",
        "configuracionCircuito": "grand_prix",
        "modeloVehiculo": "ks_ferrari_sf70h",
        "tipoSesion": 2,
        "esSimulacionMock": false,
        "fechaInicio": "2026-10-26T18:30:00Z",
        "fechaFin": "2026-10-26T19:15:30Z",
        "totalVueltas": 24,
        "mejorTiempoMs": 81240
      }
    ],
    "paginaActual": 1,
    "tamanioPagina": 10,
    "totalRegistros": 1,
    "totalPaginas": 1,
    "tienePaginaSiguiente": false,
    "tienePaginaAnterior": false
  },
  "errores": []
}
```

> **Convención de `tipoSesion`:** `0` = Práctica Libre (*Practice*), `1` = Clasificación (*Qualifying*), `2` = Carrera (*Race*).

---

#### 3. Catálogo de Vueltas de una Tanda Específica
* **Método:** `GET`
* **Ruta:** `/api/sesiones/{sesionId}/vueltas`
* **Propósito:** Extraer la tabla de clasificación de vueltas de una tanda (tiempos totales, splits de los 3 sectores y distintivos de vuelta récord o inválida).
* **Respuesta (`List<VueltaDto>`):**
```json
{
  "exitoso": true,
  "mensaje": "Operación completada satisfactoriamente",
  "datos": [
    {
      "id": "a3b8c9d0-1234-5678-9abc-def012345678",
      "numeroVuelta": 1,
      "tiempoVueltaMs": 83450,
      "tiempoVueltaTexto": "01:23.450",
      "sector1Ms": 28900,
      "sector2Ms": 28100,
      "sector3Ms": 26450,
      "esValida": true,
      "esMejorVuelta": false
    },
    {
      "id": "b7c8d9e0-2345-6789-0abc-ef0123456789",
      "numeroVuelta": 2,
      "tiempoVueltaMs": 81240,
      "tiempoVueltaTexto": "01:21.240",
      "sector1Ms": 28100,
      "sector2Ms": 27450,
      "sector3Ms": 25690,
      "esValida": true,
      "esMejorVuelta": true
    }
  ],
  "errores": []
}
```

---

#### 4. Vector Denso de Muestras de Telemetría por Vuelta (Dominio de Distancia)
* **Método:** `GET`
* **Ruta:** `/api/vueltas/{vueltaId}/telemetria`
* **Propósito:** Alimentar los gráficos de ingeniería de pista (uPlot, Canvas o Recharts). Retorna entre 4.000 y 5.500 registros métricos ordenados estrictamente por `distanciaPistaMetros` ascendente ($0.0 \to L_{\text{circuito}}$).
* **Respuesta (`List<MuestraTelemetriaDto>`):**
```json
{
  "exitoso": true,
  "mensaje": "Operación completada satisfactoriamente",
  "datos": [
    {
      "distanciaPistaMetros": 0.0,
      "velocidadKmh": 284.5,
      "acelerador": 1.0,
      "freno": 0.0,
      "marcha": 7,
      "rpms": 11800,
      "fuerzaGLat": 0.05,
      "fuerzaGLon": 0.35,
      "posicionX": 0.0,
      "posicionY": 12.0,
      "posicionZ": 0.0
    },
    {
      "distanciaPistaMetros": 1.25,
      "velocidadKmh": 285.1,
      "acelerador": 1.0,
      "freno": 0.0,
      "marcha": 7,
      "rpms": 11825,
      "fuerzaGLat": 0.04,
      "fuerzaGLon": 0.34,
      "posicionX": 1.25,
      "posicionY": 12.0,
      "posicionZ": 0.1
    }
  ],
  "errores": []
}
```

---

#### 5. Comparativa Analítica Superpuesta entre Dos Vueltas (Overlay con Deltas)
* **Método:** `GET`
* **Ruta:** `/api/vueltas/comparativa?vueltaBaseId={guid}&vueltaComparadaId={guid}`
* **Propósito:** Comparar vuelta rápida vs vuelta de referencia sin desfasajes temporales. El servidor interpola linealmente metro a metro la vuelta comparada sobre la grilla métrica de la vuelta base y calcula el delta de tiempo acumulado.
* **Respuesta (`ComparativaVueltasDto`):**
```json
{
  "exitoso": true,
  "mensaje": "Operación completada satisfactoriamente",
  "datos": {
    "vueltaBaseId": "b7c8d9e0-2345-6789-0abc-ef0123456789",
    "vueltaComparadaId": "a3b8c9d0-1234-5678-9abc-def012345678",
    "totalMuestrasBase": 4872,
    "totalMuestrasComparada": 5012,
    "puntosComparados": [
      {
        "distanciaPistaMetros": 1150.0,
        "velocidadBaseKmh": 315.2,
        "velocidadComparadaKmh": 285.0,
        "deltaVelocidadKmh": -30.2,
        "aceleradorBase": 0.0,
        "aceleradorComparada": 0.0,
        "frenoBase": 1.0,
        "frenoComparada": 0.85,
        "deltaTiempoAcumuladoMs": -240
      }
    ]
  },
  "errores": []
}
```

> **Interpretación Analítica:** En el metro `1150.0` (frenada de Variante del Rettifilo), la vuelta comparada circulaba $30.2\text{ km/h}$ más lento debido a haber frenado antes, acumulando una pérdida de tiempo de $240\text{ ms}$ en ese punto.

---

#### 6. Conmutador en Caliente Mock / Real (Exclusivo Desarrollo)
* **Método:** `POST`
* **Ruta:** `/api/mock/alternar`
* **Cabecera requerida:** `Content-Type: application/json`
* **Cuerpo opcional:** `{"usarMock": true}` (si se omite el cuerpo, invierte el modo actual).
* **Respuesta (`EstadoMockDto`):**
```json
{
  "exitoso": true,
  "mensaje": "Operación completada satisfactoriamente",
  "datos": {
    "modoMockActivo": true,
    "mensaje": "Generador sintético determinístico activado (Monza GT3)."
  },
  "errores": []
}
```

---

## 🛡️ 4. Protocolo de Errores HTTP (RFC 7807 `ProblemDetails`)

Ante excepciones o parámetros inválidos, la API REST devuelve la cabecera `Content-Type: application/problem+json`:

```json
{
  "status": 404,
  "title": "Recurso no encontrado",
  "detail": "El recurso solicitado no existe en el registro local de telemetría.",
  "instance": "/api/vueltas/00000000-0000-0000-0000-000000000000/telemetria",
  "traceId": "0HND837J41A9B:00000001"
}
```

* **HTTP 400 (Bad Request):** Parámetros de ruta o query mal formados.
* **HTTP 404 (Not Found):** Sesión, vuelta o telemetría inexistente.
* **HTTP 500 (Internal Server Error):** Fallo no controlado; mensaje sanitizado en español sin filtración de cadenas de conexión ni *stack traces*.
