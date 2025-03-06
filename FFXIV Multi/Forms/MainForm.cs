using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FFXIV_Multi.Models;
using FFXIVClientManager.Models;
using FFXIVClientManager.Services;
using FFXIVClientManager.Utils;

namespace FFXIVClientManager.Forms
{
    public partial class MainForm : Form
    {
        private readonly ProfileManager _profileManager;
        private readonly LauncherService _launcherService;
        private readonly BackupService _backupService;
        private readonly ProcessMonitor _processMonitor;
        private readonly PluginService _pluginService;
        private readonly UpdateChecker _updateChecker;
        private readonly LogHelper _logHelper;
        private readonly LauncherSettings _settings;

        private List<ClientProfile> _profiles;
        private BindingSource _bindingSource;

        public MainForm()
        {
            InitializeComponent();

            try
            {
                // Initialize settings
                _settings = new LauncherSettings();
                _settings.LoadSettings();

                // Initialize logger
                _logHelper = new LogHelper(_settings.LogPath);

                // Initialize services
                _processMonitor = new ProcessMonitor(_logHelper);
                _profileManager = new ProfileManager(_settings, _logHelper);
                _launcherService = new LauncherService(_settings, _logHelper, _processMonitor);
                _backupService = new BackupService(_settings, _logHelper);
                _pluginService = new PluginService(_settings, _logHelper);
                _updateChecker = new UpdateChecker(_logHelper);

                // Set up binding source
                _bindingSource = new BindingSource();
                dgvProfiles.DataSource = _bindingSource;

                // Subscribe to events
                _profileManager.ProfileChanged += ProfileManager_ProfileChanged;
                _profileManager.ProfileDeleted += ProfileManager_ProfileDeleted;
                _processMonitor.ProcessDetected += ProcessMonitor_ProcessDetected;
                _processMonitor.ProcessExited += ProcessMonitor_ProcessExited;
                _processMonitor.ProcessCrashed += ProcessMonitor_ProcessCrashed;
                _launcherService.ClientLaunched += LauncherService_ClientLaunched;
                _launcherService.LaunchFailed += LauncherService_LaunchFailed;
                _backupService.BackupCompleted += BackupService_BackupCompleted;
                _backupService.BackupFailed += BackupService_BackupFailed;
                _backupService.RestoreCompleted += BackupService_RestoreCompleted;
                _backupService.RestoreFailed += BackupService_RestoreFailed;
                _updateChecker.UpdateAvailable += UpdateChecker_UpdateAvailable;
                _updateChecker.UpdateCheckFailed += UpdateChecker_UpdateCheckFailed;

                // Set up UI components
                SetupDataGrid();

                // Log startup
                _logHelper.LogInfo("Application started");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error initializing application: {ex.Message}\n\n{ex.StackTrace}",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SetupDataGrid()
        {
            dgvProfiles.AutoGenerateColumns = false;

            // Add columns
            var nameColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ProfileName",
                HeaderText = "Profile Name",
                Width = 150
            };

            var characterColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Character.CharacterName",
                HeaderText = "Character",
                Width = 120
            };

            var worldColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Character.WorldName",
                HeaderText = "World",
                Width = 80
            };

            var statusColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Status",
                Width = 100
            };

            dgvProfiles.Columns.Add(nameColumn);
            dgvProfiles.Columns.Add(characterColumn);
            dgvProfiles.Columns.Add(worldColumn);
            dgvProfiles.Columns.Add(statusColumn);

            // Format the Status column
            dgvProfiles.CellFormatting += (sender, e) => {
                if (e.RowIndex >= 0 && e.ColumnIndex == statusColumn.Index)
                {
                    var profile = (ClientProfile)dgvProfiles.Rows[e.RowIndex].DataBoundItem;
                    var processInfo = _processMonitor.GetTrackedProcess(profile.Id);

                    if (processInfo != null && !processInfo.Exited)
                    {
                        e.Value = "Running";
                        e.CellStyle.ForeColor = Color.Green;
                    }
                    else
                    {
                        e.Value = "Stopped";
                        e.CellStyle.ForeColor = Color.Red;
                    }

                    e.FormattingApplied = true;
                }
            };
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Load profiles
            LoadProfiles();

            // Start monitoring
            _processMonitor.StartMonitoring();

            // Check for updates if enabled
            if (_settings.CheckForUpdates)
            {
                CheckForUpdates();
            }

            // Update launch delay from settings
            numLaunchDelay.Value = _settings.DefaultLaunchDelay;

