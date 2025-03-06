using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FFXIV_Multi.Models;
using FFXIVClientManager.Models;
using FFXIVClientManager.Utils;

namespace FFXIVClientManager.Services
{
    /// <summary>
    /// Service for launching and managing FFXIV client instances
    /// </summary>
    public class LauncherService
    {
        private readonly LauncherSettings _settings;
        private readonly LogHelper _logHelper;
        private readonly ProcessMonitor _processMonitor;
        private int _launchDelay;

        public event EventHandler<ClientLaunchedEventArgs> ClientLaunched;
        public event EventHandler<LaunchFailedEventArgs> LaunchFailed;

        /// <summary>
        /// Initializes a new instance of the LauncherService
        /// </summary>
        public LauncherService(LauncherSettings settings, LogHelper logHelper, ProcessMonitor processMonitor)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logHelper = logHelper ?? throw new ArgumentNullException(nameof(logHelper));
            _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
            _launchDelay = _settings.DefaultLaunchDelay;
        }

        /// <summary>
        /// Launches a client using the specified profile
        /// </summary>
        public async Task<Process> LaunchClientAsync(ClientProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (string.IsNullOrEmpty(_settings.XIVLauncherPath) || !File.Exists(_settings.XIVLauncherPath))
            {
                string error = "XIVLauncher path is not set or invalid";
                _logHelper.LogError(error);
                OnLaunchFailed(profile, error);
                throw new InvalidOperationException(error);
            }

            try
            {
                _logHelper.LogInfo($"Launching client for profile: {profile.ProfileName}");

                // Create arguments for XIVLauncher
                var args = BuildLaunchArguments(profile);

                // Create process start info
                var startInfo = new ProcessStartInfo
                {
                    FileName = _settings.XIVLauncherPath,
                    Arguments = args,
                    UseShellExecute = true
                };

                // Launch the process
                var process = Process.Start(startInfo);

                if (process != null)
                {
                    _logHelper.LogInfo($"Successfully launched client with PID {process.Id}");

                    // Start tracking the process
                    _processMonitor.TrackProcess(profile, process);

                    // Update profile last used time
                    profile.LastUsed = DateTime.Now;

                    // Notify subscribers
                    OnClientLaunched(profile, process);

                    return process;
                }
                else
                {
                    string error = "Failed to start process";
                    _logHelper.LogError(error);
                    OnLaunchFailed(profile, error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error launching client: {ex.Message}", ex);
                OnLaunchFailed(profile, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Builds command line arguments for XIVLauncher
        /// </summary>
        private string BuildLaunchArguments(ClientProfile profile)
        {
            var args = new System.Text.StringBuilder();

            // Add config path argument if specified
            if (!string.IsNullOrEmpty(profile.ConfigPath))
            {
                args.Append($"--roamingPath=\"{profile.ConfigPath}\" ");
            }

            // Add Dalamud plugin path if specified and enabled
            if (profile.EnableDalamud && !string.IsNullOrEmpty(profile.PluginPath))
            {
                args.Append($"--dalamudPath=\"{profile.PluginPath}\" ");
            }

            // Add DX11 argument if enabled
            if (profile.ForceDX11)
            {
                args.Append("--dx11 ");
            }

            // Add any additional arguments
            if (!string.IsNullOrEmpty(profile.AdditionalLaunchArgs))
            {
                args.Append(profile.AdditionalLaunchArgs);
            }

            return args.ToString().Trim();
        }

        /// <summary>
        /// Sets the delay between launching multiple clients
        /// </summary>
        public void SetLaunchDelay(int seconds)
        {
            _launchDelay = Math.Max(0, seconds);
        }

        /// <summary>
        /// Gets the current launch delay in seconds
        /// </summary>
        public int GetLaunchDelay()
        {
            return _launchDelay;
        }

        /// <summary>
        /// Launches multiple clients with a delay between each
        /// </summary>
        public async Task<int> LaunchMultipleClientsAsync(ClientProfile[] profiles)
        {
            if (profiles == null || profiles.Length == 0)
                return 0;

            int successCount = 0;

            for (int i = 0; i < profiles.Length; i++)
            {
                try
                {
                    var process = await LaunchClientAsync(profiles[i]);

                    if (process != null)
                    {
                        successCount++;
                    }

                    // Wait for the delay period before launching the next client
                    // Skip the delay for the last profile
                    if (i < profiles.Length - 1 && _launchDelay > 0)
                    {
                        await Task.Delay(_launchDelay * 1000);
                    }
                }
                catch (Exception ex)
                {
                    _logHelper.LogError($"Error launching client {profiles[i].ProfileName}: {ex.Message}", ex);
                    // Continue with the next profile
                }
            }

            return successCount;
        }

        #region Event Handlers

        protected virtual void OnClientLaunched(ClientProfile profile, Process process)
        {
            ClientLaunched?.Invoke(this, new ClientLaunchedEventArgs(profile, process));
        }

        protected virtual void OnLaunchFailed(ClientProfile profile, string errorMessage)
        {
            LaunchFailed?.Invoke(this, new LaunchFailedEventArgs(profile, errorMessage));
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for when a client is launched
    /// </summary>
    public class ClientLaunchedEventArgs : EventArgs
    {
        public ClientProfile Profile { get; }
        public Process Process { get; }

        public ClientLaunchedEventArgs(ClientProfile profile, Process process)
        {
            Profile = profile;
            Process = process;
        }
    }

    /// <summary>
    /// Event arguments for when a launch fails
    /// </summary>
    public class LaunchFailedEventArgs : EventArgs
    {
        public ClientProfile Profile { get; }
        public string ErrorMessage { get; }

        public LaunchFailedEventArgs(ClientProfile profile, string errorMessage)
        {
            Profile = profile;
            ErrorMessage = errorMessage;
        }
    }
}