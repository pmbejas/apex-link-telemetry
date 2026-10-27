-- ============================================================================
-- Esquema DDL de Base de Datos Central PostgreSQL (Nivel 2)
-- Proyecto: Plataforma de Telemetría SimRacing en Tiempo Real
-- Arquitectura: 3FN (Tercera Forma Normal) con Auditoría Temporal Universal
-- ============================================================================

-- Habilitar extensión para generación de identificadores UUID si no existe
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================================
-- 1. TABLAS DE SEGURIDAD Y CONTROL DE ACCESO GRANULAR (RBAC)
-- ============================================================================

CREATE TABLE IF NOT EXISTS roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre VARCHAR(50) NOT NULL CONSTRAINT uq_roles_nombre UNIQUE,
    descripcion VARCHAR(200) NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NULL
);

CREATE TABLE IF NOT EXISTS permisos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    codigo VARCHAR(100) NOT NULL CONSTRAINT uq_permisos_codigo UNIQUE,
    modulo VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NULL
);

CREATE TABLE IF NOT EXISTS roles_permisos (
    rol_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permiso_id UUID NOT NULL REFERENCES permisos(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NULL,
    PRIMARY KEY (rol_id, permiso_id)
);

CREATE TABLE IF NOT EXISTS usuarios (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre_completo VARCHAR(150) NOT NULL,
    alias_piloto VARCHAR(100) NOT NULL,
    email VARCHAR(200) NOT NULL CONSTRAINT uq_usuarios_email UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NULL
);

CREATE TABLE IF NOT EXISTS usuarios_roles (
    usuario_id UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    rol_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NULL,
    PRIMARY KEY (usuario_id, rol_id)
);

-- ============================================================================
-- 2. TABLAS DE TELEMETRÍA Y DOMINIO DEPORTIVO
-- ============================================================================

CREATE TABLE IF NOT EXISTS sesiones (
    id UUID PRIMARY KEY,
    usuario_id UUID NULL REFERENCES usuarios(id) ON DELETE SET NULL,
    circuito VARCHAR(100) NOT NULL,
    configuracion_circuito VARCHAR(100) NOT NULL,
    modelo_vehiculo VARCHAR(100) NOT NULL,
    tipo_sesion INT NOT NULL,
    es_simulacion_mock BOOLEAN NOT NULL DEFAULT FALSE,
    fecha_inicio TIMESTAMP WITH TIME ZONE NOT NULL,
    fecha_fin TIMESTAMP WITH TIME ZONE NULL,
    total_vueltas INT NOT NULL DEFAULT 0,
    mejor_tiempo_ms INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NULL
);

CREATE TABLE IF NOT EXISTS vueltas (
    id UUID PRIMARY KEY,
    sesion_id UUID NOT NULL REFERENCES sesiones(id) ON DELETE CASCADE,
    numero_vuelta INT NOT NULL,
    tiempo_vuelta_ms INT NOT NULL,
    tiempo_vuelta_formateado VARCHAR(20) NOT NULL,
    sector_1_ms INT NOT NULL DEFAULT 0,
    sector_2_ms INT NOT NULL DEFAULT 0,
    sector_3_ms INT NOT NULL DEFAULT 0,
    es_valida BOOLEAN NOT NULL DEFAULT TRUE,
    es_mejor_vuelta BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT uq_vueltas_sesion_numero UNIQUE (sesion_id, numero_vuelta)
);

CREATE TABLE IF NOT EXISTS telemetria_vueltas (
    id UUID PRIMARY KEY,
    vuelta_id UUID NOT NULL REFERENCES vueltas(id) ON DELETE CASCADE,
    marca_temporal_unix_ms BIGINT NOT NULL,
    distancia_pista_metros REAL NOT NULL,
    velocidad_kmh REAL NOT NULL,
    acelerador REAL NOT NULL,
    freno REAL NOT NULL,
    marcha INT NOT NULL,
    rpms INT NOT NULL,
    fuerza_g_lat REAL NOT NULL,
    fuerza_g_lon REAL NOT NULL,
    pos_x REAL NOT NULL,
    pos_y REAL NOT NULL,
    pos_z REAL NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NULL
);

-- ============================================================================
-- 3. ÍNDICES DE ALTO RENDIMIENTO
-- ============================================================================

-- B-Tree Compuesto: permite Index Scan sin Sort para la consulta analítica por distancia
CREATE INDEX IF NOT EXISTS idx_telemetria_vueltas_vuelta_distancia 
    ON telemetria_vueltas USING BTREE (vuelta_id, distancia_pista_metros);

-- BRIN (Block Range Index): barrido cronológico ultraligero sobre series temporales densas
CREATE INDEX IF NOT EXISTS idx_telemetria_vueltas_tiempo_brin 
    ON telemetria_vueltas USING BRIN (marca_temporal_unix_ms);

-- Índices B-Tree de soporte relacional para claves foráneas y búsquedas frecuentes
CREATE INDEX IF NOT EXISTS idx_vueltas_sesion_id ON vueltas (sesion_id);
CREATE INDEX IF NOT EXISTS idx_sesiones_usuario_id ON sesiones (usuario_id);
CREATE INDEX IF NOT EXISTS idx_sesiones_fecha_inicio ON sesiones (fecha_inicio);
CREATE INDEX IF NOT EXISTS idx_roles_permisos_permiso_id ON roles_permisos (permiso_id);
CREATE INDEX IF NOT EXISTS idx_usuarios_roles_rol_id ON usuarios_roles (rol_id);

-- ============================================================================
-- 4. SEMILLA INICIAL DE SEGURIDAD (ROLES Y PERMISOS BÁSICOS)
-- ============================================================================

INSERT INTO roles (nombre, descripcion) VALUES
    ('Administrador', 'Control total de configuración, usuarios y telemetría central'),
    ('Ingeniero de Pista', 'Visualización, comparación y análisis de telemetría de boxes'),
    ('Piloto', 'Visualización de telemetría personal y comparación de vueltas')
ON CONFLICT (nombre) DO NOTHING;

INSERT INTO permisos (codigo, modulo, descripcion) VALUES
    ('sesion:listar', 'Sesiones', 'Permite listar el historial de sesiones'),
    ('vuelta:listar', 'Vueltas', 'Permite consultar vueltas de una sesión'),
    ('telemetria:leer', 'Telemetria', 'Permite descargar la serie completa de telemetría'),
    ('telemetria:comparar', 'Telemetria', 'Permite ejecutar comparativas métricas de vueltas'),
    ('estado:leer', 'Monitoreo', 'Permite consultar el estado del simulador y servicios'),
    ('simulador:alternar_mock', 'Control', 'Permite conmutar en caliente entre simulador real y generador mock')
ON CONFLICT (codigo) DO NOTHING;
