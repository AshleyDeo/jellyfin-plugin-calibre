using System;
using System.Diagnostics.CodeAnalysis;
using Jellyfin.Plugin.Calibre.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Calibre.Configuration
{
    /// <summary>
    /// Plugin configuration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets user Library Configs.
        /// </summary>
        [SuppressMessage(category: "Performance", checkId: "CA1819", Target = "LibConfigs", Justification = "Xml Serializer doesn't support IReadOnlyList")]
        public ILibrary[] LibConfigs { get; set; } = Array.Empty<ILibrary>();
    }
}
