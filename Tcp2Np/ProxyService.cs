using System.IO.Pipes;
using System.Net.Sockets;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tcp2Np;

internal class ProxyService :
    BackgroundService
{
    private readonly ProxyServiceOptions _options;
    private readonly ILogger<ProxyService> _logger;

    public ProxyService(
        IOptions<ProxyServiceOptions> options,
        ILogger<ProxyService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        this._options = options.Value;
        this._logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var logger = this._logger;

        using var server = this.InitializeServerSocket();

        while (!stoppingToken.IsCancellationRequested)
        {
            using var cts = new CancellationTokenSource();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, cts.Token);

            logger.SocketConnectionWaiting(server.LocalEndPoint!);

            using var socket = await server
                .AcceptAsync(linkedCts.Token)
                .ConfigureAwait(false);

            logger.SocketConnectionAccepted(socket.RemoteEndPoint!);

            await using var pipe = await this
                .InitializePipe(linkedCts.Token)
                .ConfigureAwait(false);

            var p2sTask = this.PipeToSocket(
                socket,
                pipe,
                linkedCts.Token);

            var s2pTask = this.SocketToPipe(
                socket,
                pipe,
                linkedCts.Token);

            await Task
                .WhenAny(p2sTask, s2pTask)
                .ConfigureAwait(false);

            cts.Cancel();

            await Task
                .WhenAll(p2sTask, s2pTask)
                .ConfigureAwait(false);
        }
    }

    private async Task<NamedPipeClientStream> InitializePipe(
        CancellationToken cancellationToken)
    {
        var pipe = new NamedPipeClientStream(
            ".",
            this._options.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        await pipe
            .ConnectAsync(TimeSpan.FromSeconds(30), cancellationToken)
            .ConfigureAwait(false);

        return pipe;
    }

    private Socket InitializeServerSocket()
    {
        var endpoint = this._options.Endpoint;

        var socket = new Socket(
            endpoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);

        socket.Bind(endpoint);
        socket.Listen();

        return socket;
    }

    private Task PipeToSocket(
        Socket socket,
        NamedPipeClientStream pipe,
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                var logger = this._logger;
                using var scope = logger.PipeToSocketScope();

                var buffer = new byte[1024].AsMemory();

                while (!cancellationToken.IsCancellationRequested)
                {
                    logger.WaitingData();

                    var bytesReceived = await pipe
                        .ReadAsync(buffer, cancellationToken)
                        .ConfigureAwait(false);

                    if (bytesReceived == 0)
                    {
                        logger.ConnectionClosed();
                        break;
                    }

                    logger.ReceivedData(bytesReceived);

                    var bytesSent = await socket
                        .SendAsync(buffer.Slice(0, bytesReceived), cancellationToken)
                        .ConfigureAwait(false);

                    logger.SentData(bytesSent);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, cancellationToken);
    }

    private Task SocketToPipe(
        Socket socket,
        NamedPipeClientStream pipe,
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                var logger = this._logger;
                using var scope = logger.SocketToPipeScope();

                var buffer = new byte[1024].AsMemory();

                while (!cancellationToken.IsCancellationRequested)
                {
                    logger.WaitingData();

                    var bytesReceived = await socket
                        .ReceiveAsync(buffer, cancellationToken)
                        .ConfigureAwait(false);

                    if (bytesReceived == 0)
                    {
                        logger.ConnectionClosed();
                        break;
                    }

                    logger.ReceivedData(bytesReceived);

                    await pipe
                        .WriteAsync(buffer.Slice(0, bytesReceived), cancellationToken)
                        .ConfigureAwait(false);

                    await pipe
                        .FlushAsync(cancellationToken)
                        .ConfigureAwait(false);

                    logger.ReceivedData(bytesReceived);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, cancellationToken);
    }
}
