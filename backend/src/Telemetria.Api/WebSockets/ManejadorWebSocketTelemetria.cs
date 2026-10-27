namespace Telemetria.Api.WebSockets;

using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telemetria.Aplicacion.Common;
using Telemetria.Aplicacion.Contratos;
using Telemetria.Aplicacion.Dtos.Responses;

public sealed class ManejadorWebSocketTelemetria : IDifusorTelemetria, IDisposable
{
    private const int CAPACIDAD_CANAL_STREAM = 2;
    private const int CAPACIDAD_CANAL_EVENTOS = 64;
    private const int TAMANIO_BUFFER_RECEPCION_BYTES = 512;

    private sealed class ClienteConectado
    {
        public required WebSocket Socket { get; init; }
        public required Channel<byte[]> CanalTelemetriaStream { get; init; }
        public required Channel<byte[]> CanalEventosCriticos { get; init; }
        public required SemaphoreSlim SenalDatosDisponibles { get; init; }
        public required CancellationTokenSource OrigenCancelacion { get; init; }
    }

    private readonly ConcurrentDictionary<Guid, ClienteConectado> _clientes = new();
    private readonly ILogger<ManejadorWebSocketTelemetria> _logger;

    public ManejadorWebSocketTelemetria(ILogger<ManejadorWebSocketTelemetria> logger)
    {
        _logger = logger;
    }

    public bool TieneClientesConectados => !_clientes.IsEmpty;

    public async Task ConectarClienteAsync(WebSocket socket, CancellationToken tokenDetencion)
    {
        Guid clienteId = Guid.NewGuid();
        ClienteConectado cliente = CrearClienteConectado(socket, tokenDetencion);

        _clientes.TryAdd(clienteId, cliente);
        _logger.LogInformation("Cliente WebSocket conectado: {ClienteId}. Activos: {Total}", clienteId, _clientes.Count);

        Task tareaDespacho = Task.Run(
            () => EjecutarBucleEscrituraAsync(clienteId, cliente),
            CancellationToken.None);

        try
        {
            await RecibirHastaCierreDelClienteAsync(socket, cliente.OrigenCancelacion);
        }
        finally
        {
            await CerrarYRemoverClienteAsync(clienteId, tareaDespacho);
        }
    }

