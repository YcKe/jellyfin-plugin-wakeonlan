using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.WakeOnLan.Util
{
    /// <summary>
    /// Reachability checks for the storage server.
    /// </summary>
    public static class RemoteServerUtil
    {
        /// <summary>
        /// Checks whether a TCP connection to the given host and port can be established within the timeout.
        /// </summary>
        /// <param name="host">IP address or host name to connect to.</param>
        /// <param name="port">TCP port to connect to.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <param name="timeoutMs">How long to wait for the connection, in milliseconds.</param>
        /// <param name="cancellationToken">Token that aborts the probe early.</param>
        /// <returns><c>true</c> when the connection succeeded before the timeout.</returns>
        public static async Task<bool> IsServerOnlineAsync(string host, int port, ILogger logger, int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);

                using var client = new TcpClient();
                await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);

                return client.Connected;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug("WOL Plugin: Connection to {Host}:{Port} timed out after {Timeout} ms", host, port, timeoutMs);
                return false;
            }
            catch (SocketException ex)
            {
                logger.LogDebug(ex, "WOL Plugin: Connection to {Host}:{Port} refused", host, port);
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "WOL Plugin: Unexpected error while probing {Host}:{Port}", host, port);
                return false;
            }
        }
    }
}
