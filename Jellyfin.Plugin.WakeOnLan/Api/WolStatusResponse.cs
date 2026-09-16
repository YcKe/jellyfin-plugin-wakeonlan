namespace Jellyfin.Plugin.WakeOnLan.Api
{
    /// <summary>
    /// Result of probing the storage server.
    /// </summary>
    public class WolStatusResponse
    {
        /// <summary>
        /// Gets or sets the host that was probed.
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the TCP port that was probed.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a TCP connection to the host and port succeeded.
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// Gets or sets the probe duration in milliseconds.
        /// </summary>
        public long DurationMs { get; set; }
    }
}
