using System.Net;
using System.Net.Sockets;
using Jellyfin.Plugin.WakeOnLan.Util;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.WakeOnLan.Tests
{
    public class RemoteServerUtilTests
    {
        [Fact]
        public async Task IsServerOnlineAsync_ReturnsTrueWhenPortAccepts()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var online = await RemoteServerUtil.IsServerOnlineAsync("127.0.0.1", port, NullLogger.Instance);

            Assert.True(online);
        }

        [Fact]
        public async Task IsServerOnlineAsync_ReturnsFalseWhenPortRefuses()
        {
            // Bind and immediately release a port so we know nothing listens on it.
            int port;
            using (var probe = new TcpListener(IPAddress.Loopback, 0))
            {
                probe.Start();
                port = ((IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();
            }

            var online = await RemoteServerUtil.IsServerOnlineAsync("127.0.0.1", port, NullLogger.Instance);

            Assert.False(online);
        }

        [Fact]
        public async Task IsServerOnlineAsync_ReturnsFalseOnUnresolvableHost()
        {
            var online = await RemoteServerUtil.IsServerOnlineAsync("host.invalid", 445, NullLogger.Instance);

            Assert.False(online);
        }

        [Fact]
        public async Task IsServerOnlineAsync_PropagatesCallerCancellation()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => RemoteServerUtil.IsServerOnlineAsync("127.0.0.1", 1, NullLogger.Instance, cancellationToken: cts.Token));
        }
    }
}
