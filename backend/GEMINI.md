# Reglas Backend - C# ASP.NET Core

## Stack base
- C# con ASP.NET Core
- Entity Framework Core + Dapper
- PostgreSQL con Npgsql
- FluentValidation para validaciones
- BCrypt para hash de passwords
- OAuth2 para login (proveedores externos) + JWT y Refresh Tokens para sesión propia de la API

## Skills disponibles para este proyecto
Antes de generar código extenso a mano, considerar si aplica uno de estos skills (tienen el detalle de implementación completo, con código de referencia):
- `dotnet-module-scaffold` → crear un CRUD/módulo nuevo completo
- `dotnet-auth-oauth2` → implementar o modificar login, JWT, Refresh Tokens
- `dotnet-testing` → escribir tests de Services o Controllers
- `dotnet-design-patterns` → decidir y aplicar un patrón creacional (Factory, Builder, Singleton, Prototype, Abstract Factory)

## Autorización y Seguridad (RBAC + Permisos Parametrizables)
- Sistema basado en Roles y Permisos granulares (RBAC).
- Nomenclatura obligatoria de permisos: formato `recurso:accion` en minúsculas (ej. `paciente:crear`, `paciente:ver`, `odontograma:editar`, `turno:confirmar_pago`, `gasto:registrar`).
- Protección de endpoints en controladores: uso obligatorio del atributo personalizado `[HasPermission("recurso:accion")]`, resuelto de forma dinámica en memoria mediante `IAuthorizationPolicyProvider`.
- Los permisos son 100% parametrizables: cada rol tiene asignado un conjunto de permisos editable desde la interfaz (tablas `roles`, `permisos` y `roles_permisos`).
- Prohibido acoplar controladores o endpoints a nombres de roles fijos (evitar `[Authorize(Roles = "...")]`). Siempre validar contra permisos atómicos.

## Documentación Técnica Exhaustiva por Fases
- Al finalizar cada fase y antes de comenzar la siguiente, registrar en `backend/DOCUMENTACION_TECNICA.md` la explicación detallada de cada elemento creado: paquetes NuGet, carpetas, archivos, interfaces, clases y middlewares.
- Revisar 3 veces cada entrega para garantizar que no se omita ningún componente y que todos los conceptos estén rigurosamente fundamentados.

## Código limpio y buenas prácticas

