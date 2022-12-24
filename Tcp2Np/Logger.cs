using System.Net;

using Microsoft.Extensions.Logging;

namespace Tcp2Np;

internal static partial class Logger
{
    public static IDisposable PipeToSocketScope(
        this ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        return logger.BeginScope("P2S")!;
    }

    public static IDisposable SocketToPipeScope(
        this ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        return logger.BeginScope("S2P")!;
    }

    [LoggerMessage(1, LogLevel.Trace, "Waiting data.")]
    public static partial void WaitingData(
        this ILogger logger);

    [LoggerMessage(2, LogLevel.Trace, "Received {bytesReceived} bytes.")]
    public static partial void ReceivedData(
        this ILogger logger,
        int bytesReceived);

    [LoggerMessage(3, LogLevel.Trace, "Sent {bytesSent} bytes.")]
    public static partial void SentData(
        this ILogger logger,
        int bytesSent);

    [LoggerMessage(4, LogLevel.Information, "Connection closed.")]
    public static partial void ConnectionClosed(
        this ILogger logger);

    [LoggerMessage(5, LogLevel.Information, "Waiting socket connection on {localEndpoint}.")]
    public static partial void SocketConnectionWaiting(
        this ILogger logger,
        EndPoint localEndpoint);

    [LoggerMessage(6, LogLevel.Information, "Connection accepted from {remoteEndpoint}.")]
    public static partial void SocketConnectionAccepted(
        this ILogger logger,
        EndPoint remoteEndpoint);
}
