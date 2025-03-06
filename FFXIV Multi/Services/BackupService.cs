using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using FFXIV_Multi.Models;
using FFXIVClientManager.Models;
using FFXIVClientManager.Utils;

namespace FFXIVClientManager.Services
{
    /// <summary>
    /// Service for managing backups of client profiles
    /// </summary>
    public class BackupService
    {
        private readonly LogHelper _logHelper;
        private readonly LauncherSettings _settings;

        public event EventHandler<BackupCompletedEventArgs> BackupCompleted;
        public event EventHandler<BackupFailedEventArgs> BackupFailed;
        public event EventHandler<RestoreCompletedEventArgs> RestoreCompleted;
        public event EventHandler<RestoreFailedEventArgs> RestoreFailed;

        public BackupService(LauncherSettings settings, LogHelper logHelper)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logHelper = logHelper ?? throw new ArgumentNullException(nameof(logHelper));
        }

        /// <summary>
        /// Creates a backup of a client profile
        /// </summary>
        public async Task<string> CreateBackupAsync(ClientProfile profile, string customBackupPath = null)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            string backupPath = customBackupPath ?? _settings.BackupPath;

            if (string.IsNullOrEmpty(backupPath))
            {
                backupPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "FFXIVClientManager", "Backups");
            }

            try
            {
                // Create the backup directory if it doesn't exist
                if (!Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);

                // Create a profile-specific directory
                string profileBackupDir = Path.Combine(backupPath, profile.Id.ToString());
                if (!Directory.Exists(profileBackupDir))
                    Directory.CreateDirectory(profileBackupDir);

                // Create a timestamped backup filename
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupFileName = $"{profile.ProfileName}_{timestamp}.zip";
                string backupFilePath = Path.Combine(profileBackupDir, backupFileName);

                // Create a temporary directory for the backup contents
                string tempDir = Path.Combine(Path.GetTempPath(), $"FFXIVBackup_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    _logHelper.LogInfo($"Creating backup for profile: {profile.ProfileName}");

                    // Save profile data
                    await SaveProfileDataAsync(profile, tempDir);

                    // Backup config directory if it exists
                    if (!string.IsNullOrEmpty(profile.ConfigPath) && Directory.Exists(profile.ConfigPath))
                    {
                        string configBackupDir = Path.Combine(tempDir, "config");
                        Directory.CreateDirectory(configBackupDir);
                        FileUtils.CopyDirectory(profile.ConfigPath, configBackupDir);
                        _logHelper.LogInfo($"Backed up config directory: {profile.ConfigPath}");
                    }

                    // Backup plugin directory if it exists and backup plugins is enabled
                    if (_settings.BackupPlugins && !string.IsNullOrEmpty(profile.PluginPath) && Directory.Exists(profile.PluginPath))
                    {
                        string pluginBackupDir = Path.Combine(tempDir, "plugins");
                        Directory.CreateDirectory(pluginBackupDir);
                        FileUtils.CopyDirectory(profile.PluginPath, pluginBackupDir);
                        _logHelper.LogInfo($"Backed up plugin directory: {profile.PluginPath}");
                    }

                    // Create ZIP archive
                    if (File.Exists(backupFilePath))
                        File.Delete(backupFilePath);

                    ZipFile.CreateFromDirectory(tempDir, backupFilePath);
                    _logHelper.LogInfo($"Created backup archive: {backupFilePath}");

                    // Update profile
                    profile.LastBackup = DateTime.Now;

                    // Notify listeners
                    BackupCompleted?.Invoke(this, new BackupCompletedEventArgs(profile, backupFilePath));

                    return backupFilePath;
                }
                finally
                {
                    // Clean up temporary directory
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error creating backup: {ex.Message}", ex);
                BackupFailed?.Invoke(this, new BackupFailedEventArgs(profile, ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Restores a client profile from a backup
        /// </summary>
        public async Task<bool> RestoreBackupAsync(string backupFilePath, ClientProfile profile)
        {
            if (string.IsNullOrEmpty(backupFilePath))
                throw new ArgumentException("Backup file path cannot be null or empty", nameof(backupFilePath));

            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (!File.Exists(backupFilePath))
            {
                _logHelper.LogError($"Backup file does not exist: {backupFilePath}");
                RestoreFailed?.Invoke(this, new RestoreFailedEventArgs(profile, "Backup file does not exist"));
                return false;
            }

            try
            {
                _logHelper.LogInfo($"Restoring backup for profile: {profile.ProfileName}");

                // Create a temporary directory for extraction
                string tempDir = Path.Combine(Path.GetTempPath(), $"FFXIVRestore_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Extract the backup
                    ZipFile.ExtractToDirectory(backupFilePath, tempDir);

                    // Restore config directory if it exists in the backup
                    string configBackupDir = Path.Combine(tempDir, "config");
                    if (Directory.Exists(configBackupDir) && !string.IsNullOrEmpty(profile.ConfigPath))
                    {
                        // Create the profile's config directory if it doesn't exist
                        if (!Directory.Exists(profile.ConfigPath))
                            Directory.CreateDirectory(profile.ConfigPath);
                        else
                            Directory.Delete(profile.ConfigPath, true); // Clear existing config

                        // Copy the backup config to the profile's config directory
                        FileUtils.CopyDirectory(configBackupDir, profile.ConfigPath);
                        _logHelper.LogInfo($"Restored config directory to: {profile.ConfigPath}");
                    }

                    // Restore plugin directory if it exists in the backup and restore plugins is enabled
                    string pluginBackupDir = Path.Combine(tempDir, "plugins");
                    if (_settings.RestorePlugins && Directory.Exists(pluginBackupDir) && !string.IsNullOrEmpty(profile.PluginPath))
                    {
                        // Create the profile's plugin directory if it doesn't exist
                        if (!Directory.Exists(profile.PluginPath))
                            Directory.CreateDirectory(profile.PluginPath);
                        else
                            Directory.Delete(profile.PluginPath, true); // Clear existing plugins

                        // Copy the backup plugins to the profile's plugin directory
                        FileUtils.CopyDirectory(pluginBackupDir, profile.PluginPath);
                        _logHelper.LogInfo($"Restored plugin directory to: {profile.PluginPath}");
                    }

                    // Notify listeners
                    RestoreCompleted?.Invoke(this, new RestoreCompletedEventArgs(profile, backupFilePath));

                    return true;
                }
                finally
                {
                    // Clean up temporary directory
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error restoring backup: {ex.Message}", ex);
                RestoreFailed?.Invoke(this, new RestoreFailedEventArgs(profile, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Gets a list of available backups for a profile
        /// </summary>
        public List<BackupInfo> GetAvailableBackups(ClientProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            string backupPath = _settings.BackupPath;

            if (string.IsNullOrEmpty(backupPath))
            {
                backupPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "FFXIVClientManager", "Backups");
            }

            string profileBackupDir = Path.Combine(backupPath, profile.Id.ToString());

            if (!Directory.Exists(profileBackupDir))
                return new List<BackupInfo>();

            try
            {
                var backupInfos = new List<BackupInfo>();

                foreach (var backupFile in Directory.GetFiles(profileBackupDir, "*.zip"))
                {
                    var fileName = Path.GetFileName(backupFile);
                    var fileInfo = new FileInfo(backupFile);

                    // Parse timestamp from filename
                    DateTime timestamp = DateTime.MinValue;
                    var parts = fileName.Split('_');

                    if (parts.Length >= 2)
                    {
                        var datePart = parts[parts.Length - 2];
                        var timePart = parts[parts.Length - 1].Replace(".zip", "");

                        if (DateTime.TryParseExact(
                            $"{datePart}_{timePart}",
                            "yyyy-MM-dd_HH-mm-ss",
                            null,
                            System.Globalization.DateTimeStyles.None,
                            out timestamp))
                        {
                            // Use the parsed timestamp
                        }
                        else
                        {
                            // Fall back to file creation time
                            timestamp = fileInfo.CreationTime;
                        }
                    }
                    else
                    {
                        // Fall back to file creation time
                        timestamp = fileInfo.CreationTime;
                    }

                    backupInfos.Add(new BackupInfo
                    {
                        ProfileId = profile.Id,
                        FileName = fileName,
                        FilePath = backupFile,
                        CreationTime = timestamp,
                        Size = fileInfo.Length
                    });
                }

                // Sort by creation time descending
                return backupInfos.OrderByDescending(b => b.CreationTime).ToList();
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error getting available backups: {ex.Message}", ex);
                return new List<BackupInfo>();
            }
        }

        /// <summary>
        /// Deletes a specific backup file
        /// </summary>
        public bool DeleteBackup(string backupFilePath)
        {
            if (string.IsNullOrEmpty(backupFilePath))
                throw new ArgumentException("Backup file path cannot be null or empty", nameof(backupFilePath));

            if (!File.Exists(backupFilePath))
            {
                _logHelper.LogWarning($"Backup file does not exist: {backupFilePath}");
                return false;
            }

            try
            {
                File.Delete(backupFilePath);
                _logHelper.LogInfo($"Deleted backup file: {backupFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error deleting backup: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Saves profile data to a JSON file in the backup directory
        /// </summary>
        private async Task SaveProfileDataAsync(ClientProfile profile, string backupDir)
        {
            string profileDataPath = Path.Combine(backupDir, "profile.json");
            string json = JsonHelper.Serialize(profile);
            await File.WriteAllTextAsync(profileDataPath, json);
        }

        /// <summary>
        /// Creates a backup for all profiles with auto-backup enabled
        /// </summary>
        public async Task<Dictionary<Guid, string>> BackupAllProfilesAsync(IEnumerable<ClientProfile> profiles)
        {
            var results = new Dictionary<Guid, string>();

            foreach (var profile in profiles.Where(p => p.AutoBackup))
            {
                try
                {
                    var backupPath = await CreateBackupAsync(profile);
                    results[profile.Id] = backupPath;
                    _logHelper.LogInfo($"Auto-backup completed for profile: {profile.ProfileName}");
                }
                catch (Exception ex)
                {
                    _logHelper.LogError($"Auto-backup failed for profile {profile.ProfileName}: {ex.Message}", ex);
                    results[profile.Id] = null; // Indicate failure
                }
            }

            return results;
        }

        /// <summary>
        /// Cleans up old backups beyond the specified retention limit
        /// </summary>
        public void CleanupOldBackups(int maxBackupsPerProfile = 5)
        {
            string backupPath = _settings.BackupPath;

            if (string.IsNullOrEmpty(backupPath))
            {
                backupPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "FFXIVClientManager", "Backups");
            }

            if (!Directory.Exists(backupPath))
                return;

            try
            {
                foreach (var profileDir in Directory.GetDirectories(backupPath))
                {
                    var backupFiles = Directory.GetFiles(profileDir, "*.zip")
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.CreationTime)
                        .ToList();

                    if (backupFiles.Count <= maxBackupsPerProfile)
                        continue;

                    // Delete excess backups
                    foreach (var file in backupFiles.Skip(maxBackupsPerProfile))
                    {
                        try
                        {
                            File.Delete(file.FullName);
                            _logHelper.LogInfo($"Deleted old backup: {file.Name}");
                        }
                        catch (Exception ex)
                        {
                            _logHelper.LogError($"Error deleting old backup {file.Name}: {ex.Message}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error cleaning up old backups: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Represents information about a backup file
    /// </summary>
    public class BackupInfo
    {
        /// <summary>
        /// The ID of the profile this backup belongs to
        /// </summary>
        public Guid ProfileId { get; set; }

        /// <summary>
        /// The name of the backup file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The full path to the backup file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// When the backup was created
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// The size of the backup file in bytes
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets a formatted string representing the backup size
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
    }

    /// <summary>
    /// Event arguments for when a backup is completed
    /// </summary>
    public class BackupCompletedEventArgs : EventArgs
    {
        public ClientProfile Profile { get; }
        public string BackupFilePath { get; }

        public BackupCompletedEventArgs(ClientProfile profile, string backupFilePath)
        {
            Profile = profile;
            BackupFilePath = backupFilePath;
        }
    }

    /// <summary>
    /// Event arguments for when a backup fails
    /// </summary>
    public class BackupFailedEventArgs : EventArgs
    {
        public ClientProfile Profile { get; }
        public string ErrorMessage { get; }

        public BackupFailedEventArgs(ClientProfile profile, string errorMessage)
        {
            Profile = profile;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Event arguments for when a restore is completed
    /// </summary>
    public class RestoreCompletedEventArgs : EventArgs
    {
        public ClientProfile Profile { get; }
        public string BackupFilePath { get; }

        public RestoreCompletedEventArgs(ClientProfile profile, string backupFilePath)
        {
            Profile = profile;
            BackupFilePath = backupFilePath;
        }
    }

    /// <summary>
    /// Event arguments for when a restore fails
    /// </summary>
    public class RestoreFailedEventArgs : EventArgs
    {
        public ClientProfile Profile { get; }
        public string ErrorMessage { get; }

        public RestoreFailedEventArgs(ClientProfile profile, string errorMessage)
        {
            Profile = profile;
            ErrorMessage = errorMessage;
        }
    }
}