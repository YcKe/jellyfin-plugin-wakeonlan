using Jellyfin.Plugin.WakeOnLan.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.WakeOnLan
{
    /// <summary>
    /// Plugin entry point registered with the Jellyfin host.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="appPaths">Application paths provided by the host.</param>
        /// <param name="xmlSerializer">Serializer used to persist the configuration.</param>
        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the current plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <inheritdoc />
        public override string Name => "Wake-on-LAN (WOL)";

        /// <inheritdoc />
        public override string Description => "Wakes a sleeping storage server when a client connects or starts playback.";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("4f1f54e2-6d22-4c39-be90-0cda215c64b6");

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return
            [
                new PluginPageInfo
                {
                    Name = "WakeOnLanConfig",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                }
            ];
        }
    }
}