**Nomenclatura del dominio en español, siempre representativa de su función:**
- Nombres descriptivos en español, excepto estándares universales de la industria para campos de auditoría temporal (`created_at`, `updated_at` en base de datos / `CreatedAt`, `UpdatedAt` en C#)
- TODAS las tablas de la base de datos sin excepción deben tener las columnas `created_at` (TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()) y `updated_at` (TIMESTAMP WITH TIME ZONE NULL), y todas las entidades C# asociadas deben incluir `CreatedAt` y `UpdatedAt`.
- Nombres claros sin abreviaturas crípticas: `cantidadDisponible`, no `cantDisp`
- Nada de nombres genéricos sin contexto: evitar `data`, `temp`, `aux`, `lista1`, `obj`
- Booleanos con prefijo que indique la pregunta que responden: `esActivo`, `tieneStock`, `puedeEditar`, `estaVencido`
- Colecciones en plural y descriptivas: `usuariosActivos`, no `listaUsuarios1`
- Un nombre debe permitir entender qué hace la variable/método sin leer su implementación

**Métodos y funciones:**
- Una sola responsabilidad por método; si hace más de una cosa, se divide
- Métodos cortos (guía: si supera ~25-30 líneas, evaluar extraer sub-métodos)
- Evitar anidamiento profundo de `if`: usar guard clauses / early return
  ```csharp
  // Evitar:
  if (usuario != null) { if (usuario.EstaActivo) { /* lógica */ } }
  // Preferir:
  if (usuario is null) return;
  if (!usuario.EstaActivo) return;
  /* lógica */
  ```
- Parámetros de método: si superan 4-5, evaluar agruparlos en un objeto/Request

**Evitar duplicación (DRY):**
- Si el mismo bloque de lógica se repite en 2 o más lugares, extraer a un método reutilizable
- Constantes compartidas entre clases van en una clase estática de constantes, no repetidas

**Sin números o strings mágicos:**
- Valores fijos con significado propio van como `const` o `enum`, nunca sueltos en el código
  ```csharp
  // Evitar: if (estado == 3)
  // Preferir: if (estado == EstadoPedido.Enviado)
  ```

**Comentarios:**
- El código se explica solo mediante nombres bien elegidos; comentario solo cuando el *por qué* no es obvio (una decisión de negocio, un workaround, un caso límite)
- Nunca comentar *qué* hace una línea si el nombre ya lo dice

## Nomenclatura C#
- Clases en PascalCase: Usuario, ProductoDetalle
- Propiedades en PascalCase: NombreCompleto, FechaExpiracion
- Variables locales en camelCase: listaUsuarios, productoEncontrado
- Parámetros de métodos en camelCase: usuarioId, fechaInicio
- Interfaces con prefijo I: IUsuarioService, IProductoRepository
- DTOs de entrada con sufijo Request: CrearUsuarioRequest
- DTOs de salida con sufijo Response: UsuarioResponse
- Validadores con sufijo Validator: CrearUsuarioRequestValidator
- Strings requeridos inicializan con string.Empty
- Propiedades opcionales con nullable ?: string?, int?

## Estructura de cualquier proyecto backend
- Models/       → entidades que mapean las tablas de la base de datos
- DTOs/         → objetos de entrada (Request) y salida (Response)
- Data/         → AppDbContext y Repositories
- Services/     → lógica de negocio
- Interfaces/   → contratos de servicios y repositorios
- Controllers/  → endpoints REST
- Validators/   → clases FluentValidation
- Middleware/   → GlobalExceptionHandler y otros middlewares
- Common/       → tipos compartidos transversales (ApiResponse<T>, ApiResponseFactory, PaginacionResponse<T>)

Ver skill `dotnet-module-scaffold` para el andamiaje completo de un módulo nuevo respetando esta estructura.

## Controllers
- Solo manejan peticiones HTTP, nunca contienen lógica de negocio
- Solo llaman a Services, nunca acceden directamente a la base de datos
- Siempre retornar ActionResult o IActionResult
- Usar atributos HTTP: [HttpGet], [HttpPost], [HttpPut], [HttpDelete]
- Códigos HTTP correctos:
  - 200 Ok() para consultas exitosas
  - 201 Created() para creación exitosa
  - 400 BadRequest() para datos inválidos
  - 401 Unauthorized() para no autenticado
  - 403 Forbidden() para sin permisos
  - 404 NotFound() cuando no se encuentra el recurso
  - 500 manejado por el GlobalExceptionHandler
- Todos los endpoints que retornen listas deben soportar paginación

## Respuestas de la API
- Envolver todas las respuestas en ApiResponse<T>
- ApiResponse<T> contiene: bool Exitoso, string Mensaje, T? Datos, List<string> Errores
- Los errores devuelven ProblemDetails (RFC 7807): type, title, status, detail, instance

## Manejo de errores
- GlobalExceptionHandler como middleware en Middleware/
- Siempre usar try/catch en Repositories y Services
- En el catch registrar con ILogger y relanzar con mensaje descriptivo en español
- Nunca silenciar excepciones con catch vacío
- Nunca usar Console.WriteLine, siempre ILogger

## Validaciones con FluentValidation
- Cada DTO Request tiene su clase Validator en Validators/
- Nunca usar DataAnnotations ([Required], [MaxLength]) en los DTOs
- Registrar validators en Program.cs con AddFluentValidationAutoValidation()

## Métodos asincrónicos
- Siempre retornar Task o Task<T>
- Siempre usar async/await, nunca .Result o .Wait()
- Incluir CancellationToken como parámetro en repositories y services
- Validar parámetros al inicio del método antes de cualquier lógica

## Paginación
- Endpoints GET de listas aceptan: numeroPagina (default 1), tamanioPagina (default 10, max 100)
- Respuesta paginada incluye: datos, totalRegistros, totalPaginas, paginaActual

## EF Core vs Dapper
- Dapper   → consultas de cualquier tipo (simples, con JOINs, filtros múltiples o reportes)
- EF Core  → para todo lo que no sea consultas (CRUD sin read, migraciones, etc.)
- Si una operación combina ambos en la misma unidad de trabajo, deben compartir la misma conexión y transacción — ver detalle en skill `dotnet-module-scaffold`

## Mapeo Entity <-> DTO
- Usar Mapster para el mapeo entre entidades y DTOs (Request/Response)
- Definir los perfiles de mapeo en Data/Mappings/ o junto al DTO correspondiente, no repetir lógica de mapeo dentro de los Services
- Nunca exponer una entidad de Models/ directamente en un Controller; siempre pasar por su Response correspondiente
- Para mapeos con lógica condicional (campos calculados, formateo), usar un método de mapeo explícito en el Service, no forzarlo dentro del mapper automático

## Autenticación: OAuth2 + JWT + Refresh Tokens
- Login primario vía OAuth2 con proveedor externo; el backend emite su propio JWT + Refresh Token tras el callback, nunca reenvía el token del proveedor
- JWT de expiración corta (ej. 15 min), Refresh Token de expiración larga (ej. 7 días)
- Refresh Tokens se guardan hasheados, con rotación obligatoria y revocación en cadena ante reuso de un token ya revocado
- Implementación completa (endpoints, TokenService, configuración de Program.cs) en el skill `dotnet-auth-oauth2`

## Testing
- xUnit + Moq para tests unitarios de Services; WebApplicationFactory para tests de integración de Controllers
- Un archivo de test por clase, método nombrado MetodoTesteado_Escenario_ResultadoEsperado
- Plantillas y patrón de casos a cubrir en el skill `dotnet-testing`

## Migraciones EF Core
- Nombrar migraciones en PascalCase descriptivo: AgregarTablaRefreshTokens, no Migration1
- Nunca editar una migración ya aplicada en el entorno compartido/producción; crear una nueva
- Migraciones se aplican explícitamente (dotnet ef database update o en el pipeline de CI/CD), nunca automáticamente al levantar la app en producción

## Seguridad en Dapper
- Todas las queries Dapper van parametrizadas (@parametro), nunca concatenar valores de usuario directamente en el string SQL
- Para queries dinámicas (filtros opcionales), construir el WHERE con parámetros condicionales, nunca con interpolación de strings sobre el valor recibido

## Nullable reference types
- El proyecto tiene <Nullable>enable</Nullable> en el .csproj
- Toda propiedad o parámetro que puede no tener valor se marca explícitamente con ?
- No usar el operador ! (null-forgiving) salvo que sea imposible que el valor sea null en ese punto, y dejar un comentario breve explicando por qué

## Inyección de dependencias
- Siempre depender de interfaces, nunca instanciar con new dentro de otros servicios
- Registrar todo en Program.cs
- Scoped para servicios y repositorios, Singleton para configuraciones

## Configuración por entorno
- Configuración sensible (connection strings, secrets de OAuth2, claves JWT) nunca hardcodeada ni commiteada
- Usar appsettings.{Environment}.json para configuración no sensible por entorno
- Secrets reales (producción) vía variables de entorno o un secret manager, nunca en appsettings.json versionado
- En desarrollo local, usar dotnet user-secrets para no exponer secrets en el repo
