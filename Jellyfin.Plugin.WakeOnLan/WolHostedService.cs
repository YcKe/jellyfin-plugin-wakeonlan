using Jellyfin.Plugin.WakeOnLan.Configuration;
using Jellyfin.Plugin.WakeOnLan.Util;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.WakeOnLan
{
    /// <summary>
    /// Listens for session and playback events and wakes the configured storage server when it is offline.
    /// </summary>
    public sealed class WolHostedService : IHostedService, IDisposable
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger<WolHostedService> _logger;

        // Only one probe-and-wake may run at a time; further triggers during that window are dropped.
        private readonly SemaphoreSlim _wakeLock = new(1, 1);
        private readonly CancellationTokenSource _shutdown = new();

        private DateTime _lastWol = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="WolHostedService"/> class.
        /// </summary>
        /// <param name="sessionManager">Session manager whose events trigger a wake.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        public WolHostedService(ISessionManager sessionManager, ILogger<WolHostedService> logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WOL Plugin: Starting background service");
            _sessionManager.SessionStarted += OnSessionStarted;
            _sessionManager.PlaybackStart += OnPlaybackStart;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WOL Plugin: Stopping background service");
            _sessionManager.SessionStarted -= OnSessionStarted;
            _sessionManager.PlaybackStart -= OnPlaybackStart;
            await _shutdown.CancelAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _shutdown.Dispose();
            _wakeLock.Dispose();
        }

        private void OnSessionStarted(object? sender, SessionEventArgs e)
        {
            TriggerWake("session started");
        }

        private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e)
        {
            TriggerWake("playback started");
        }

        /// <summary>
        /// Runs the probe-and-wake on the thread pool so the session manager's event thread is never blocked.
        /// </summary>
        private void TriggerWake(string reason)
        {
            if (_shutdown.IsCancellationRequested)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await ValidateAndSendWolToServerAsync(reason, _shutdown.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Shutting down.
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WOL Plugin: Unhandled error while handling '{Reason}'", reason);
                }
            });
        }

        private async Task ValidateAndSendWolToServerAsync(string reason, CancellationToken cancellationToken)
        {
            // Always read the live configuration: Jellyfin swaps the instance on every dashboard save.
            var config = Plugin.Instance?.Configuration;
            if (config is null)
            {
                _logger.LogWarning("WOL Plugin: No configuration available");
                return;
            }

            if (!WolService.TryParseMacAddress(config.MacAddress, out _))
            {
                _logger.LogWarning("WOL Plugin: MAC address '{Mac}' is missing or invalid, nothing to wake", config.MacAddress);
                return;
            }

            if (!IsCoolDownOver(config))
            {
                _logger.LogDebug("WOL Plugin: '{Reason}' ignored, still within the cooldown window", reason);
                return;
            }

            // Skip instead of queueing when another trigger is already probing or waking.
            if (!await _wakeLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("WOL Plugin: '{Reason}' ignored, a wake is already in progress", reason);
                return;
            }

            try
            {
                // Re-check under the lock so two near-simultaneous triggers cannot both pass the cooldown test.
                if (!IsCoolDownOver(config))
                {
                    return;
                }

                if (await IsServerOnlineAsync(config, cancellationToken).ConfigureAwait(false))
                {
                    _lastWol = DateTime.UtcNow;
                    _logger.LogInformation("WOL Plugin: Server is already online, no packet needed");
                    return;
                }

                _logger.LogInformation("WOL Plugin: '{Reason}' detected and server is offline, sending packet to {Mac}", reason, config.MacAddress);

                if (WolService.SendWolPacket(config.MacAddress, config.WOLPort, _logger))
                {
                    _lastWol = DateTime.UtcNow;
                }
            }
            finally
            {
                _wakeLock.Release();
            }
        }

        private Task<bool> IsServerOnlineAsync(PluginConfiguration config, CancellationToken cancellationToken)
        {
            // Without a probe target we cannot tell, so assume the server is offline and always wake it.
            if (string.IsNullOrWhiteSpace(config.IpAddress) || config.Port <= 0)
            {
                return Task.FromResult(false);
            }

            return RemoteServerUtil.IsServerOnlineAsync(config.IpAddress, config.Port, _logger, cancellationToken: cancellationToken);
        }

        private bool IsCoolDownOver(PluginConfiguration config)
        {
            return DateTime.UtcNow - _lastWol > TimeSpan.FromSeconds(Math.Max(0, config.WOLCoolDown));
        }
    }
}