            // Update launcher status
            UpdateLauncherStatus();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_settings.ConfirmOnClose)
            {
                var runningProcesses = _processMonitor.GetTrackedProcesses().Where(p => !p.Exited).Count();

                if (runningProcesses > 0)
                {
                    var result = MessageBox.Show(
                        $"There are {runningProcesses} running FFXIV instances. Are you sure you want to exit?",
                        "Confirm Exit",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            try
            {
                // Stop monitoring
                _processMonitor.StopMonitoring();
                _processMonitor.Dispose();

                // Save settings
                _settings.SaveSettings();

                // Log shutdown
                _logHelper.LogInfo("Application shutdown");
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error during shutdown: {ex.Message}", ex);
            }
        }

        private async void LoadProfiles()
        {
            try
            {
                _profiles = await _profileManager.GetAllProfilesAsync();
                _bindingSource.DataSource = _profiles;

                // Log the count
                _logHelper.LogInfo($"Loaded {_profiles.Count} profiles");

                // Update UI status
                toolStripStatusLabel.Text = $"Loaded {_profiles.Count} profiles";
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error loading profiles: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error loading profiles: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                toolStripStatusLabel.Text = "Error loading profiles";
            }
        }

        private void UpdateLauncherStatus()
        {
            if (string.IsNullOrEmpty(_settings.XIVLauncherPath) || !File.Exists(_settings.XIVLauncherPath))
            {
                lblLauncherStatus.Text = "XIVLauncher Status: Not Configured";
                lblLauncherStatus.ForeColor = Color.Red;
            }
            else
            {
                lblLauncherStatus.Text = $"XIVLauncher Status: {_settings.XIVLauncherPath}";
                lblLauncherStatus.ForeColor = Color.Green;
            }
        }

        private async void CheckForUpdates()
        {
            try
            {
                await _updateChecker.CheckForUpdatesAsync();
                _logHelper.LogInfo("Update check completed");
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error checking for updates: {ex.Message}", ex);
            }
        }

        #region Event Handlers

        private void ProfileManager_ProfileChanged(object sender, ProfileChangedEventArgs e)
        {
            // Refresh profile list
            LoadProfiles();
        }

        private void ProfileManager_ProfileDeleted(object sender, ProfileDeletedEventArgs e)
        {
            // Refresh profile list
            LoadProfiles();

            // Log the event
            _logHelper.LogInfo($"Profile deleted: {e.ProfileName}");
            AppendToLog($"Profile deleted: {e.ProfileName}");
        }

        private void ProcessMonitor_ProcessDetected(object sender, ProcessDetectedEventArgs e)
        {
            // Update UI with detected process
            AppendToLog($"Process detected: {e.ProcessInfo.ProcessId} for profile {e.ProcessInfo.ProfileName}");

            // Refresh the data grid to update status column
            dgvProfiles.Refresh();
        }

        private void ProcessMonitor_ProcessExited(object sender, ProcessExitedEventArgs e)
        {
            // Update UI with exited process
            AppendToLog($"Process exited: {e.ProcessInfo.ProcessId} for profile {e.ProcessInfo.ProfileName}");

            // Refresh the data grid to update status column
            dgvProfiles.Refresh();

            // Create auto-backup if enabled
            if (_settings.AutoBackupAfterExit && e.ProcessInfo.ProfileId != Guid.Empty)
            {
                var profile = _profiles.FirstOrDefault(p => p.Id == e.ProcessInfo.ProfileId);
                if (profile != null && profile.AutoBackup)
                {
                    CreateBackup(profile);
                }
            }

            // Update play time
            if (e.ProcessInfo.ProfileId != Guid.Empty)
            {
                var runTimeMinutes = (long)e.ProcessInfo.RunTime.TotalMinutes;
                if (runTimeMinutes > 0)
                {
                    try
                    {
                        _profileManager.UpdatePlayTimeAsync(e.ProcessInfo.ProfileId, runTimeMinutes);
                        AppendToLog($"Updated play time for {e.ProcessInfo.ProfileName}: +{runTimeMinutes} minutes");
                    }
                    catch (Exception ex)
                    {
                        _logHelper.LogError($"Error updating play time: {ex.Message}", ex);
                    }
                }
            }
        }

        private void ProcessMonitor_ProcessCrashed(object sender, ProcessCrashedEventArgs e)
        {
            // Show notification about crashed process
            AppendToLog($"Process crashed: {e.ProcessInfo.ProcessId} for profile {e.ProcessInfo.ProfileName}");

            // Refresh the data grid to update status column
            dgvProfiles.Refresh();

            MessageBox.Show(
                $"Process for profile '{e.ProcessInfo.ProfileName}' crashed with exit code {e.ProcessInfo.ExitCode}.",
                "Process Crashed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void LauncherService_ClientLaunched(object sender, ClientLaunchedEventArgs e)
        {
            // Update UI with launched client
            AppendToLog($"Client launched: {e.Profile.ProfileName}");

            // Refresh the data grid to update status column
            dgvProfiles.Refresh();
        }

        private void LauncherService_LaunchFailed(object sender, LaunchFailedEventArgs e)
        {
            // Show error message
            AppendToLog($"Launch failed: {e.Profile.ProfileName} - {e.ErrorMessage}");

            MessageBox.Show(
                $"Failed to launch client for profile '{e.Profile.ProfileName}': {e.ErrorMessage}",
                "Launch Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void BackupService_BackupCompleted(object sender, BackupCompletedEventArgs e)
        {
            // Show backup completed notification
            AppendToLog($"Backup completed for profile: {e.Profile.ProfileName}");
        }

        private void BackupService_BackupFailed(object sender, BackupFailedEventArgs e)
        {
            // Show backup failed notification
            AppendToLog($"Backup failed for profile: {e.Profile.ProfileName} - {e.ErrorMessage}");

            MessageBox.Show(
                $"Backup failed for profile '{e.Profile.ProfileName}': {e.ErrorMessage}",
                "Backup Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void BackupService_RestoreCompleted(object sender, RestoreCompletedEventArgs e)
        {
            // Show restore completed notification
            AppendToLog($"Restore completed for profile: {e.Profile.ProfileName}");
        }

        private void BackupService_RestoreFailed(object sender, RestoreFailedEventArgs e)
        {
            // Show restore failed notification
            AppendToLog($"Restore failed for profile: {e.Profile.ProfileName} - {e.ErrorMessage}");

            MessageBox.Show(
                $"Restore failed for profile '{e.Profile.ProfileName}': {e.ErrorMessage}",
                "Restore Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void UpdateChecker_UpdateAvailable(object sender, UpdateAvailableEventArgs e)
        {
            // Show update available notification
            AppendToLog($"Update available: {e.UpdateInfo.LatestVersion} (current: {e.UpdateInfo.CurrentVersion})");

            var result = MessageBox.Show(
                $"A new version ({e.UpdateInfo.LatestVersion}) is available. Would you like to download it now?",
                "Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                // Open download page
                _updateChecker.OpenDownloadPage(e.UpdateInfo.ReleaseUrl);
            }
        }

        private void UpdateChecker_UpdateCheckFailed(object sender, UpdateCheckFailedEventArgs e)
        {
            // Log the error
            _logHelper.LogError($"Update check failed: {e.ErrorMessage}");
            AppendToLog($"Update check failed: {e.ErrorMessage}");
        }

        #endregion

        #region UI Event Handlers

        private async void btnAddProfile_Click(object sender, EventArgs e)
        {
            // Show profile creation form
            using (var form = new ProfileForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Generate unique paths if needed
                        if (!_profileManager.ValidateProfilePaths(form.Profile))
                        {
                            _profileManager.GenerateUniquePaths(form.Profile);
                        }

                        // Save the profile
                        await _profileManager.SaveProfileAsync(form.Profile);

                        // Refresh profiles
                        LoadProfiles();

                        // Log the event
                        _logHelper.LogInfo($"Created new profile: {form.Profile.ProfileName}");
                        AppendToLog($"Created new profile: {form.Profile.ProfileName}");
                    }
                    catch (Exception ex)
                    {
                        _logHelper.LogError($"Error creating profile: {ex.Message}", ex);
                        MessageBox.Show(
                            $"Error creating profile: {ex.Message}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnLaunchSelected_Click(object sender, EventArgs e)
        {
            // Launch selected profile
            if (dgvProfiles.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a profile to launch.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var profile = (ClientProfile)dgvProfiles.SelectedRows[0].DataBoundItem;

            // Check if XIVLauncher is configured
            if (string.IsNullOrEmpty(_settings.XIVLauncherPath) || !File.Exists(_settings.XIVLauncherPath))
            {
                MessageBox.Show(
                    "XIVLauncher path is not configured. Please set it in the Settings dialog.",
                    "Launch Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Check if another instance is already running
            if (_settings.CheckForRunningInstances)
            {
                var processInfo = _processMonitor.GetTrackedProcess(profile.Id);
                if (processInfo != null && !processInfo.Exited)
                {
                    var result = MessageBox.Show(
                        $"Profile '{profile.ProfileName}' is already running. Launch another instance?",
                        "Profile Already Running",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.No)
                    {
                        return;
                    }
                }
            }

            // Check if auto-backup is enabled
            if (_settings.AutoBackupBeforeLaunch && profile.AutoBackup)
            {
                try
                {
                    AppendToLog($"Creating backup for profile: {profile.ProfileName} before launch...");
                    await _backupService.CreateBackupAsync(profile);
                }
                catch (Exception ex)
                {
                    _logHelper.LogError($"Error creating backup before launch: {ex.Message}", ex);
                    AppendToLog($"Error creating backup before launch: {ex.Message}");
                }
            }

            try
            {
                AppendToLog($"Launching profile: {profile.ProfileName}...");
                await _launcherService.LaunchClientAsync(profile);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error launching client: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error launching client: {ex.Message}",
                    "Launch Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void btnLaunchAll_Click(object sender, EventArgs e)
        {
            // Launch all profiles
            if (_profiles.Count == 0)
            {
                MessageBox.Show("No profiles available to launch.", "No Profiles", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if XIVLauncher is configured
            if (string.IsNullOrEmpty(_settings.XIVLauncherPath) || !File.Exists(_settings.XIVLauncherPath))
            {
                MessageBox.Show(
                    "XIVLauncher path is not configured. Please set it in the Settings dialog.",
                    "Launch Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show(
                $"Are you sure you want to launch all {_profiles.Count} profiles?\n\nProfiles will be launched with a {numLaunchDelay.Value} second delay between them.",
                "Confirm Launch All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // Set launch delay
                _launcherService.SetLaunchDelay((int)numLaunchDelay.Value);

                // Create backups if enabled
                if (_settings.AutoBackupBeforeLaunch)
                {
                    var backupProfiles = _profiles.Where(p => p.AutoBackup).ToList();
                    if (backupProfiles.Count > 0)
                    {
                        AppendToLog($"Creating backups for {backupProfiles.Count} profiles before launch...");
                        await _backupService.BackupAllProfilesAsync(backupProfiles);
                    }
                }

                // Launch all profiles
                AppendToLog($"Launching all profiles with {numLaunchDelay.Value} second delay...");
                int count = await _launcherService.LaunchMultipleClientsAsync(_profiles.ToArray());

                AppendToLog($"Launched {count} of {_profiles.Count} profiles");
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error launching clients: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error launching clients: {ex.Message}",
                    "Launch Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            // Refresh profile list
            LoadProfiles();

            // Refresh data grid to update status column
            dgvProfiles.Refresh();

            AppendToLog("Refreshed profiles");
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            // Show settings form
            using (var form = new SettingsForm(_settings))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Save settings
                    _settings.SaveSettings();

                    // Update UI
                    UpdateLauncherStatus();

                    // Update launch delay
                    numLaunchDelay.Value = _settings.DefaultLaunchDelay;

                    // Log the event
                    _logHelper.LogInfo("Settings updated");
                    AppendToLog("Settings updated");
                }
            }
        }

        private void btnBackupManager_Click(object sender, EventArgs e)
        {
            // Show backup manager form
            using (var form = new BackupManagerForm(_profiles, _backupService, _logHelper))
            {
                form.ShowDialog();
            }
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            // Clear log display
            txtLog.Clear();
        }

        private void OnEditProfileClick(object sender, EventArgs e)
        {
            // Edit selected profile
            if (dgvProfiles.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a profile to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var profile = (ClientProfile)dgvProfiles.SelectedRows[0].DataBoundItem;

            using (var form = new ProfileForm(profile))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Save the profile
                        _profileManager.SaveProfileAsync(form.Profile);

                        // Refresh profiles
                        LoadProfiles();

                        // Log the event
                        _logHelper.LogInfo($"Updated profile: {form.Profile.ProfileName}");
                        AppendToLog($"Updated profile: {form.Profile.ProfileName}");
                    }
                    catch (Exception ex)
                    {
                        _logHelper.LogError($"Error updating profile: {ex.Message}", ex);
                        MessageBox.Show(
                            $"Error updating profile: {ex.Message}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void OnDeleteProfileClick(object sender, EventArgs e)
        {
            // Delete selected profile
            if (dgvProfiles.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a profile to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var profile = (ClientProfile)dgvProfiles.SelectedRows[0].DataBoundItem;

            // Check if profile is running
            var processInfo = _processMonitor.GetTrackedProcess(profile.Id);
            if (processInfo != null && !processInfo.Exited)
            {
                MessageBox.Show(
                    $"Profile '{profile.ProfileName}' is currently running. Please stop it before deleting.",
                    "Profile Running",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Are you sure you want to delete the profile '{profile.ProfileName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // Delete the profile
                await _profileManager.DeleteProfileAsync(profile.Id);

                // Refresh profiles
                LoadProfiles();
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error deleting profile: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error deleting profile: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void OnCloneProfileClick(object sender, EventArgs e)
        {
            // Clone selected profile
            if (dgvProfiles.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a profile to clone.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var profile = (ClientProfile)dgvProfiles.SelectedRows[0].DataBoundItem;
            var clonedProfile = profile.Clone();

            try
            {
                // Generate unique paths
                _profileManager.GenerateUniquePaths(clonedProfile);

                // Save the cloned profile
                await _profileManager.SaveProfileAsync(clonedProfile);

                // Refresh profiles
                LoadProfiles();

                // Log the event
                _logHelper.LogInfo($"Cloned profile: {profile.ProfileName} -> {clonedProfile.ProfileName}");
                AppendToLog($"Cloned profile: {profile.ProfileName} -> {clonedProfile.ProfileName}");
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error cloning profile: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error cloning profile: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Exit application
            Close();
        }

        private async void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check for updates
            AppendToLog("Checking for updates...");

            try
            {
                var updateInfo = await _updateChecker.CheckForUpdatesAsync();

                if (updateInfo != null && !updateInfo.IsUpdateAvailable)
                {
                    MessageBox.Show(
                        $"You are running the latest version ({updateInfo.CurrentVersion}).",
                        "No Updates Available",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error checking for updates: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error checking for updates: {ex.Message}",
                    "Update Check Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show about form
            using (var form = new AboutForm())
            {
                form.ShowDialog();
            }
        }

        private void viewLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show logs
            string logFilePath = _logHelper.GetLogFilePath();

            if (File.Exists(logFilePath))
            {
                try
                {
                    Process.Start(logFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error opening log file: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show(
                    "Log file not found.",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void terminateSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Terminate selected process
            if (dgvProfiles.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a profile to terminate.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var profile = (ClientProfile)dgvProfiles.SelectedRows[0].DataBoundItem;
            var processInfo = _processMonitor.GetTrackedProcess(profile.Id);

            if (processInfo == null || processInfo.Exited)
            {
                MessageBox.Show(
                    $"No running process found for profile '{profile.ProfileName}'.",
                    "Process Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Are you sure you want to terminate the process for profile '{profile.ProfileName}'?",
                "Confirm Terminate",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // Terminate the process
                processInfo.Process.Kill();

                // Log the event
                _logHelper.LogInfo($"Terminated process for profile: {profile.ProfileName}");
                AppendToLog($"Terminated process for profile: {profile.ProfileName}");
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error terminating process: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error terminating process: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void terminateAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Terminate all processes
            var runningProcesses = _processMonitor.GetTrackedProcesses().Where(p => !p.Exited).ToList();

            if (runningProcesses.Count == 0)
            {
                MessageBox.Show(
                    "No running processes found.",
                    "No Processes",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                $"Are you sure you want to terminate all {runningProcesses.Count} running processes?",
                "Confirm Terminate All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var processInfo in runningProcesses)
            {
                try
                {
                    processInfo.Process.Kill();
                    successCount++;

                    // Log the event
                    _logHelper.LogInfo($"Terminated process for profile: {processInfo.ProfileName}");
                    AppendToLog($"Terminated process for profile: {processInfo.ProfileName}");
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logHelper.LogError($"Error terminating process for {processInfo.ProfileName}: {ex.Message}", ex);
                    AppendToLog($"Error terminating process for {processInfo.ProfileName}: {ex.Message}");
                }
            }

            if (failCount > 0)
            {
                MessageBox.Show(
                    $"Terminated {successCount} processes, failed to terminate {failCount} processes.",
                    "Terminate Results",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    $"Successfully terminated {successCount} processes.",
                    "Terminate Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            // Refresh data grid to update status column
            dgvProfiles.Refresh();
        }

        #endregion

        private async void CreateBackup(ClientProfile profile)
        {
            try
            {
                AppendToLog($"Creating backup for profile: {profile.ProfileName}...");
                string backupPath = await _backupService.CreateBackupAsync(profile);
                AppendToLog($"Backup created: {Path.GetFileName(backupPath)}");
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error creating backup: {ex.Message}", ex);
                AppendToLog($"Error creating backup: {ex.Message}");
            }
        }

        private void AppendToLog(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(AppendToLog), message);
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            txtLog.AppendText($"[{timestamp}] {message}{Environment.NewLine}");

            // Scroll to bottom
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
    }
}