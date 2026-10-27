# Reglas del Proyecto - Consultorio Odontológico

## Idioma y nomenclatura
- Todo el código, comentarios, nombres de variables, clases y métodos en español (a excepción de estándares universales de la industria como `created_at` y `updated_at` para auditoría temporal)
- Nombres descriptivos que reflejen claramente la función de cada elemento
- Evitar abreviaciones: usar "cantidad" en vez de "cant", "usuario" en vez de "usr"
- Clases en PascalCase, variables locales en camelCase, constantes en UPPER_SNAKE_CASE

## Auditoría obligatoria en Base de Datos
- TODAS las tablas de la base de datos sin excepción deben poseer los campos de auditoría temporal `created_at` (TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()) y `updated_at` (TIMESTAMP WITH TIME ZONE NULL). En C# se mapean como `CreatedAt` y `UpdatedAt`.

## Documentación Técnica Exhaustiva por Fases
- Al finalizar cada fase y antes de continuar con la siguiente, se debe registrar en `backend/DOCUMENTACION_TECNICA.md` una explicación completa, detallada y fundamentada de todo lo creado: paquetes NuGet, carpetas, archivos, interfaces, middlewares y entidades, sin omitir ningún componente.
- Revisar 3 veces el contenido agregado al documento para verificar exhaustivamente que no falte ninguna explicación conceptual ni funcional.

## Estilo de código
- Código simple y legible, que cualquier programador junior pueda entender
- Un método debe hacer una sola cosa (responsabilidad única, máx. 30-40 líneas)
- Usar early return para reducir anidación

## Autorización y Permisos (RBAC Parametrizable)
- Sistema basado en Roles y Permisos granulares (RBAC + Permisos)
- Nomenclatura obligatoria de permisos: formato `recurso:accion` en minúsculas (ej. `paciente:crear`, `odontograma:editar`, `turno:confirmar_pago`, `gasto:registrar`)
- Protección en controladores: uso obligatorio del atributo `[HasPermission("recurso:accion")]`
- Los permisos asignados a cada rol deben ser 100% parametrizables y editables desde la interfaz de usuario (tablas `roles`, `permisos`, `roles_permisos`)
- Proteger endpoints y vistas evaluando permisos específicos, evitando acoplarse a nombres fijos de roles hardcodeados

## Metodología y Planes de Implementación
- Para planificar el trabajo o acordar la construcción del sistema, generar siempre un artifact markdown (`plan_implementacion.md`) con solicitud de feedback (`RequestFeedback: true`) para habilitar el botón "Proceed" y permitir comentarios del usuario antes de ejecutar los cambios

## Ejecución de pruebas y testing
- No ejecutar toda la suite completa de pruebas en cada cambio; hacerlo únicamente cuando sea estrictamente necesario
- Ejecutar pruebas focalizadas exclusivamente sobre los archivos, clases o métodos que se hayan modificado

## Terminología y restricciones de negocio
- Prohibido terminantemente usar la palabra "fraude" o "antifraude" en el código, comentarios, mensajes o interfaz; usar términos como "validación", "control" o "auditoría"

## Seguridad
- Nunca hardcodear credenciales, claves ni secrets en el código
- Todas las credenciales van en variables de entorno o archivos de configuración
- Validar y sanitizar siempre los datos de entrada del usuario

## Verificaciones visuales y optimización de recursos
- No realizar tantas pruebas visuales en el navegador; ejecutar únicamente las estrictamente esenciales cuando sea indispensable o solicitado por el usuario, evitando demoras y consumo innecesario de tokens.

## Diseño de Base de Datos y Normalización (3FN)
- Al realizar modificaciones o agregar tablas y columnas en la base de datos, el modelo debe respetar rigurosamente la Tercera Forma Normal (3FN), garantizando que todo atributo dependa de la clave primaria sin dependencias transitivas, a menos que exista una justificación técnica o de rendimiento estrictamente indispensable.

