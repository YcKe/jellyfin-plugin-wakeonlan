using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.WakeOnLan
{
    /// <summary>
    /// Builds and broadcasts Wake-on-LAN magic packets.
    /// </summary>
    public static class WolService
    {
        private const int MacLength = 6;
        private const int MacRepetitions = 16;

        /// <summary>
        /// Parses a MAC address in any common notation (AA:BB:CC:DD:EE:FF, AA-BB-CC-DD-EE-FF,
        /// AABB.CCDD.EEFF or AABBCCDDEEFF) into its six bytes.
        /// </summary>
        /// <param name="macAddress">The MAC address text to parse.</param>
        /// <param name="bytes">The six address bytes when parsing succeeds, otherwise empty.</param>
        /// <returns><c>true</c> when the input is a valid six-octet MAC address.</returns>
        public static bool TryParseMacAddress(string? macAddress, out byte[] bytes)
        {
            bytes = [];

            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return false;
            }

            // Strip every accepted separator so all notations collapse to the bare 12-hex-digit form.
            var normalized = new string(macAddress.Where(c => c != ':' && c != '-' && c != '.' && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

            if (normalized.Length != MacLength * 2 || !PhysicalAddress.TryParse(normalized, out var parsed))
            {
                return false;
            }

            var parsedBytes = parsed.GetAddressBytes();
            if (parsedBytes.Length != MacLength)
            {
                return false;
            }

            bytes = parsedBytes;
            return true;
        }

        /// <summary>
        /// Builds a standard 102-byte magic packet: 6 x 0xFF followed by the MAC address repeated 16 times.
        /// </summary>
        /// <param name="mac">The six MAC address bytes.</param>
        /// <returns>The magic packet payload.</returns>
        public static byte[] BuildMagicPacket(byte[] mac)
        {
            var packet = new byte[MacLength + (MacLength * MacRepetitions)];
            packet.AsSpan(0, MacLength).Fill(0xFF);

            for (var i = 0; i < MacRepetitions; i++)
            {
                mac.CopyTo(packet, MacLength + (i * MacLength));
            }

            return packet;
        }

        /// <summary>
        /// Broadcasts a magic packet for the given MAC address to 255.255.255.255 on the given UDP port.
        /// </summary>
        /// <param name="macAddress">Target MAC address in any common notation.</param>
        /// <param name="port">UDP port to broadcast to, normally 9.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <returns><c>true</c> when the full packet was handed to the network stack.</returns>
        public static bool SendWolPacket(string macAddress, int port, ILogger logger)
        {
            if (!TryParseMacAddress(macAddress, out var mac))
            {
                logger.LogWarning("WOL Plugin: '{Mac}' is not a valid MAC address, no packet sent", macAddress);
                return false;
            }

            if (port is < 1 or > ushort.MaxValue)
            {
                logger.LogWarning("WOL Plugin: {Port} is not a valid UDP port, no packet sent", port);
                return false;
            }

            try
            {
                logger.LogInformation("WOL Plugin: Sending magic packet for {Mac} to {Broadcast}:{Port}", macAddress, IPAddress.Broadcast, port);

                var packet = BuildMagicPacket(mac);

                using var client = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
                var sent = client.Send(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, port));

                if (sent == packet.Length)
                {
                    logger.LogInformation("WOL Plugin: Magic packet sent");
                    return true;
                }

                logger.LogWarning("WOL Plugin: Only {Sent} of {Total} bytes were sent", sent, packet.Length);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WOL Plugin: Failed to send magic packet for {Mac}", macAddress);
                return false;
            }
        }
    }
}
