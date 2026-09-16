using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.WakeOnLan.Configuration
{
    /// <summary>
    /// Settings persisted by the Jellyfin host and edited on the plugin settings page.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets the MAC address of the storage server, in any common notation.
        /// </summary>
        public string MacAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the IP address or host name used to check whether the server is already awake.
        /// Leave empty to always send a packet.
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the TCP port probed on the storage server, for example 445 for SMB or 2049 for NFS.
        /// </summary>
        public int Port { get; set; } = 445;

        /// <summary>
        /// Gets or sets the UDP port the magic packet is broadcast to.
        /// </summary>
        public int WOLPort { get; set; } = 9;

        /// <summary>
        /// Gets or sets the minimum number of seconds between two probes or packets.
        /// </summary>
        public int WOLCoolDown { get; set; } = 600;
    }
}
