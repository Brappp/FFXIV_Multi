using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using FFXIVClientManager.Utils;

namespace FFXIVClientManager.Services
{
    /// <summary>
    /// Service for checking for application updates
    /// </summary>
    public class UpdateChecker
    {
        private readonly LogHelper _logHelper;
        private readonly string _updateUrl;
        private readonly Version _currentVersion;

        public event EventHandler<UpdateAvailableEventArgs> UpdateAvailable;
        public event EventHandler<UpdateCheckFailedEventArgs> UpdateCheckFailed;

        /// <summary>
        /// Initializes a new instance of the UpdateChecker class
        /// </summary>
        public UpdateChecker(LogHelper logHelper, string updateUrl = null)
        {
            _logHelper = logHelper ?? throw new ArgumentNullException(nameof(logHelper));
            _updateUrl = updateUrl ?? "https://api.github.com/repos/yourusername/FFXIVClientManager/releases/latest";
            _currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        }

        /// <summary>
        /// Checks for updates
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                _logHelper.LogInfo("Checking for updates...");

                // Create a temporary HttpClient to check for updates
                using (var httpClient = new HttpClient())
                {
                    // GitHub API requires a user agent
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "FFXIVClientManager-UpdateChecker");

                    // Get the latest release info from GitHub
                    var response = await httpClient.GetAsync(_updateUrl);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var releaseInfo = JsonSerializer.Deserialize<GitHubReleaseInfo>(content);

                    if (releaseInfo == null || string.IsNullOrEmpty(releaseInfo.TagName))
                    {
                        _logHelper.LogWarning("Failed to parse update information.");
                        OnUpdateCheckFailed("Failed to parse update information.");
                        return null;
                    }

                    // Parse the version from the tag name
                    string versionString = releaseInfo.TagName.TrimStart('v');
                    if (!Version.TryParse(versionString, out Version latestVersion))
                    {
                        _logHelper.LogWarning($"Failed to parse version: {releaseInfo.TagName}");
                        OnUpdateCheckFailed($"Failed to parse version: {releaseInfo.TagName}");
                        return null;
                    }

                    bool updateAvailable = latestVersion > _currentVersion;

                    var updateInfo = new UpdateInfo
                    {
                        CurrentVersion = _currentVersion,
                        LatestVersion = latestVersion,
                        ReleaseUrl = releaseInfo.HtmlUrl,
                        ReleaseNotes = releaseInfo.Body,
                        IsUpdateAvailable = updateAvailable,
                        PublishedAt = releaseInfo.PublishedAt
                    };

                    if (updateAvailable)
                    {
                        _logHelper.LogInfo($"Update available: {latestVersion} (current: {_currentVersion})");
                        OnUpdateAvailable(updateInfo);
                    }
                    else
                    {
                        _logHelper.LogInfo($"No updates available. Current version: {_currentVersion}, Latest version: {latestVersion}");
                    }

                    return updateInfo;
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error checking for updates: {ex.Message}", ex);
                OnUpdateCheckFailed(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Opens the download URL in the default browser
        /// </summary>
        public void OpenDownloadPage(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error opening download page: {ex.Message}", ex);
            }
        }

        #region Event Handlers

        protected virtual void OnUpdateAvailable(UpdateInfo updateInfo)
        {
            UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(updateInfo));
        }

        protected virtual void OnUpdateCheckFailed(string errorMessage)
        {
            UpdateCheckFailed?.Invoke(this, new UpdateCheckFailedEventArgs(errorMessage));
        }

        #endregion
    }

    /// <summary>
    /// Contains information about an available update
    /// </summary>
    public class UpdateInfo
    {
        public Version CurrentVersion { get; set; }
        public Version LatestVersion { get; set; }
        public string ReleaseUrl { get; set; }
        public string ReleaseNotes { get; set; }
        public bool IsUpdateAvailable { get; set; }
        public DateTime PublishedAt { get; set; }
    }

    /// <summary>
    /// Event arguments for when an update is available
    /// </summary>
    public class UpdateAvailableEventArgs : EventArgs
    {
        public UpdateInfo UpdateInfo { get; }

        public UpdateAvailableEventArgs(UpdateInfo updateInfo)
        {
            UpdateInfo = updateInfo;
        }
    }

    /// <summary>
    /// Event arguments for when an update check fails
    /// </summary>
    public class UpdateCheckFailedEventArgs : EventArgs
    {
        public string ErrorMessage { get; }

        public UpdateCheckFailedEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Model for deserializing GitHub release information
    /// </summary>
    internal class GitHubReleaseInfo
    {
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string TagName { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public DateTime PublishedAt { get; set; }
    }
}