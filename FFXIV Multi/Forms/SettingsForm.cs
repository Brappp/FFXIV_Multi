using System;
using System.Windows.Forms;
using FFXIV_Multi.Models; // Added to resolve LauncherSettings

namespace FFXIVClientManager.Forms
{
    public partial class SettingsForm : Form
    {
        // Declare a field to hold the settings object.
        private LauncherSettings _settings;

        /// <summary>
        /// Initializes a new instance of the SettingsForm.
        /// </summary>
        /// <param name="settings">The LauncherSettings instance to be edited.</param>
        public SettingsForm(LauncherSettings settings)
        {
            // Validate the incoming settings parameter.
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            InitializeComponent();
            LoadSettings();
        }

        /// <summary>
        /// Loads the settings values into the form controls.
        /// </summary>
        private void LoadSettings()
        {
            // Set form controls to display current settings.
            txtXIVLauncherPath.Text = _settings.XIVLauncherPath;
            txtBackupPath.Text = _settings.BackupPath;
            txtLogPath.Text = _settings.LogPath;
            numLaunchDelay.Value = _settings.DefaultLaunchDelay;
            chkBackupPlugins.Checked = _settings.BackupPlugins;
            chkRestorePlugins.Checked = _settings.RestorePlugins;
            // Load additional settings as required.
        }

        /// <summary>
        /// Event handler for saving settings.
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            // Update settings object with values from form controls.
            _settings.XIVLauncherPath = txtXIVLauncherPath.Text;
            _settings.BackupPath = txtBackupPath.Text;
            _settings.LogPath = txtLogPath.Text;
            _settings.DefaultLaunchDelay = (int)numLaunchDelay.Value;
            _settings.BackupPlugins = chkBackupPlugins.Checked;
            _settings.RestorePlugins = chkRestorePlugins.Checked;
            // Update additional settings as needed.

            // Persist settings to disk.
            _settings.SaveSettings();

            MessageBox.Show("Settings saved successfully.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        /// <summary>
        /// Event handler for canceling without saving.
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
