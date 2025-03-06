using System;
using System.IO;
using System.Windows.Forms;
using FFXIVClientManager.Models;
using Microsoft.Win32;

namespace FFXIVClientManager.Forms
{
    public partial class SettingsForm : Form
    {
        private readonly LauncherSettings _settings;

        public SettingsForm(LauncherSettings settings)
        {
            InitializeComponent();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            LoadSettings();
        }

        private void LoadSettings()
        {
            // General settings
            txtLauncherPath.Text = _settings.XIVLauncherPath;
            txtBackupPath.Text = _settings.BackupPath;
            txtLogPath.Text = _settings.LogPath;
            numLaunchDelay.Value = _settings.DefaultLaunchDelay;
            numMaxBackups.Value = _settings.MaxBackupsPerProfile;

            // Backup settings
            chkBackupPlugins.Checked = _settings.BackupPlugins;
            chkRestorePlugins.Checked = _settings.RestorePlugins;

            // UI settings
            chkMinimizeToTray.Checked = _settings.MinimizeToTray;
            chkConfirmOnClose.Checked = _settings.ConfirmOnClose;
            chkShowTooltips.Checked = _settings.ShowTooltips;

            // Auto settings
            chkAutoBackupBeforeLaunch.Checked = _settings.AutoBackupBeforeLaunch;
            chkAutoBackupAfterExit.Checked = _settings.AutoBackupAfterExit;
            chkCheckForUpdates.Checked = _settings.CheckForUpdates;
            chkStartWithWindows.Checked = _settings.StartWithWindows;
            chkCheckForRunningInstances.Checked = _settings.CheckForRunningInstances;

            // Theme
            switch (_settings.Theme)
            {
                case "Light":
                    rbLight.Checked = true;
                    break;
                case "Dark":
                    rbDark.Checked = true;
                    break;
                default:
                    rbSystem.Checked = true;
                    break;
            }
        }

        private void SaveSettings()
        {
            // General settings
            _settings.XIVLauncherPath = txtLauncherPath.Text.Trim();
            _settings.BackupPath = txtBackupPath.Text.Trim();
            _settings.LogPath = txtLogPath.Text.Trim();
            _settings.DefaultLaunchDelay = (int)numLaunchDelay.Value;
            _settings.MaxBackupsPerProfile = (int)numMaxBackups.Value;

            // Backup settings
            _settings.BackupPlugins = chkBackupPlugins.Checked;
            _settings.RestorePlugins = chkRestorePlugins.Checked;

            // UI settings
            _settings.MinimizeToTray = chkMinimizeToTray.Checked;
            _settings.ConfirmOnClose = chkConfirmOnClose.Checked;
            _settings.ShowTooltips = chkShowTooltips.Checked;

            // Auto settings
            _settings.AutoBackupBeforeLaunch = chkAutoBackupBeforeLaunch.Checked;
            _settings.AutoBackupAfterExit = chkAutoBackupAfterExit.Checked;
            _settings.CheckForUpdates = chkCheckForUpdates.Checked;
            _settings.StartWithWindows = chkStartWithWindows.Checked;
            _settings.CheckForRunningInstances = chkCheckForRunningInstances.Checked;

            // Theme
            if (rbLight.Checked)
                _settings.Theme = "Light";
            else if (rbDark.Checked)
                _settings.Theme = "Dark";
            else
                _settings.Theme = "System";

            // Handle start with Windows setting
            UpdateStartupRegistry();
        }

        private void UpdateStartupRegistry()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (_settings.StartWithWindows)
                {
                    string appPath = Application.ExecutablePath;
                    rk.SetValue("FFXIVClientManager", appPath);
                }
                else
                {
                    if (rk.GetValue("FFXIVClientManager") != null)
                        rk.DeleteValue("FFXIVClientManager", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to update startup settings: {ex.Message}",
                    "Registry Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void btnBrowseLauncher_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select XIVLauncher Executable";
                dialog.Filter = "Executable Files (*.exe)|*.exe";
                dialog.FileName = "XIVLauncher.exe";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtLauncherPath.Text = dialog.FileName;
                }
            }
        }

