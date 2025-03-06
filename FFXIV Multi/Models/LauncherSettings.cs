using System;
using System.IO;
using System.Text.Json;

namespace FFXIV_Multi.Models
{
    /// <summary>
    /// Holds configuration settings for the launcher, including file paths and user preferences.
    /// </summary>
    public class LauncherSettings
    {
        // Paths and general settings
        public string XIVLauncherPath { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public string LogPath { get; set; } = string.Empty;
        public int DefaultLaunchDelay { get; set; } = 0;
        public int MaxBackupsPerProfile { get; set; } = 5;

        // Backup/Restore options
        public bool BackupPlugins { get; set; } = false;
        public bool RestorePlugins { get; set; } = false;

        // UI/behavior settings
        public string Theme { get; set; } = "System";  // "Light", "Dark", or "System"
        public bool ShowTooltips { get; set; } = true;
        public bool ConfirmOnClose { get; set; } = true;
        public bool MinimizeToTray { get; set; } = false;

        // Startup and update settings
        public bool CheckForRunningInstances { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
        public bool CheckForUpdates { get; set; } = true;

        // Automatic backup settings
        public bool AutoBackupBeforeLaunch { get; set; } = false;
        public bool AutoBackupAfterExit { get; set; } = false;

        // File path for saving/loading settings (in AppData\FFXIVClientManager)
        private readonly string _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FFXIVClientManager", "settings.json");

        /// <summary>
        /// Loads settings from disk (if available) into this instance.
        /// If no settings file exists, defaults (initialized above) are used.
        /// </summary>
        public void LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                // No settings file: use default values
                return;
            }
            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<LauncherSettings>(json);
                if (loaded != null)
                {
                    // Copy loaded values into this instance
                    XIVLauncherPath = loaded.XIVLauncherPath;
                    BackupPath = loaded.BackupPath;
                    LogPath = loaded.LogPath;
                    DefaultLaunchDelay = loaded.DefaultLaunchDelay;
                    MaxBackupsPerProfile = loaded.MaxBackupsPerProfile;
                    BackupPlugins = loaded.BackupPlugins;
                    RestorePlugins = loaded.RestorePlugins;
                    Theme = loaded.Theme;
                    ShowTooltips = loaded.ShowTooltips;
                    ConfirmOnClose = loaded.ConfirmOnClose;
                    MinimizeToTray = loaded.MinimizeToTray;
                    CheckForRunningInstances = loaded.CheckForRunningInstances;
                    StartWithWindows = loaded.StartWithWindows;
                    CheckForUpdates = loaded.CheckForUpdates;
                    AutoBackupBeforeLaunch = loaded.AutoBackupBeforeLaunch;
                    AutoBackupAfterExit = loaded.AutoBackupAfterExit;
                }
            }
            catch (Exception)
            {
                // If there's an error reading or parsing, ignore and use defaults.
                // (Error handling can be added here, e.g., logging to a file or console.)
            }
        }

        /// <summary>
        /// Saves the current settings to disk (in a JSON file under AppData).
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                // Ensure the settings directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath) ?? string.Empty);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception)
            {
                // If saving fails, we can log a warning (not throwing to avoid crashing on exit).
            }
        }
    }
}
