using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telemetria.Api.Configuracion;
using Telemetria.Api.Middlewares;
using Telemetria.Api.WebSockets;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(opcionesServidor =>
{
    int puertoEscucha = builder.Configuration.GetValue<int>("Servidor:Puerto", 8080);
    opcionesServidor.ListenAnyIP(puertoEscucha);
});

builder.Services.AddControllers()
    .AddJsonOptions(opciones =>
    {
        opciones.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opciones.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.ConfigurarCorsTelemetria(builder.Configuration);
builder.Services.AgregarSeguridadRbacTelemetria();
builder.Services.AgregarPersistenciaTelemetria(builder.Configuration);
builder.Services.AgregarSincronizacionPostgreSql();

bool modoDemoSolicitado = args.Any(arg => string.Equals(arg, "--demo", StringComparison.OrdinalIgnoreCase));
builder.Services.AgregarMotorAdquisicionYRelay(builder.Configuration, modoDemoSolicitado);

var app = builder.Build();

app.UseMiddleware<ManejadorExcepcionesGlobal>();
app.UseCors(ConfiguracionCorsExtensiones.POLITICA_CORS_SIMULADOR);
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(2) });

var origenesPermitidosWs = app.Configuration
    .GetSection("Cors:OrigenesPermitidos")
    .Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://127.0.0.1:3000", "http://localhost:8080" };

app.Map("/ws/telemetry", async contexto =>
{
    if (!contexto.WebSockets.IsWebSocketRequest)
    {
        contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    string cabeceraOrigen = contexto.Request.Headers["Origin"].ToString();
    if (!string.IsNullOrWhiteSpace(cabeceraOrigen) &&
        !ValidadorOrigenRedLocal.EsOrigenPermitido(cabeceraOrigen, origenesPermitidosWs))
    {
        contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    using var socket = await contexto.WebSockets.AcceptWebSocketAsync();
    var difusor = contexto.RequestServices.GetRequiredService<ManejadorWebSocketTelemetria>();
    await difusor.ConectarClienteAsync(socket, contexto.RequestAborted);
});

app.MapControllers();

await app.MigrarBaseDatosSqliteAsync();
await app.RunAsync();
await app.EjecutarCheckpointFinalSqliteAsync();

public partial class Program { }
