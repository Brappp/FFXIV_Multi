using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FFXIV_Multi.Models;
using FFXIV_Multi.Utils;
using FFXIVClientManager.Models;
using FFXIVClientManager.Utils;
using Newtonsoft.Json;

namespace FFXIVClientManager.Services
{
    /// <summary>
    /// Service for managing client profiles
    /// </summary>
    public class ProfileManager
    {
        private readonly LauncherSettings _settings;
        private readonly LogHelper _logHelper;
        private readonly string _profilesDirectory;

        public event EventHandler<ProfileChangedEventArgs> ProfileChanged;
        public event EventHandler<ProfileDeletedEventArgs> ProfileDeleted;

        public ProfileManager(LauncherSettings settings, LogHelper logHelper)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logHelper = logHelper ?? throw new ArgumentNullException(nameof(logHelper));

            _profilesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FFXIVClientManager", "Profiles");

            // Ensure directory exists
            if (!Directory.Exists(_profilesDirectory))
                Directory.CreateDirectory(_profilesDirectory);
        }

        /// <summary>
        /// Gets all client profiles
        /// </summary>
        public async Task<List<ClientProfile>> GetAllProfilesAsync()
        {
            var profiles = new List<ClientProfile>();

            try
            {
                foreach (var file in Directory.GetFiles(_profilesDirectory, "*.json"))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var profile = JsonConvert.DeserializeObject<ClientProfile>(json);

                        if (profile != null)
                        {
                            profiles.Add(profile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logHelper.LogError($"Error loading profile from {file}: {ex.Message}", ex);
                    }
                }

                // Sort profiles by name
                return profiles.OrderBy(p => p.ProfileName).ToList();
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error loading profiles: {ex.Message}", ex);
                return profiles;
            }
        }

        /// <summary>
        /// Gets a client profile by ID
        /// </summary>
        public ClientProfile GetProfileById(Guid id)
        {
            try
            {
                string profilePath = Path.Combine(_profilesDirectory, $"{id}.json");

                if (!File.Exists(profilePath))
                    return null;

                var json = File.ReadAllText(profilePath);
                return JsonConvert.DeserializeObject<ClientProfile>(json);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error getting profile {id}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Saves a client profile
        /// </summary>
        public async Task SaveProfileAsync(ClientProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            try
            {
                string profilePath = Path.Combine(_profilesDirectory, $"{profile.Id}.json");
                string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                await File.WriteAllTextAsync(profilePath, json);
                _logHelper.LogInfo($"Saved profile {profile.ProfileName}");

                // Notify listeners
                OnProfileChanged(profile);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error saving profile {profile.ProfileName}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes a client profile by ID
        /// </summary>
        public async Task<bool> DeleteProfileAsync(Guid id)
        {
            try
            {
                // First, load the profile to get its name for the event
                var profile = GetProfileById(id);

                string profilePath = Path.Combine(_profilesDirectory, $"{id}.json");

                if (!File.Exists(profilePath))
                    return false;

                File.Delete(profilePath);
                _logHelper.LogInfo($"Deleted profile {id}");

                // Notify listeners
                if (profile != null)
                {
                    OnProfileDeleted(profile.Id, profile.ProfileName);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error deleting profile {id}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Creates a default profile
        /// </summary>
        public ClientProfile CreateDefaultProfile()
        {
            try
            {
                // Create a new profile with default settings
                var profile = new ClientProfile
                {
                    Id = Guid.NewGuid(),
                    ProfileName = "Default Profile",
                    ConfigPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "FFXIVClientManager", "Clients", "Default", "Config"),
                    PluginPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "FFXIVClientManager", "Clients", "Default", "Plugins"),
                    ForceDX11 = true,
                    EnableDalamud = true,
                    AutoBackup = true
                };

                return profile;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error creating default profile: {ex.Message}", ex);
                return new ClientProfile(); // Return a basic profile
            }
        }

        /// <summary>
        /// Validates profile paths to ensure they're unique
        /// </summary>
        public bool ValidateProfilePaths(ClientProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            try
            {
                var allProfiles = GetAllProfilesAsync().Result;

                // Skip the current profile when checking for duplicates
                var otherProfiles = allProfiles.Where(p => p.Id != profile.Id).ToList();

                // Check for duplicate config path
                if (!string.IsNullOrEmpty(profile.ConfigPath) &&
                    otherProfiles.Any(p => string.Equals(p.ConfigPath, profile.ConfigPath, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                // Check for duplicate plugin path
                if (!string.IsNullOrEmpty(profile.PluginPath) &&
                    otherProfiles.Any(p => string.Equals(p.PluginPath, profile.PluginPath, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error validating profile paths: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Creates new default paths for a profile to avoid conflicts
        /// </summary>
        public void GenerateUniquePaths(ClientProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            try
            {
                int suffix = 1;
                var basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "FFXIVClientManager", "Clients");

                string configPath;
                string pluginPath;

                do
                {
                    string folderName = $"Client{suffix}";
                    configPath = Path.Combine(basePath, folderName, "Config");
                    pluginPath = Path.Combine(basePath, folderName, "Plugins");

                    // Update the profile
                    profile.ConfigPath = configPath;
                    profile.PluginPath = pluginPath;

                    // Check if paths are unique
                    suffix++;
                } while (!ValidateProfilePaths(profile) && suffix < 100); // Limit to prevent infinite loop
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error generating unique paths: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates a profile's play time
        /// </summary>
        public async Task UpdatePlayTimeAsync(Guid profileId, long additionalMinutes)
        {
            try
            {
                var profile = GetProfileById(profileId);
                if (profile == null)
                    return;

                profile.TotalPlayTimeMinutes += additionalMinutes;
                await SaveProfileAsync(profile);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error updating play time: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Formats play time as a readable string
        /// </summary>
        public static string FormatPlayTime(long minutes)
        {
            var timeSpan = TimeSpan.FromMinutes(minutes);

            if (timeSpan.TotalHours < 1)
                return $"{timeSpan.Minutes} minutes";

            if (timeSpan.TotalDays < 1)
                return $"{(int)timeSpan.TotalHours} hours, {timeSpan.Minutes} minutes";

            return $"{(int)timeSpan.TotalDays} days, {timeSpan.Hours} hours";
        }

        #region Event Handlers

        protected virtual void OnProfileChanged(ClientProfile profile)
        {
            ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(profile));
        }

        protected virtual void OnProfileDeleted(Guid profileId, string profileName)
        {
            ProfileDeleted?.Invoke(this, new ProfileDeletedEventArgs(profileId, profileName));
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for when a profile is changed
    /// </summary>
    public class ProfileChangedEventArgs : EventArgs
    {
        public ClientProfile Profile { get; }

        public ProfileChangedEventArgs(ClientProfile profile)
        {
            Profile = profile;
        }
    }

    /// <summary>
    /// Event arguments for when a profile is deleted
    /// </summary>
    public class ProfileDeletedEventArgs : EventArgs
    {
        public Guid ProfileId { get; }
        public string ProfileName { get; }

        public ProfileDeletedEventArgs(Guid profileId, string profileName)
        {
            ProfileId = profileId;
            ProfileName = profileName;
        }
    }
}