namespace Jellyfin.Plugin.WakeOnLan.Api
{
    /// <summary>
    /// Request body for sending a test magic packet.
    /// </summary>
    public class WolTestRequest
    {
        /// <summary>
        /// Gets or sets the MAC address of the machine to wake, in any common notation.
        /// </summary>
        public string MacAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UDP port the magic packet is broadcast to.
        /// </summary>
        public int WOLPort { get; set; } = 9;
    }
}
