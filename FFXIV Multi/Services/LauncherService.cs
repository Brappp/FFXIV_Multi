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
    /// Service for launching and managing FFXIV client instances.
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
        /// Initializes a new instance of the LauncherService.
        /// </summary>
        public LauncherService(LauncherSettings settings, LogHelper logHelper, ProcessMonitor processMonitor)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logHelper = logHelper ?? throw new ArgumentNullException(nameof(logHelper));
            _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
            _launchDelay = _settings.DefaultLaunchDelay;
        }

        /// <summary>
        /// Launches a client using the specified profile.
        /// </summary>
        public async Task<Process> LaunchClientAsync(ClientProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            // Ensure the XIVLauncher path is configured and valid
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

                // Build command-line arguments for XIVLauncher
                string args = BuildLaunchArguments(profile);

                // Create process start info
                var startInfo = new ProcessStartInfo
                {
                    FileName = _settings.XIVLauncherPath,
                    Arguments = args,
                    UseShellExecute = true
                };

                // Launch the process
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    _logHelper.LogInfo($"Successfully launched client with PID {process.Id}");

                    // Track the process and update profile usage
                    _processMonitor.TrackProcess(profile, process);
                    profile.LastUsed = DateTime.Now;

                    // Notify subscribers of successful launch
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
        /// Builds the command-line arguments for XIVLauncher based on the profile settings.
        /// </summary>
        private string BuildLaunchArguments(ClientProfile profile)
        {
            var args = new System.Text.StringBuilder();

            // Config path argument
            if (!string.IsNullOrEmpty(profile.ConfigPath))
            {
                args.Append($"--roamingPath=\"{profile.ConfigPath}\" ");
            }
            // Dalamud plugin path argument
            if (profile.EnableDalamud && !string.IsNullOrEmpty(profile.PluginPath))
            {
                args.Append($"--dalamudPath=\"{profile.PluginPath}\" ");
            }
            // Steam argument
            if (profile.IsSteam)
            {
                args.Append("--steam ");
            }
            // DirectX11 mode argument
            if (profile.ForceDX11)
            {
                args.Append("--dx11 ");
            }
            // No auto-login argument
            if (profile.NoAutoLogin)
            {
                args.Append("--noautologin ");
            }
            // OTP (One-Time Password) argument
            if (profile.UseOTP)
            {
                args.Append("--otp ");
            }
            // Additional custom arguments
            if (!string.IsNullOrEmpty(profile.AdditionalArgs))
            {
                args.Append(profile.AdditionalArgs);
            }

            return args.ToString().Trim();
        }

        /// <summary>
        /// Sets the delay (in seconds) between launching multiple clients.
        /// </summary>
        public void SetLaunchDelay(int seconds)
        {
            _launchDelay = Math.Max(0, seconds);
        }

        /// <summary>
        /// Gets the current launch delay (in seconds).
        /// </summary>
        public int GetLaunchDelay()
        {
            return _launchDelay;
        }

        /// <summary>
        /// Launches multiple clients sequentially with a delay between each launch.
        /// Returns the count of successfully launched clients.
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
                    Process proc = await LaunchClientAsync(profiles[i]);
                    if (proc != null)
                        successCount++;

                    // Wait for the specified delay before launching the next client (if any)
                    if (i < profiles.Length - 1 && _launchDelay > 0)
                        await Task.Delay(_launchDelay * 1000);
                }
                catch (Exception ex)
                {
                    _logHelper.LogError($"Error launching client {profiles[i].ProfileName}: {ex.Message}", ex);
                    // Continue launching the next profile even if one fails
                }
            }
            return successCount;
        }

        #region Event Triggers

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
    /// Event arguments for when a client is successfully launched.
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
    /// Event arguments for when a client launch fails.
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