        private void btnBrowseBackup_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Backup Directory";
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(txtBackupPath.Text) && Directory.Exists(txtBackupPath.Text))
                    dialog.SelectedPath = txtBackupPath.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtBackupPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnBrowseLog_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Log Directory";
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(txtLogPath.Text) && Directory.Exists(txtLogPath.Text))
                    dialog.SelectedPath = txtLogPath.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtLogPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Validate paths
            if (string.IsNullOrWhiteSpace(txtLauncherPath.Text) || !File.Exists(txtLauncherPath.Text))
            {
                MessageBox.Show(
                    "XIVLauncher path is invalid. Please select a valid executable.",
                    "Invalid Path",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtLauncherPath.Focus();
                return;
            }

            SaveSettings();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnDetectLauncher_Click(object sender, EventArgs e)
        {
            // Try common installation paths
            string[] commonPaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "XIVLauncher", "XIVLauncher.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "XIVLauncher", "XIVLauncher.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XIVLauncher", "XIVLauncher.exe"),
                @"C:\XIVLauncher\XIVLauncher.exe"
            };

            foreach (string path in commonPaths)
            {
                if (File.Exists(path))
                {
                    txtLauncherPath.Text = path;
                    MessageBox.Show(
                        $"XIVLauncher detected at: {path}",
                        "Launcher Detected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
            }

            // If we get here, no launcher was found
            MessageBox.Show(
                "XIVLauncher could not be automatically detected. Please browse to the location manually.",
                "Launcher Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void btnResetDefaults_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Are you sure you want to reset all settings to default values?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _settings.ResetToDefaults();
                LoadSettings();
            }
        }

        #region Form Designer Code
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.btnDetectLauncher = new System.Windows.Forms.Button();
            this.numMaxBackups = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.numLaunchDelay = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.btnBrowseLog = new System.Windows.Forms.Button();
            this.txtLogPath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnBrowseBackup = new System.Windows.Forms.Button();
            this.txtBackupPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnBrowseLauncher = new System.Windows.Forms.Button();
            this.txtLauncherPath = new System.Windows.Forms.TextBox();
            this.lblLauncherPath = new System.Windows.Forms.Label();
            this.tabBackup = new System.Windows.Forms.TabPage();
            this.chkRestorePlugins = new System.Windows.Forms.CheckBox();
            this.chkBackupPlugins = new System.Windows.Forms.CheckBox();
            this.tabInterface = new System.Windows.Forms.TabPage();
            this.grpTheme = new System.Windows.Forms.GroupBox();
            this.rbDark = new System.Windows.Forms.RadioButton();
            this.rbLight = new System.Windows.Forms.RadioButton();
            this.rbSystem = new System.Windows.Forms.RadioButton();
            this.chkShowTooltips = new System.Windows.Forms.CheckBox();
            this.chkConfirmOnClose = new System.Windows.Forms.CheckBox();
            this.chkMinimizeToTray = new System.Windows.Forms.CheckBox();
            this.tabAdvanced = new System.Windows.Forms.TabPage();
            this.chkCheckForRunningInstances = new System.Windows.Forms.CheckBox();
            this.chkStartWithWindows = new System.Windows.Forms.CheckBox();
            this.chkCheckForUpdates = new System.Windows.Forms.CheckBox();
            this.chkAutoBackupAfterExit = new System.Windows.Forms.CheckBox();
            this.chkAutoBackupBeforeLaunch = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnResetDefaults = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxBackups)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLaunchDelay)).BeginInit();
            this.tabBackup.SuspendLayout();
            this.tabInterface.SuspendLayout();
            this.grpTheme.SuspendLayout();
            this.tabAdvanced.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabGeneral);
            this.tabControl.Controls.Add(this.tabBackup);
            this.tabControl.Controls.Add(this.tabInterface);
            this.tabControl.Controls.Add(this.tabAdvanced);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(460, 307);
            this.tabControl.TabIndex = 0;
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add(this.btnDetectLauncher);
            this.tabGeneral.Controls.Add(this.numMaxBackups);
            this.tabGeneral.Controls.Add(this.label4);
            this.tabGeneral.Controls.Add(this.numLaunchDelay);
            this.tabGeneral.Controls.Add(this.label3);
            this.tabGeneral.Controls.Add(this.btnBrowseLog);
            this.tabGeneral.Controls.Add(this.txtLogPath);
            this.tabGeneral.Controls.Add(this.label2);
            this.tabGeneral.Controls.Add(this.btnBrowseBackup);
            this.tabGeneral.Controls.Add(this.txtBackupPath);
            this.tabGeneral.Controls.Add(this.label1);
            this.tabGeneral.Controls.Add(this.btnBrowseLauncher);
            this.tabGeneral.Controls.Add(this.txtLauncherPath);
            this.tabGeneral.Controls.Add(this.lblLauncherPath);
            this.tabGeneral.Location = new System.Drawing.Point(4, 22);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabGeneral.Size = new System.Drawing.Size(452, 281);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            this.tabGeneral.UseVisualStyleBackColor = true;
            // 
            // btnDetectLauncher
            // 
            this.btnDetectLauncher.Location = new System.Drawing.Point(136, 58);
            this.btnDetectLauncher.Name = "btnDetectLauncher";
            this.btnDetectLauncher.Size = new System.Drawing.Size(75, 23);
            this.btnDetectLauncher.TabIndex = 13;
            this.btnDetectLauncher.Text = "Auto Detect";
            this.btnDetectLauncher.UseVisualStyleBackColor = true;
            this.btnDetectLauncher.Click += new System.EventHandler(this.btnDetectLauncher_Click);
            // 
            // numMaxBackups
            // 
            this.numMaxBackups.Location = new System.Drawing.Point(136, 193);
            this.numMaxBackups.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numMaxBackups.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMaxBackups.Name = "numMaxBackups";
            this.numMaxBackups.Size = new System.Drawing.Size(75, 20);
            this.numMaxBackups.TabIndex = 12;
            this.numMaxBackups.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 195);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(124, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Max Backups Per Profile:";
            // 
            // numLaunchDelay
            // 
            this.numLaunchDelay.Location = new System.Drawing.Point(136, 167);
            this.numLaunchDelay.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numLaunchDelay.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numLaunchDelay.Name = "numLaunchDelay";
            this.numLaunchDelay.Size = new System.Drawing.Size(75, 20);
            this.numLaunchDelay.TabIndex = 10;
            this.numLaunchDelay.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 169);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(124, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Default Launch Delay (s):";
            // 
            // btnBrowseLog
            // 
            this.btnBrowseLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseLog.Location = new System.Drawing.Point(371, 138);
            this.btnBrowseLog.Name = "btnBrowseLog";
            this.btnBrowseLog.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseLog.TabIndex = 8;
            this.btnBrowseLog.Text = "Browse...";
            this.btnBrowseLog.UseVisualStyleBackColor = true;
            this.btnBrowseLog.Click += new System.EventHandler(this.btnBrowseLog_Click);
            // 
            // txtLogPath
            // 
            this.txtLogPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLogPath.Location = new System.Drawing.Point(9, 138);
            this.txtLogPath.Name = "txtLogPath";
            this.txtLogPath.Size = new System.Drawing.Size(356, 20);
            this.txtLogPath.TabIndex = 7;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 122);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Log Path:";
            // 
            // btnBrowseBackup
            // 
            this.btnBrowseBackup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseBackup.Location = new System.Drawing.Point(371, 94);
            this.btnBrowseBackup.Name = "btnBrowseBackup";
            this.btnBrowseBackup.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseBackup.TabIndex = 5;
            this.btnBrowseBackup.Text = "Browse...";
            this.btnBrowseBackup.UseVisualStyleBackColor = true;
            this.btnBrowseBackup.Click += new System.EventHandler(this.btnBrowseBackup_Click);
            // 
            // txtBackupPath
            // 
            this.txtBackupPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBackupPath.Location = new System.Drawing.Point(9, 94);
            this.txtBackupPath.Name = "txtBackupPath";
            this.txtBackupPath.Size = new System.Drawing.Size(356, 20);
            this.txtBackupPath.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 78);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Backup Path:";
            // 
            // btnBrowseLauncher
            // 
            this.btnBrowseLauncher.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseLauncher.Location = new System.Drawing.Point(371, 32);
            this.btnBrowseLauncher.Name = "btnBrowseLauncher";
            this.btnBrowseLauncher.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseLauncher.TabIndex = 2;
            this.btnBrowseLauncher.Text = "Browse...";
            this.btnBrowseLauncher.UseVisualStyleBackColor = true;
            this.btnBrowseLauncher.Click += new System.EventHandler(this.btnBrowseLauncher_Click);
            // 
            // txtLauncherPath
            // 
            this.txtLauncherPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLauncherPath.Location = new System.Drawing.Point(9, 32);
            this.txtLauncherPath.Name = "txtLauncherPath";
            this.txtLauncherPath.Size = new System.Drawing.Size(356, 20);
            this.txtLauncherPath.TabIndex = 1;
            // 
            // lblLauncherPath
            // 
            this.lblLauncherPath.AutoSize = true;
            this.lblLauncherPath.Location = new System.Drawing.Point(6, 16);
            this.lblLauncherPath.Name = "lblLauncherPath";
            this.lblLauncherPath.Size = new System.Drawing.Size(98, 13);
            this.lblLauncherPath.TabIndex = 0;
            this.lblLauncherPath.Text = "XIVLauncher Path:";
            // 
            // tabBackup
            // 
            this.tabBackup.Controls.Add(this.chkRestorePlugins);
            this.tabBackup.Controls.Add(this.chkBackupPlugins);
            this.tabBackup.Location = new System.Drawing.Point(4, 22);
            this.tabBackup.Name = "tabBackup";
            this.tabBackup.Padding = new System.Windows.Forms.Padding(3);
            this.tabBackup.Size = new System.Drawing.Size(452, 281);
            this.tabBackup.TabIndex = 1;
            this.tabBackup.Text = "Backup";
            this.tabBackup.UseVisualStyleBackColor = true;
            // 
            // chkRestorePlugins
            // 
            this.chkRestorePlugins.AutoSize = true;
            this.chkRestorePlugins.Location = new System.Drawing.Point(18, 51);
            this.chkRestorePlugins.Name = "chkRestorePlugins";
            this.chkRestorePlugins.Size = new System.Drawing.Size(246, 17);
            this.chkRestorePlugins.TabIndex = 1;
            this.chkRestorePlugins.Text = "Restore plugins when restoring profile from backup";
            this.chkRestorePlugins.UseVisualStyleBackColor = true;
            // 
            // chkBackupPlugins
            // 
            this.chkBackupPlugins.AutoSize = true;
            this.chkBackupPlugins.Location = new System.Drawing.Point(18, 28);
            this.chkBackupPlugins.Name = "chkBackupPlugins";
            this.chkBackupPlugins.Size = new System.Drawing.Size(203, 17);
            this.chkBackupPlugins.TabIndex = 0;
            this.chkBackupPlugins.Text = "Include plugins when creating backups";
            this.chkBackupPlugins.UseVisualStyleBackColor = true;
            // 
            // tabInterface
            // 
            this.tabInterface.Controls.Add(this.grpTheme);
            this.tabInterface.Controls.Add(this.chkShowTooltips);
            this.tabInterface.Controls.Add(this.chkConfirmOnClose);
            this.tabInterface.Controls.Add(this.chkMinimizeToTray);
            this.tabInterface.Location = new System.Drawing.Point(4, 22);
            this.tabInterface.Name = "tabInterface";
            this.tabInterface.Size = new System.Drawing.Size(452, 281);
            this.tabInterface.TabIndex = 2;
            this.tabInterface.Text = "Interface";
            this.tabInterface.UseVisualStyleBackColor = true;
            // 
            // grpTheme
            // 
            this.grpTheme.Controls.Add(this.rbDark);
            this.grpTheme.Controls.Add(this.rbLight);
            this.grpTheme.Controls.Add(this.rbSystem);
            this.grpTheme.Location = new System.Drawing.Point(18, 100);
            this.grpTheme.Name = "grpTheme";
            this.grpTheme.Size = new System.Drawing.Size(200, 100);
            this.grpTheme.TabIndex = 3;
            this.grpTheme.TabStop = false;
            this.grpTheme.Text = "Theme";
            // 
            // rbDark
            // 
            this.rbDark.AutoSize = true;
            this.rbDark.Location = new System.Drawing.Point(16, 65);
            this.rbDark.Name = "rbDark";
            this.rbDark.Size = new System.Drawing.Size(48, 17);
            this.rbDark.TabIndex = 2;
            this.rbDark.TabStop = true;
            this.rbDark.Text = "Dark";
            this.rbDark.UseVisualStyleBackColor = true;
            // 
            // rbLight
            // 
            this.rbLight.AutoSize = true;
            this.rbLight.Location = new System.Drawing.Point(16, 42);
            this.rbLight.Name = "rbLight";
            this.rbLight.Size = new System.Drawing.Size(48, 17);
            this.rbLight.TabIndex = 1;
            this.rbLight.TabStop = true;
            this.rbLight.Text = "Light";
            this.rbLight.UseVisualStyleBackColor = true;
            // 
            // rbSystem
            // 
            this.rbSystem.AutoSize = true;
            this.rbSystem.Location = new System.Drawing.Point(16, 19);
            this.rbSystem.Name = "rbSystem";
            this.rbSystem.Size = new System.Drawing.Size(95, 17);
            this.rbSystem.TabIndex = 0;
            this.rbSystem.TabStop = true;
            this.rbSystem.Text = "System Default";
            this.rbSystem.UseVisualStyleBackColor = true;
            // 
            // chkShowTooltips
            // 
            this.chkShowTooltips.AutoSize = true;
            this.chkShowTooltips.Location = new System.Drawing.Point(18, 74);
            this.chkShowTooltips.Name = "chkShowTooltips";
            this.chkShowTooltips.Size = new System.Drawing.Size(96, 17);
            this.chkShowTooltips.TabIndex = 2;
            this.chkShowTooltips.Text = "Show Tooltips";
            this.chkShowTooltips.UseVisualStyleBackColor = true;
            // 
            // chkConfirmOnClose
            // 
            this.chkConfirmOnClose.AutoSize = true;
            this.chkConfirmOnClose.Location = new System.Drawing.Point(18, 51);
            this.chkConfirmOnClose.Name = "chkConfirmOnClose";
            this.chkConfirmOnClose.Size = new System.Drawing.Size(230, 17);
            this.chkConfirmOnClose.TabIndex = 1;
            this.chkConfirmOnClose.Text = "Confirm before closing with running instances";
            this.chkConfirmOnClose.UseVisualStyleBackColor = true;
            // 
            // chkMinimizeToTray
            // 
            this.chkMinimizeToTray.AutoSize = true;
            this.chkMinimizeToTray.Location = new System.Drawing.Point(18, 28);
            this.chkMinimizeToTray.Name = "chkMinimizeToTray";
            this.chkMinimizeToTray.Size = new System.Drawing.Size(151, 17);
            this.chkMinimizeToTray.TabIndex = 0;
            this.chkMinimizeToTray.Text = "Minimize to system tray";
            this.chkMinimizeToTray.UseVisualStyleBackColor = true;
            // 
            // tabAdvanced
            // 
            this.tabAdvanced.Controls.Add(this.chkCheckForRunningInstances);
            this.tabAdvanced.Controls.Add(this.chkStartWithWindows);
            this.tabAdvanced.Controls.Add(this.chkCheckForUpdates);
            this.tabAdvanced.Controls.Add(this.chkAutoBackupAfterExit);
            this.tabAdvanced.Controls.Add(this.chkAutoBackupBeforeLaunch);
            this.tabAdvanced.Location = new System.Drawing.Point(4, 22);
            this.tabAdvanced.Name = "tabAdvanced";
            this.tabAdvanced.Size = new System.Drawing.Size(452, 281);
            this.tabAdvanced.TabIndex = 3;
            this.tabAdvanced.Text = "Advanced";
            this.tabAdvanced.UseVisualStyleBackColor = true;
            // 
            // chkCheckForRunningInstances
            // 
            this.chkCheckForRunningInstances.AutoSize = true;
            this.chkCheckForRunningInstances.Location = new System.Drawing.Point(18, 120);
            this.chkCheckForRunningInstances.Name = "chkCheckForRunningInstances";
            this.chkCheckForRunningInstances.Size = new System.Drawing.Size(253, 17);
            this.chkCheckForRunningInstances.TabIndex = 4;
            this.chkCheckForRunningInstances.Text = "Check for running instances before launching client";
            this.chkCheckForRunningInstances.UseVisualStyleBackColor = true;
            // 
            // chkStartWithWindows
            // 
            this.chkStartWithWindows.AutoSize = true;
            this.chkStartWithWindows.Location = new System.Drawing.Point(18, 97);
            this.chkStartWithWindows.Name = "chkStartWithWindows";
            this.chkStartWithWindows.Size = new System.Drawing.Size(199, 17);
            this.chkStartWithWindows.TabIndex = 3;
            this.chkStartWithWindows.Text = "Start application when Windows starts";
            this.chkStartWithWindows.UseVisualStyleBackColor = true;
            // 
            // chkCheckForUpdates
            // 
            this.chkCheckForUpdates.AutoSize = true;
            this.chkCheckForUpdates.Location = new System.Drawing.Point(18, 74);
            this.chkCheckForUpdates.Name = "chkCheckForUpdates";
            this.chkCheckForUpdates.Size = new System.Drawing.Size(213, 17);
            this.chkCheckForUpdates.TabIndex = 2;
            this.chkCheckForUpdates.Text = "Automatically check for updates on startup";
            this.chkCheckForUpdates.UseVisualStyleBackColor = true;
            // 
            // chkAutoBackupAfterExit
            // 
            this.chkAutoBackupAfterExit.AutoSize = true;
            this.chkAutoBackupAfterExit.Location = new System.Drawing.Point(18, 51);
            this.chkAutoBackupAfterExit.Name = "chkAutoBackupAfterExit";
            this.chkAutoBackupAfterExit.Size = new System.Drawing.Size(219, 17);
            this.chkAutoBackupAfterExit.TabIndex = 1;
            this.chkAutoBackupAfterExit.Text = "Automatically backup profile after client exit";
            this.chkAutoBackupAfterExit.UseVisualStyleBackColor = true;
            // 
            // chkAutoBackupBeforeLaunch
            // 
            this.chkAutoBackupBeforeLaunch.AutoSize = true;
            this.chkAutoBackupBeforeLaunch.Location = new System.Drawing.Point(18, 28);
            this.chkAutoBackupBeforeLaunch.Name = "chkAutoBackupBeforeLaunch";
            this.chkAutoBackupBeforeLaunch.Size = new System.Drawing.Size(237, 17);
            this.chkAutoBackupBeforeLaunch.TabIndex = 0;
            this.chkAutoBackupBeforeLaunch.Text = "Automatically backup profile before client launch";
            this.chkAutoBackupBeforeLaunch.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(316, 326);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(397, 326);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnResetDefaults
            // 
            this.btnResetDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnResetDefaults.Location = new System.Drawing.Point(12, 326);
            this.btnResetDefaults.Name = "btnResetDefaults";
            this.btnResetDefaults.Size = new System.Drawing.Size(95, 23);
            this.btnResetDefaults.TabIndex = 3;
            this.btnResetDefaults.Text = "Reset to Defaults";
            this.btnResetDefaults.UseVisualStyleBackColor = true;
            this.btnResetDefaults.Click += new System.EventHandler(this.btnResetDefaults_Click);
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(484, 361);
            this.Controls.Add(this.btnResetDefaults);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.tabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Application Settings";
            this.tabControl.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.tabGeneral.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxBackups)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLaunchDelay)).EndInit();
            this.tabBackup.ResumeLayout(false);
            this.tabBackup.PerformLayout();
            this.tabInterface.ResumeLayout(false);
            this.tabInterface.PerformLayout();
            this.grpTheme.ResumeLayout(false);
            this.grpTheme.PerformLayout();
            this.tabAdvanced.ResumeLayout(false);
            this.tabAdvanced.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.Button btnBrowseLauncher;
        private System.Windows.Forms.TextBox txtLauncherPath;
        private System.Windows.Forms.Label lblLauncherPath;
        private System.Windows.Forms.TabPage tabBackup;
        private System.Windows.Forms.TabPage tabInterface;
        private System.Windows.Forms.TabPage tabAdvanced;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnResetDefaults;
        private System.Windows.Forms.Button btnBrowseLog;
        private System.Windows.Forms.TextBox txtLogPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnBrowseBackup;
        private System.Windows.Forms.TextBox txtBackupPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numMaxBackups;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numLaunchDelay;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnDetectLauncher;
        private System.Windows.Forms.CheckBox chkRestorePlugins;
        private System.Windows.Forms.CheckBox chkBackupPlugins;
        private System.Windows.Forms.GroupBox grpTheme;
        private System.Windows.Forms.RadioButton rbDark;
        private System.Windows.Forms.RadioButton rbLight;
        private System.Windows.Forms.RadioButton rbSystem;
        private System.Windows.Forms.CheckBox chkShowTooltips;
        private System.Windows.Forms.CheckBox chkConfirmOnClose;
        private System.Windows.Forms.CheckBox chkMinimizeToTray;
        private System.Windows.Forms.CheckBox chkCheckForRunningInstances;
        private System.Windows.Forms.CheckBox chkStartWithWindows;
        private System.Windows.Forms.CheckBox chkCheckForUpdates;
        private System.Windows.Forms.CheckBox chkAutoBackupAfterExit;
        private System.Windows.Forms.CheckBox chkAutoBackupBeforeLaunch;
    }
}