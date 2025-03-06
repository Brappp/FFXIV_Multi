using System;
using System.Collections.Generic;
using System.IO;

namespace FFXIV_Multi.Models
{
    /// <summary>
    /// Represents a Final Fantasy XIV client profile configuration
    /// </summary>
    public class ClientProfile
    {
        /// <summary>
        /// Unique identifier for the profile
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Display name for the profile
        /// </summary>
        public string ProfileName { get; set; } = "New Profile";

        /// <summary>
        /// Path to the client's configuration directory
        /// </summary>
        public string ConfigPath { get; set; }

        /// <summary>
        /// Path to the client's plugins directory
        /// </summary>
        public string PluginPath { get; set; }

        /// <summary>
        /// Path to the game installation
        /// </summary>
        public string GamePath { get; set; }

        /// <summary>
        /// Whether this is a Steam version
        /// </summary>
        public bool IsSteam { get; set; } = false;

        /// <summary>
        /// Whether to force DirectX 11 mode
        /// </summary>
        public bool ForceDX11 { get; set; } = true;

        /// <summary>
        /// Whether to disable auto-login
        /// </summary>
        public bool NoAutoLogin { get; set; } = false;

        /// <summary>
        /// Whether to use OTP (One-Time Password)
        /// </summary>
        public bool UseOTP { get; set; } = false;

        /// <summary>
        /// Whether to enable Dalamud plugin system
        /// </summary>
        public bool EnableDalamud { get; set; } = true;

        /// <summary>
        /// Additional launch arguments
        /// </summary>
        public string AdditionalArgs { get; set; } = "";

        /// <summary>
        /// Whether to automatically backup this profile
        /// </summary>
        public bool AutoBackup { get; set; } = false;

        /// <summary>
        /// Character information for this profile
        /// </summary>
        public CharacterInfo Character { get; set; } = new CharacterInfo();

        /// <summary>
        /// When the profile was last backed up
        /// </summary>
        public DateTime? LastBackup { get; set; }

        /// <summary>
        /// Total play time in minutes
        /// </summary>
        public long TotalPlayTimeMinutes { get; set; } = 0;

        /// <summary>
        /// Additional launch arguments
        /// </summary>
        public string AdditionalLaunchArgs { get; set; } = "";

        /// <summary>
        /// Whether to show a notification when this client is launched
        /// </summary>
        public bool ShowLaunchNotification { get; set; } = true;

        /// <summary>
        /// The date and time when this profile was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// The date and time when this profile was last used
        /// </summary>
        public DateTime LastUsed { get; set; } = DateTime.Now;

        /// <summary>
        /// Whether to create a desktop shortcut for this profile
        /// </summary>
        public bool CreateDesktopShortcut { get; set; } = false;

        /// <summary>
        /// List of plugin IDs that are explicitly enabled
        /// </summary>
        public List<string> EnabledPluginIds { get; set; } = new List<string>();

        /// <summary>
        /// List of plugin IDs that are explicitly disabled
        /// </summary>
        public List<string> DisabledPluginIds { get; set; } = new List<string>();

        /// <summary>
        /// Creates a clone of this profile with a new ID
        /// </summary>
        public ClientProfile Clone()
        {
            return new ClientProfile
            {
                Id = Guid.NewGuid(),
                ProfileName = $"{ProfileName} (Copy)",
                ConfigPath = ConfigPath,
                PluginPath = PluginPath,
                GamePath = GamePath,
                IsSteam = IsSteam,
                ForceDX11 = ForceDX11,
                NoAutoLogin = NoAutoLogin,
                UseOTP = UseOTP,
                EnableDalamud = EnableDalamud,
                AdditionalArgs = AdditionalArgs,
                AutoBackup = AutoBackup,
                Character = Character.Clone(),
                AdditionalLaunchArgs = AdditionalLaunchArgs,
                ShowLaunchNotification = ShowLaunchNotification,
                CreatedDate = DateTime.Now,
                LastUsed = DateTime.Now,
                CreateDesktopShortcut = CreateDesktopShortcut,
                EnabledPluginIds = new List<string>(EnabledPluginIds),
                DisabledPluginIds = new List<string>(DisabledPluginIds)
            };
        }

        /// <summary>
        /// Checks if the profile paths exist
        /// </summary>
        public bool ValidatePaths()
        {
            bool configValid = string.IsNullOrEmpty(ConfigPath) || Directory.Exists(ConfigPath);
            bool pluginValid = string.IsNullOrEmpty(PluginPath) || Directory.Exists(PluginPath);
            bool gameValid = string.IsNullOrEmpty(GamePath) || Directory.Exists(GamePath);

            return configValid && pluginValid && gameValid;
        }
    }

    /// <summary>
    /// Represents character information for an FFXIV profile
    /// </summary>
    public class CharacterInfo
    {
        /// <summary>
        /// Character name
        /// </summary>
        public string CharacterName { get; set; } = "";

        /// <summary>
        /// World or server name
        /// </summary>
        public string WorldName { get; set; } = "";

        /// <summary>
        /// Job or class abbreviation
        /// </summary>
        public string JobClass { get; set; } = "";

        /// <summary>
        /// Creates a clone of this character info
        /// </summary>
        public CharacterInfo Clone()
        {
            return new CharacterInfo
            {
                CharacterName = CharacterName,
                WorldName = WorldName,
                JobClass = JobClass
            };
        }
    }
}