using System;
using System.Collections.Generic;
using System.IO;

namespace FFXIVClientManager.Models
{
    /// <summary>
    /// Represents information about a Dalamud plugin
    /// </summary>
    public class PluginInfo
    {
        /// <summary>
        /// The display name of the plugin
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The internal name/ID of the plugin
        /// </summary>
        public string InternalName { get; set; }

        /// <summary>
        /// The author of the plugin
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The description of the plugin
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The version of the plugin
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Whether this is a developer plugin
        /// </summary>
        public bool IsDev { get; set; }

        /// <summary>
        /// Path to the plugin's installation directory
        /// </summary>
        public string InstallPath { get; set; }

        /// <summary>
        /// Path to the plugin's icon, if available
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Tags associated with the plugin
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// URL to the plugin's repository, if available
        /// </summary>
        public string RepoUrl { get; set; }

        /// <summary>
        /// Whether the plugin is enabled in the current profile
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Last time the plugin was updated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Whether a config file exists for this plugin
        /// </summary>
        public bool HasConfig
        {
            get
            {
                if (string.IsNullOrEmpty(InstallPath))
                    return false;

                // Check if there's a config directory in the plugin directory
                return Directory.Exists(Path.Combine(InstallPath, "config"));
            }
        }

        /// <summary>
        /// Estimated size of the plugin in bytes
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Formatted size string (KB, MB, etc.)
        /// </summary>
        public string FormattedSize
        {
            get
            {
                const long KB = 1024;
                const long MB = KB * 1024;
                const long GB = MB * 1024;

                if (Size < KB)
                    return $"{Size} B";
                if (Size < MB)
                    return $"{Size / KB:F2} KB";
                if (Size < GB)
                    return $"{Size / MB:F2} MB";
                return $"{Size / GB:F2} GB";
            }
        }

        /// <summary>
        /// Creates a clone of this PluginInfo
        /// </summary>
        public PluginInfo Clone()
        {
            return new PluginInfo
            {
                Name = Name,
                InternalName = InternalName,
                Author = Author,
                Description = Description,
                Version = Version,
                IsDev = IsDev,
                InstallPath = InstallPath,
                IconPath = IconPath,
                Tags = new List<string>(Tags),
                RepoUrl = RepoUrl,
                IsEnabled = IsEnabled,
                LastUpdated = LastUpdated,
                Size = Size
            };
        }
    }
}