    public ValueTask DifundirTelemetriaAsync(PaqueteTelemetriaDto paquete, CancellationToken token = default)
    {
        if (_clientes.IsEmpty || token.IsCancellationRequested) return ValueTask.CompletedTask;

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(
            paquete,
            ContextoSerializacionTelemetria.Default.PaqueteTelemetriaDto);

        foreach (var par in _clientes)
        {
            ClienteConectado cliente = par.Value;
            if (cliente.Socket.State == WebSocketState.Open && cliente.CanalTelemetriaStream.Writer.TryWrite(bytes))
            {
                NotificarDatosDisponibles(cliente);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask EmitirEventoVueltaAsync(EventoVueltaCompletadaDto evento, CancellationToken token = default)
    {
        if (_clientes.IsEmpty || token.IsCancellationRequested) return ValueTask.CompletedTask;

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(
            evento,
            ContextoSerializacionTelemetria.Default.EventoVueltaCompletadaDto);

        foreach (var par in _clientes)
        {
            ClienteConectado cliente = par.Value;
            if (cliente.Socket.State != WebSocketState.Open) continue;

            if (cliente.CanalEventosCriticos.Writer.TryWrite(bytes))
            {
                NotificarDatosDisponibles(cliente);
            }
            else
            {
                _logger.LogWarning("Cola de eventos llena para cliente {ClienteId}. Cancelando consumidor inactivo.", par.Key);
                cliente.OrigenCancelacion.Cancel();
            }
        }

        return ValueTask.CompletedTask;
    }

    private static ClienteConectado CrearClienteConectado(WebSocket socket, CancellationToken tokenDetencion)
    {
        var opcionesStream = new BoundedChannelOptions(capacity: CAPACIDAD_CANAL_STREAM)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = false,
            SingleReader = true
        };

        var opcionesEventos = new BoundedChannelOptions(capacity: CAPACIDAD_CANAL_EVENTOS)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        };

        return new ClienteConectado
        {
            Socket = socket,
            CanalTelemetriaStream = Channel.CreateBounded<byte[]>(opcionesStream),
            CanalEventosCriticos = Channel.CreateBounded<byte[]>(opcionesEventos),
            SenalDatosDisponibles = new SemaphoreSlim(0, int.MaxValue),
            OrigenCancelacion = CancellationTokenSource.CreateLinkedTokenSource(tokenDetencion)
        };
    }

    private async Task EjecutarBucleEscrituraAsync(Guid clienteId, ClienteConectado cliente)
    {
        CancellationToken token = cliente.OrigenCancelacion.Token;
        try
        {
            while (!token.IsCancellationRequested && cliente.Socket.State == WebSocketState.Open)
            {
                await cliente.SenalDatosDisponibles.WaitAsync(token);
                await DespacharColaEventosAsync(cliente.Socket, cliente.CanalEventosCriticos.Reader, token);
                await DespacharUltimoCuadroStreamAsync(cliente.Socket, cliente.CanalTelemetriaStream.Reader, token);
            }
        }
        catch (Exception excepcion) when (excepcion is OperationCanceledException or WebSocketException or ObjectDisposedException)
        {
            _logger.LogDebug("Bucle de escritura finalizado para el cliente {ClienteId}.", clienteId);
        }
        finally
        {
            cliente.OrigenCancelacion.Cancel();
        }
    }

    private static async Task DespacharColaEventosAsync(
        WebSocket socket,
        ChannelReader<byte[]> lectorEventos,
        CancellationToken token)
    {
        while (lectorEventos.TryRead(out byte[]? paqueteEvento))
        {
            await socket.SendAsync(new ArraySegment<byte>(paqueteEvento), WebSocketMessageType.Text, true, token);
        }
    }

    private static async Task DespacharUltimoCuadroStreamAsync(
        WebSocket socket,
        ChannelReader<byte[]> lectorStream,
        CancellationToken token)
    {
        byte[]? ultimoPaqueteStream = null;
        while (lectorStream.TryRead(out byte[]? candidato))
        {
            ultimoPaqueteStream = candidato;
        }

        if (ultimoPaqueteStream is not null)
        {
            await socket.SendAsync(new ArraySegment<byte>(ultimoPaqueteStream), WebSocketMessageType.Text, true, token);
        }
    }

    private static async Task RecibirHastaCierreDelClienteAsync(
        WebSocket socket,
        CancellationTokenSource origenCancelacion)
    {
        byte[] bufferRecepcion = new byte[TAMANIO_BUFFER_RECEPCION_BYTES];
        try
        {
            while (socket.State == WebSocketState.Open && !origenCancelacion.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult resultado = await socket.ReceiveAsync(
                    new ArraySegment<byte>(bufferRecepcion),
                    origenCancelacion.Token);

                if (resultado.MessageType == WebSocketMessageType.Close)
                {
                    origenCancelacion.Cancel();
                    break;
                }
            }
        }
        catch (Exception excepcion) when (excepcion is OperationCanceledException or WebSocketException or ObjectDisposedException)
        {
            origenCancelacion.Cancel();
        }
    }

    private static void NotificarDatosDisponibles(ClienteConectado cliente)
    {
        try
        {
            if (cliente.SenalDatosDisponibles.CurrentCount == 0)
            {
                cliente.SenalDatosDisponibles.Release();
            }
        }
        catch (ObjectDisposedException)
        {
            // El cliente ya fue desconectado y liberado
        }
    }

    private async Task CerrarYRemoverClienteAsync(Guid clienteId, Task tareaDespacho)
    {
        if (!_clientes.TryRemove(clienteId, out ClienteConectado? cliente)) return;

        try
        {
            cliente.OrigenCancelacion.Cancel();
            await tareaDespacho;

            if (cliente.Socket.State == WebSocketState.CloseReceived)
            {
                using var timeoutCierre = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                await cliente.Socket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Cierre ordenado confirmado por el servidor",
                    timeoutCierre.Token);
            }
        }
        catch (Exception excepcion)
        {
            _logger.LogDebug(excepcion, "Cierre controlado de socket para el cliente {ClienteId}.", clienteId);
        }
        finally
        {
            LiberarRecursosCliente(cliente);
            _logger.LogInformation("Cliente WebSocket desconectado: {ClienteId}. Restantes: {Total}", clienteId, _clientes.Count);
        }
    }

    private static void LiberarRecursosCliente(ClienteConectado cliente)
    {
        cliente.OrigenCancelacion.Dispose();
        cliente.SenalDatosDisponibles.Dispose();
        cliente.Socket.Dispose();
    }

    public void Dispose()
    {
        foreach (var par in _clientes)
        {
            if (_clientes.TryRemove(par.Key, out ClienteConectado? cliente))
            {
                cliente.OrigenCancelacion.Cancel();
                LiberarRecursosCliente(cliente);
            }
        }
    }
}
