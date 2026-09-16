using System.Diagnostics;
using Jellyfin.Plugin.WakeOnLan.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.WakeOnLan.Api
{
    /// <summary>
    /// Administrative endpoints used by the plugin settings page.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "RequiresElevation")]
    [Route("Wol")]
    [Produces("application/json")]
    public class WolController : ControllerBase
    {
        private readonly ILogger<WolController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WolController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public WolController(ILogger<WolController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Sends a test Wake-on-LAN magic packet.
        /// </summary>
        /// <param name="request">The MAC address and UDP port to use.</param>
        /// <returns>204 when the packet was sent, 400 on invalid input, 500 when sending failed.</returns>
        [HttpPost("Test")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult Test([FromBody] WolTestRequest request)
        {
            if (!WolService.TryParseMacAddress(request.MacAddress, out _))
            {
                return BadRequest("A valid MAC address is required (for example AA:BB:CC:DD:EE:FF).");
            }

            if (request.WOLPort is < 1 or > ushort.MaxValue)
            {
                return BadRequest("Wake-on-LAN port must be between 1 and 65535.");
            }

            _logger.LogInformation("WOL Plugin: Sending test packet to {Mac} on port {Port}", request.MacAddress, request.WOLPort);

            if (!WolService.SendWolPacket(request.MacAddress, request.WOLPort, _logger))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Sending the magic packet failed. Check the server log for details.");
            }

            return NoContent();
        }

        /// <summary>
        /// Probes whether the storage server currently accepts TCP connections.
        /// </summary>
        /// <param name="host">Host to probe. Defaults to the configured IP address.</param>
        /// <param name="port">TCP port to probe. Defaults to the configured port.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The probe result, or 400 when no host or port is available.</returns>
        [HttpGet("Status")]
        [ProducesResponseType(typeof(WolStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WolStatusResponse>> Status([FromQuery] string? host, [FromQuery] int? port, CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration;
            var targetHost = string.IsNullOrWhiteSpace(host) ? config?.IpAddress : host.Trim();
            var targetPort = port ?? config?.Port ?? 0;

            if (string.IsNullOrWhiteSpace(targetHost))
            {
                return BadRequest("A storage server IP address or host name is required.");
            }

            if (targetPort is < 1 or > ushort.MaxValue)
            {
                return BadRequest("Storage server port must be between 1 and 65535.");
            }

            var stopwatch = Stopwatch.StartNew();
            var online = await RemoteServerUtil.IsServerOnlineAsync(targetHost, targetPort, _logger, cancellationToken: cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            return new WolStatusResponse
            {
                Host = targetHost,
                Port = targetPort,
                Online = online,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
    }
}
