using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FFXIV_Multi.Models;
using FFXIVClientManager.Models;
using FFXIVClientManager.Utils;

namespace FFXIVClientManager.Services
{
    /// <summary>
    /// Service for managing Dalamud plugins across different profiles
    /// </summary>
    public class PluginService
    {
        private readonly LogHelper _logHelper;
        private readonly LauncherSettings _settings;

        public PluginService(LauncherSettings settings, LogHelper logHelper)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logHelper = logHelper ?? throw new ArgumentNullException(nameof(logHelper));
        }

        /// <summary>
        /// Gets a list of installed plugins for a specific profile
        /// </summary>
        public async Task<List<PluginInfo>> GetInstalledPluginsAsync(ClientProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (string.IsNullOrEmpty(profile.PluginPath) || !Directory.Exists(profile.PluginPath))
                return new List<PluginInfo>();

            try
            {
                var result = new List<PluginInfo>();
                var devPluginsPath = Path.Combine(profile.PluginPath, "dev");
                var installedPluginsPath = Path.Combine(profile.PluginPath, "installed");

                // Check for dev plugins
                if (Directory.Exists(devPluginsPath))
                {
                    foreach (var pluginDirectory in Directory.GetDirectories(devPluginsPath))
                    {
                        var pluginInfo = await GetPluginInfoFromDirectory(pluginDirectory, true);
                        if (pluginInfo != null)
                        {
                            result.Add(pluginInfo);
                        }
                    }
                }

                // Check for installed plugins
                if (Directory.Exists(installedPluginsPath))
                {
                    foreach (var pluginDirectory in Directory.GetDirectories(installedPluginsPath))
                    {
                        var pluginInfo = await GetPluginInfoFromDirectory(pluginDirectory, false);
                        if (pluginInfo != null)
                        {
                            result.Add(pluginInfo);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error getting installed plugins: {ex.Message}", ex);
                return new List<PluginInfo>();
            }
        }

        /// <summary>
        /// Copies plugins from one profile to another
        /// </summary>
        public async Task<bool> CopyPluginsAsync(ClientProfile sourceProfile, ClientProfile targetProfile, IEnumerable<string> pluginIds = null)
        {
            if (sourceProfile == null)
                throw new ArgumentNullException(nameof(sourceProfile));

            if (targetProfile == null)
                throw new ArgumentNullException(nameof(targetProfile));

            if (string.IsNullOrEmpty(sourceProfile.PluginPath) || !Directory.Exists(sourceProfile.PluginPath))
            {
                _logHelper.LogError($"Source plugin path does not exist: {sourceProfile.PluginPath}");
                return false;
            }

            if (string.IsNullOrEmpty(targetProfile.PluginPath))
            {
                _logHelper.LogError("Target plugin path is not set");
                return false;
            }

            try
            {
                // Ensure target directory exists
                if (!Directory.Exists(targetProfile.PluginPath))
                    Directory.CreateDirectory(targetProfile.PluginPath);

                // Get source plugins
                var sourcePlugins = await GetInstalledPluginsAsync(sourceProfile);

                // Filter plugins if specific IDs are provided
                if (pluginIds != null)
                {
                    sourcePlugins = sourcePlugins.Where(p => pluginIds.Contains(p.InternalName)).ToList();
                }

                _logHelper.LogInfo($"Copying {sourcePlugins.Count} plugins from {sourceProfile.ProfileName} to {targetProfile.ProfileName}");

                // Copy each plugin
                foreach (var plugin in sourcePlugins)
                {
                    var sourceDir = plugin.InstallPath;
                    var targetSubDir = plugin.IsDev ? "dev" : "installed";
                    var targetPluginDir = Path.Combine(targetProfile.PluginPath, targetSubDir, plugin.InternalName);

                    // Ensure target plugin directory exists
                    if (!Directory.Exists(Path.Combine(targetProfile.PluginPath, targetSubDir)))
                        Directory.CreateDirectory(Path.Combine(targetProfile.PluginPath, targetSubDir));

                    // Remove existing plugin if it exists
                    if (Directory.Exists(targetPluginDir))
                        Directory.Delete(targetPluginDir, true);

                    // Copy plugin directory
                    FileUtils.CopyDirectory(sourceDir, targetPluginDir);

                    _logHelper.LogInfo($"Copied plugin: {plugin.Name}");
                }

                // Copy plugin configurations
                await CopyPluginConfigurationsAsync(sourceProfile, targetProfile);

                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error copying plugins: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Copies plugin configurations from one profile to another
        /// </summary>
        private async Task<bool> CopyPluginConfigurationsAsync(ClientProfile sourceProfile, ClientProfile targetProfile)
        {
            if (string.IsNullOrEmpty(sourceProfile.ConfigPath) || !Directory.Exists(sourceProfile.ConfigPath))
                return false;

            if (string.IsNullOrEmpty(targetProfile.ConfigPath))
                return false;

            try
            {
                // Ensure target config directory exists
                if (!Directory.Exists(targetProfile.ConfigPath))
                    Directory.CreateDirectory(targetProfile.ConfigPath);

                // Path to Dalamud configuration folders
                var sourceDalamudDir = Path.Combine(sourceProfile.ConfigPath, "dalamud");
                var targetDalamudDir = Path.Combine(targetProfile.ConfigPath, "dalamud");

                if (!Directory.Exists(sourceDalamudDir))
                    return true; // No configurations to copy

                // Ensure target Dalamud directory exists
                if (!Directory.Exists(targetDalamudDir))
                    Directory.CreateDirectory(targetDalamudDir);

                // Copy plugin configurations directory
                var sourcePluginConfigDir = Path.Combine(sourceDalamudDir, "pluginConfigs");
                var targetPluginConfigDir = Path.Combine(targetDalamudDir, "pluginConfigs");

                if (Directory.Exists(sourcePluginConfigDir))
                {
                    // Ensure target plugin config directory exists
                    if (!Directory.Exists(targetPluginConfigDir))
                        Directory.CreateDirectory(targetPluginConfigDir);

                    // Copy all plugin configuration files
                    foreach (var configFile in Directory.GetFiles(sourcePluginConfigDir))
                    {
                        var fileName = Path.GetFileName(configFile);
                        var targetFilePath = Path.Combine(targetPluginConfigDir, fileName);
                        File.Copy(configFile, targetFilePath, true);
                        _logHelper.LogInfo($"Copied plugin configuration: {fileName}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error copying plugin configurations: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Removes a plugin from a profile
        /// </summary>
        public async Task<bool> RemovePluginAsync(ClientProfile profile, string pluginId)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (string.IsNullOrEmpty(pluginId))
                throw new ArgumentException("Plugin ID cannot be empty", nameof(pluginId));

            if (string.IsNullOrEmpty(profile.PluginPath) || !Directory.Exists(profile.PluginPath))
            {
                _logHelper.LogError($"Plugin path does not exist: {profile.PluginPath}");
                return false;
            }

            try
            {
                // Check dev plugins
                var devPluginPath = Path.Combine(profile.PluginPath, "dev", pluginId);
                if (Directory.Exists(devPluginPath))
                {
                    Directory.Delete(devPluginPath, true);
                    _logHelper.LogInfo($"Removed dev plugin: {pluginId}");
                    return true;
                }

                // Check installed plugins
                var installedPluginPath = Path.Combine(profile.PluginPath, "installed", pluginId);
                if (Directory.Exists(installedPluginPath))
                {
                    Directory.Delete(installedPluginPath, true);
                    _logHelper.LogInfo($"Removed installed plugin: {pluginId}");
                    return true;
                }

                _logHelper.LogWarning($"Plugin not found: {pluginId}");
                return false;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error removing plugin: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Synchronizes plugins between profiles
        /// </summary>
        public async Task<bool> SyncPluginsAsync(ClientProfile sourceProfile, ClientProfile targetProfile)
        {
            if (sourceProfile == null)
                throw new ArgumentNullException(nameof(sourceProfile));

            if (targetProfile == null)
                throw new ArgumentNullException(nameof(targetProfile));

            try
            {
                // Get plugins from both profiles
                var sourcePlugins = await GetInstalledPluginsAsync(sourceProfile);
                var targetPlugins = await GetInstalledPluginsAsync(targetProfile);

                // Calculate which plugins to add to the target profile
                var pluginsToAdd = sourcePlugins
                    .Where(sp => !targetPlugins.Any(tp => tp.InternalName == sp.InternalName))
                    .Select(p => p.InternalName)
                    .ToList();

                // Calculate which plugins to remove from the target profile
                var pluginsToRemove = targetPlugins
                    .Where(tp => !sourcePlugins.Any(sp => sp.InternalName == tp.InternalName))
                    .Select(p => p.InternalName)
                    .ToList();

                _logHelper.LogInfo($"Synchronizing plugins: Adding {pluginsToAdd.Count}, Removing {pluginsToRemove.Count}");

                // Remove plugins
                foreach (var pluginId in pluginsToRemove)
                {
                    await RemovePluginAsync(targetProfile, pluginId);
                }

                // Add plugins
                if (pluginsToAdd.Count > 0)
                {
                    await CopyPluginsAsync(sourceProfile, targetProfile, pluginsToAdd);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error synchronizing plugins: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Extracts plugin information from a plugin directory
        /// </summary>
        private async Task<PluginInfo> GetPluginInfoFromDirectory(string pluginDirectory, bool isDev)
        {
            try
            {
                var pluginName = Path.GetFileName(pluginDirectory);
                var manifestPath = Path.Combine(pluginDirectory, "plugin.json");

                if (!File.Exists(manifestPath))
                {
                    _logHelper.LogWarning($"No plugin manifest found in {pluginDirectory}");
                    return null;
                }

                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson);

                if (manifest == null)
                {
                    _logHelper.LogWarning($"Failed to parse plugin manifest in {pluginDirectory}");
                    return null;
                }

                return new PluginInfo
                {
                    Name = manifest.Name,
                    InternalName = pluginName,
                    Author = manifest.Author,
                    Description = manifest.Description,
                    Version = manifest.AssemblyVersion,
                    IsDev = isDev,
                    InstallPath = pluginDirectory,
                    IconPath = File.Exists(Path.Combine(pluginDirectory, "icon.png"))
                        ? Path.Combine(pluginDirectory, "icon.png")
                        : null
                };
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error reading plugin info: {ex.Message}", ex);
                return null;
            }
        }
    }

    /// <summary>
    /// Represents the structure of a Dalamud plugin manifest file
    /// </summary>
    public class PluginManifest
    {
        public string Author { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string InternalName { get; set; }
        public string AssemblyVersion { get; set; }
        public string TestingAssemblyVersion { get; set; }
        public bool IsHide { get; set; }
        public string RepoUrl { get; set; }
        public string ApplicableVersion { get; set; }
        public string[] Tags { get; set; }
        // Add other fields as needed
    }
}