using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FFXIV_Multi.Models;
using FFXIV_Multi.Services;
using FFXIVClientManager.Models;
using FFXIVClientManager.Services;
using FFXIVClientManager.Utils;

namespace FFXIVClientManager.Forms
{
    public partial class BackupManagerForm : Form
    {
        private readonly List<ClientProfile> _profiles;
        private readonly BackupService _backupService;
        private readonly LogHelper _logHelper;
        private BindingSource _bindingSource;
        private List<BackupInfo> _currentBackups;
        private ClientProfile _selectedProfile;

        public BackupManagerForm(List<ClientProfile> profiles, BackupService backupService, LogHelper logHelper)
        {
            InitializeComponent();

            _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            _logHelper = logHelper ?? throw new ArgumentNullException(nameof(logHelper));

            // Set up the data grid
            SetupDataGrid();

            // Subscribe to events
            _backupService.BackupCompleted += OnBackupCompleted;
            _backupService.BackupFailed += OnBackupFailed;

            // Initialize profile dropdown
            FillProfileDropdown();
        }

        private void SetupDataGrid()
        {
            dgvBackups.AutoGenerateColumns = false;

            // Add columns
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "FileName",
                HeaderText = "Backup Name",
                Width = 250
            });

            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CreationTime",
                HeaderText = "Created",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" }
            });

            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "FormattedSize",
                HeaderText = "Size",
                Width = 80
            });

            // Set up binding source
            _bindingSource = new BindingSource();
            dgvBackups.DataSource = _bindingSource;
        }

        private void FillProfileDropdown()
        {
            cboProfiles.Items.Clear();
            cboProfiles.Items.AddRange(_profiles.ToArray());
            cboProfiles.DisplayMember = "ProfileName";
            cboProfiles.ValueMember = "Id";

            if (_profiles.Count > 0)
            {
                cboProfiles.SelectedIndex = 0;
                _selectedProfile = (ClientProfile)cboProfiles.SelectedItem;
                LoadBackupsForProfile(_selectedProfile);
            }
        }

        private void LoadBackupsForProfile(ClientProfile profile)
        {
            if (profile == null)
                return;

            _currentBackups = _backupService.GetAvailableBackups(profile);
            _bindingSource.DataSource = _currentBackups;

            if (_currentBackups.Count == 0)
            {
                lblNoBackups.Visible = true;
                lblBackupCount.Text = "No backups found";
            }
            else
            {
                lblNoBackups.Visible = false;
                lblBackupCount.Text = $"{_currentBackups.Count} backup(s) found";
            }

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool hasBackups = _currentBackups != null && _currentBackups.Count > 0;
            bool hasSelection = dgvBackups.CurrentRow != null && dgvBackups.CurrentRow.Index >= 0;

            btnRestoreBackup.Enabled = hasBackups && hasSelection;
            btnDeleteBackup.Enabled = hasBackups && hasSelection;
            btnDeleteAllBackups.Enabled = hasBackups;
            btnExportBackup.Enabled = hasBackups && hasSelection;
        }

        private async void btnCreateBackup_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null)
                return;

            try
            {
                Cursor = Cursors.WaitCursor;
                btnCreateBackup.Enabled = false;
                lblStatus.Text = $"Creating backup for profile '{_selectedProfile.ProfileName}'...";

                string backupPath = await _backupService.CreateBackupAsync(_selectedProfile);

                Cursor = Cursors.Default;
                btnCreateBackup.Enabled = true;
                lblStatus.Text = $"Backup created successfully.";

                // Refresh the backup list
                LoadBackupsForProfile(_selectedProfile);
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                btnCreateBackup.Enabled = true;
                lblStatus.Text = "Error creating backup.";

                _logHelper.LogError($"Error creating backup: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error creating backup: {ex.Message}",
                    "Backup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void btnRestoreBackup_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null || dgvBackups.CurrentRow == null)
                return;

            var backup = (BackupInfo)dgvBackups.CurrentRow.DataBoundItem;

            if (MessageBox.Show(
                $"Are you sure you want to restore profile '{_selectedProfile.ProfileName}' from backup '{backup.FileName}'?\n\nThis will overwrite the current profile configuration.",
                "Confirm Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                btnRestoreBackup.Enabled = false;
                lblStatus.Text = $"Restoring profile '{_selectedProfile.ProfileName}' from backup...";

                bool success = await _backupService.RestoreBackupAsync(backup.FilePath, _selectedProfile);

                Cursor = Cursors.Default;
                btnRestoreBackup.Enabled = true;

                if (success)
                {
                    lblStatus.Text = "Profile restored successfully.";
                    MessageBox.Show(
                        $"Profile '{_selectedProfile.ProfileName}' has been restored successfully.",
                        "Restore Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    lblStatus.Text = "Failed to restore profile.";
                    MessageBox.Show(
                        $"Failed to restore profile '{_selectedProfile.ProfileName}'.",
                        "Restore Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                btnRestoreBackup.Enabled = true;
                lblStatus.Text = "Error restoring backup.";

                _logHelper.LogError($"Error restoring backup: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error restoring backup: {ex.Message}",
                    "Restore Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnDeleteBackup_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null || dgvBackups.CurrentRow == null)
                return;

            var backup = (BackupInfo)dgvBackups.CurrentRow.DataBoundItem;

            if (MessageBox.Show(
                $"Are you sure you want to delete the backup '{backup.FileName}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                bool success = _backupService.DeleteBackup(backup.FilePath);

                if (success)
                {
                    lblStatus.Text = "Backup deleted successfully.";
                    LoadBackupsForProfile(_selectedProfile);
                }
                else
                {
                    lblStatus.Text = "Failed to delete backup.";
                    MessageBox.Show(
                        $"Failed to delete backup '{backup.FileName}'.",
                        "Delete Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error deleting backup.";
                _logHelper.LogError($"Error deleting backup: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error deleting backup: {ex.Message}",
                    "Delete Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnDeleteAllBackups_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null || _currentBackups.Count == 0)
                return;

            if (MessageBox.Show(
                $"Are you sure you want to delete ALL backups for profile '{_selectedProfile.ProfileName}'?\n\nThis action cannot be undone.",
                "Confirm Delete All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                int deleteCount = 0;
                int failCount = 0;

                foreach (var backup in _currentBackups.ToList())
                {
                    if (_backupService.DeleteBackup(backup.FilePath))
                        deleteCount++;
                    else
                        failCount++;
                }

                if (failCount == 0)
                    lblStatus.Text = $"Successfully deleted {deleteCount} backup(s).";
                else
                    lblStatus.Text = $"Deleted {deleteCount} backup(s), failed to delete {failCount} backup(s).";

                LoadBackupsForProfile(_selectedProfile);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error deleting backups.";
                _logHelper.LogError($"Error deleting backups: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error deleting backups: {ex.Message}",
                    "Delete Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnExportBackup_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null || dgvBackups.CurrentRow == null)
                return;

            var backup = (BackupInfo)dgvBackups.CurrentRow.DataBoundItem;

            if (!File.Exists(backup.FilePath))
            {
                MessageBox.Show(
                    $"Backup file does not exist: {backup.FilePath}",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Export Backup";
                dialog.FileName = backup.FileName;
                dialog.Filter = "ZIP Files (*.zip)|*.zip";
                dialog.OverwritePrompt = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.Copy(backup.FilePath, dialog.FileName, true);
                        lblStatus.Text = "Backup exported successfully.";

                        MessageBox.Show(
                            $"Backup exported successfully to:\n{dialog.FileName}",
                            "Export Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = "Error exporting backup.";
                        _logHelper.LogError($"Error exporting backup: {ex.Message}", ex);
                        MessageBox.Show(
                            $"Error exporting backup: {ex.Message}",
                            "Export Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnBackupAll_Click(object sender, EventArgs e)
        {
            if (_profiles.Count == 0)
            {
                MessageBox.Show(
                    "No profiles available for backup.",
                    "No Profiles",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            BackupAllProfiles();
        }

        private async void BackupAllProfiles()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                btnBackupAll.Enabled = false;
                lblStatus.Text = "Creating backups for all profiles...";

                var results = await _backupService.BackupAllProfilesAsync(_profiles);

                Cursor = Cursors.Default;
                btnBackupAll.Enabled = true;

                int successCount = results.Count(r => r.Value != null);
                int failCount = results.Count(r => r.Value == null);

                lblStatus.Text = $"Completed backups: {successCount} succeeded, {failCount} failed.";

                if (_selectedProfile != null)
                    LoadBackupsForProfile(_selectedProfile);

                if (failCount > 0)
                {
                    MessageBox.Show(
                        $"Created {successCount} backups successfully, {failCount} backups failed. Check the log for details.",
                        "Backup Completed with Errors",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(
                        $"Created {successCount} backups successfully.",
                        "Backup Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                btnBackupAll.Enabled = true;
                lblStatus.Text = "Error backing up profiles.";

                _logHelper.LogError($"Error backing up profiles: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error backing up profiles: {ex.Message}",
                    "Backup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnOpenBackupFolder_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null || _currentBackups.Count == 0)
                return;

            try
            {
                string directory = Path.GetDirectoryName(_currentBackups[0].FilePath);

                if (Directory.Exists(directory))
                {
                    Process.Start("explorer.exe", directory);
                }
                else
                {
                    MessageBox.Show(
                        $"Backup directory not found: {directory}",
                        "Directory Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error opening backup folder: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error opening backup folder: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnCleanupOldBackups_Click(object sender, EventArgs e)
        {
            if (_profiles.Count == 0)
                return;

            try
            {
                int maxBackups = (int)numMaxBackups.Value;
                _backupService.CleanupOldBackups(maxBackups);

                lblStatus.Text = $"Cleaned up old backups, keeping {maxBackups} most recent backups per profile.";

                if (_selectedProfile != null)
                    LoadBackupsForProfile(_selectedProfile);

                MessageBox.Show(
                    $"Successfully cleaned up old backups, keeping the {maxBackups} most recent backups for each profile.",
                    "Cleanup Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error cleaning up old backups.";
                _logHelper.LogError($"Error cleaning up old backups: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error cleaning up old backups: {ex.Message}",
                    "Cleanup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cboProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedProfile = (ClientProfile)cboProfiles.SelectedItem;
            if (_selectedProfile != null)
            {
                LoadBackupsForProfile(_selectedProfile);
            }
        }

        private void dgvBackups_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void OnBackupCompleted(object sender, BackupCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnBackupCompleted(sender, e)));
                return;
            }

            // Only refresh if the completed backup is for the selected profile
            if (_selectedProfile != null && e.Profile.Id == _selectedProfile.Id)
            {
                LoadBackupsForProfile(_selectedProfile);
            }
        }

        private void OnBackupFailed(object sender, BackupFailedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnBackupFailed(sender, e)));
                return;
            }

            lblStatus.Text = $"Backup failed for profile '{e.Profile.ProfileName}': {e.ErrorMessage}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Unsubscribe from events
            _backupService.BackupCompleted -= OnBackupCompleted;
            _backupService.BackupFailed -= OnBackupFailed;
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
            this.dgvBackups = new System.Windows.Forms.DataGridView();
            this.lblProfileHeader = new System.Windows.Forms.Label();
            this.cboProfiles = new System.Windows.Forms.ComboBox();
            this.btnCreateBackup = new System.Windows.Forms.Button();
            this.btnRestoreBackup = new System.Windows.Forms.Button();
            this.btnDeleteBackup = new System.Windows.Forms.Button();
            this.btnDeleteAllBackups = new System.Windows.Forms.Button();
            this.btnExportBackup = new System.Windows.Forms.Button();
            this.btnOpenBackupFolder = new System.Windows.Forms.Button();
            this.btnBackupAll = new System.Windows.Forms.Button();
            this.btnCleanupOldBackups = new System.Windows.Forms.Button();
            this.numMaxBackups = new System.Windows.Forms.NumericUpDown();
            this.lblKeep = new System.Windows.Forms.Label();
            this.lblBackups = new System.Windows.Forms.Label();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblBackupCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblNoBackups = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBackups)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxBackups)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvBackups
            // 
            this.dgvBackups.AllowUserToAddRows = false;
            this.dgvBackups.AllowUserToDeleteRows = false;
            this.dgvBackups.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvBackups.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvBackups.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvBackups.Location = new System.Drawing.Point(12, 39);
            this.dgvBackups.MultiSelect = false;
            this.dgvBackups.Name = "dgvBackups";
            this.dgvBackups.ReadOnly = true;
            this.dgvBackups.RowHeadersVisible = false;
            this.dgvBackups.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvBackups.Size = new System.Drawing.Size(520, 321);
            this.dgvBackups.TabIndex = 0;
            this.dgvBackups.SelectionChanged += new System.EventHandler(this.dgvBackups_SelectionChanged);
            // 
            // lblProfileHeader
            // 
            this.lblProfileHeader.AutoSize = true;
            this.lblProfileHeader.Location = new System.Drawing.Point(12, 15);
            this.lblProfileHeader.Name = "lblProfileHeader";
            this.lblProfileHeader.Size = new System.Drawing.Size(39, 13);
            this.lblProfileHeader.TabIndex = 1;
            this.lblProfileHeader.Text = "Profile:";
            // 
            // cboProfiles
            // 
            this.cboProfiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboProfiles.FormattingEnabled = true;
            this.cboProfiles.Location = new System.Drawing.Point(57, 12);
            this.cboProfiles.Name = "cboProfiles";
            this.cboProfiles.Size = new System.Drawing.Size(249, 21);
            this.cboProfiles.TabIndex = 2;
            this.cboProfiles.SelectedIndexChanged += new System.EventHandler(this.cboProfiles_SelectedIndexChanged);
            // 
            // btnCreateBackup
            // 
            this.btnCreateBackup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCreateBackup.Location = new System.Drawing.Point(538, 39);
            this.btnCreateBackup.Name = "btnCreateBackup";
            this.btnCreateBackup.Size = new System.Drawing.Size(134, 23);
            this.btnCreateBackup.TabIndex = 3;
            this.btnCreateBackup.Text = "Create Backup";
            this.btnCreateBackup.UseVisualStyleBackColor = true;
            this.btnCreateBackup.Click += new System.EventHandler(this.btnCreateBackup_Click);
            // 
            // btnRestoreBackup
            // 
            this.btnRestoreBackup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRestoreBackup.Location = new System.Drawing.Point(538, 68);
            this.btnRestoreBackup.Name = "btnRestoreBackup";
            this.btnRestoreBackup.Size = new System.Drawing.Size(134, 23);
            this.btnRestoreBackup.TabIndex = 4;
            this.btnRestoreBackup.Text = "Restore Selected";
            this.btnRestoreBackup.UseVisualStyleBackColor = true;
            this.btnRestoreBackup.Click += new System.EventHandler(this.btnRestoreBackup_Click);
            // 
            // btnDeleteBackup
            // 
            this.btnDeleteBackup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDeleteBackup.Location = new System.Drawing.Point(538, 97);
            this.btnDeleteBackup.Name = "btnDeleteBackup";
            this.btnDeleteBackup.Size = new System.Drawing.Size(134, 23);
            this.btnDeleteBackup.TabIndex = 5;
            this.btnDeleteBackup.Text = "Delete Selected";
            this.btnDeleteBackup.UseVisualStyleBackColor = true;
            this.btnDeleteBackup.Click += new System.EventHandler(this.btnDeleteBackup_Click);
            // 
            // btnDeleteAllBackups
            // 
            this.btnDeleteAllBackups.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDeleteAllBackups.Location = new System.Drawing.Point(538, 126);
            this.btnDeleteAllBackups.Name = "btnDeleteAllBackups";
            this.btnDeleteAllBackups.Size = new System.Drawing.Size(134, 23);
            this.btnDeleteAllBackups.TabIndex = 6;
            this.btnDeleteAllBackups.Text = "Delete All";
            this.btnDeleteAllBackups.UseVisualStyleBackColor = true;
            this.btnDeleteAllBackups.Click += new System.EventHandler(this.btnDeleteAllBackups_Click);
            // 
            // btnExportBackup
            // 
            this.btnExportBackup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportBackup.Location = new System.Drawing.Point(538, 155);
            this.btnExportBackup.Name = "btnExportBackup";
            this.btnExportBackup.Size = new System.Drawing.Size(134, 23);
            this.btnExportBackup.TabIndex = 7;
            this.btnExportBackup.Text = "Export Backup";
            this.btnExportBackup.UseVisualStyleBackColor = true;
            this.btnExportBackup.Click += new System.EventHandler(this.btnExportBackup_Click);
            // 
            // btnOpenBackupFolder
            // 
            this.btnOpenBackupFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenBackupFolder.Location = new System.Drawing.Point(538, 184);
            this.btnOpenBackupFolder.Name = "btnOpenBackupFolder";
            this.btnOpenBackupFolder.Size = new System.Drawing.Size(134, 23);
            this.btnOpenBackupFolder.TabIndex = 8;
            this.btnOpenBackupFolder.Text = "Open Backup Folder";
            this.btnOpenBackupFolder.UseVisualStyleBackColor = true;
            this.btnOpenBackupFolder.Click += new System.EventHandler(this.btnOpenBackupFolder_Click);
            // 
            // btnBackupAll
            // 
            this.btnBackupAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBackupAll.Location = new System.Drawing.Point(538, 223);
            this.btnBackupAll.Name = "btnBackupAll";
            this.btnBackupAll.Size = new System.Drawing.Size(134, 23);
            this.btnBackupAll.TabIndex = 9;
            this.btnBackupAll.Text = "Backup All Profiles";
            this.btnBackupAll.UseVisualStyleBackColor = true;
            this.btnBackupAll.Click += new System.EventHandler(this.btnBackupAll_Click);
            // 
            // btnCleanupOldBackups
            // 
            this.btnCleanupOldBackups.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCleanupOldBackups.Location = new System.Drawing.Point(538, 268);
            this.btnCleanupOldBackups.Name = "btnCleanupOldBackups";
            this.btnCleanupOldBackups.Size = new System.Drawing.Size(134, 23);
            this.btnCleanupOldBackups.TabIndex = 10;
            this.btnCleanupOldBackups.Text = "Cleanup Old Backups";
            this.btnCleanupOldBackups.UseVisualStyleBackColor = true;
            this.btnCleanupOldBackups.Click += new System.EventHandler(this.btnCleanupOldBackups_Click);
            // 
            // numMaxBackups
            // 
            this.numMaxBackups.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numMaxBackups.Location = new System.Drawing.Point(568, 297);
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
            this.numMaxBackups.Size = new System.Drawing.Size(39, 20);
            this.numMaxBackups.TabIndex = 11;
            this.numMaxBackups.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // lblKeep
            // 
            this.lblKeep.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblKeep.AutoSize = true;
            this.lblKeep.Location = new System.Drawing.Point(540, 299);
            this.lblKeep.Name = "lblKeep";
            this.lblKeep.Size = new System.Drawing.Size(32, 13);
            this.lblKeep.TabIndex = 12;
            this.lblKeep.Text = "Keep";
            // 
            // lblBackups
            // 
            this.lblBackups.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBackups.AutoSize = true;
            this.lblBackups.Location = new System.Drawing.Point(609, 299);
            this.lblBackups.Name = "lblBackups";
            this.lblBackups.Size = new System.Drawing.Size(50, 13);
            this.lblBackups.TabIndex = 13;
            this.lblBackups.Text = "backups";
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.lblBackupCount});
            this.statusStrip.Location = new System.Drawing.Point(0, 366);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(684, 22);
            this.statusStrip.TabIndex = 14;
            this.statusStrip.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(39, 17);
            this.lblStatus.Text = "Ready";
            // 
            // lblBackupCount
            // 
            this.lblBackupCount.Name = "lblBackupCount";
            this.lblBackupCount.Size = new System.Drawing.Size(99, 17);
            this.lblBackupCount.Text = "No backups found";
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(597, 337);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 15;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // lblNoBackups
            // 
            this.lblNoBackups.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblNoBackups.AutoSize = true;
            this.lblNoBackups.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNoBackups.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblNoBackups.Location = new System.Drawing.Point(181, 189);
            this.lblNoBackups.Name = "lblNoBackups";
            this.lblNoBackups.Size = new System.Drawing.Size(183, 16);
            this.lblNoBackups.TabIndex = 16;
            this.lblNoBackups.Text = "No backups found for this profile";
            this.lblNoBackups.Visible = false;
            // 
            // BackupManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 388);
            this.Controls.Add(this.lblNoBackups);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.lblBackups);
            this.Controls.Add(this.lblKeep);
            this.Controls.Add(this.numMaxBackups);
            this.Controls.Add(this.btnCleanupOldBackups);
            this.Controls.Add(this.btnBackupAll);
            this.Controls.Add(this.btnOpenBackupFolder);
            this.Controls.Add(this.btnExportBackup);
            this.Controls.Add(this.btnDeleteAllBackups);
            this.Controls.Add(this.btnDeleteBackup);
            this.Controls.Add(this.btnRestoreBackup);
            this.Controls.Add(this.btnCreateBackup);
            this.Controls.Add(this.cboProfiles);
            this.Controls.Add(this.lblProfileHeader);
            this.Controls.Add(this.dgvBackups);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(700, 400);
            this.Name = "BackupManagerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Backup Manager";
            ((System.ComponentModel.ISupportInitialize)(this.dgvBackups)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxBackups)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.DataGridView dgvBackups;
        private System.Windows.Forms.Label lblProfileHeader;
        private System.Windows.Forms.ComboBox cboProfiles;
        private System.Windows.Forms.Button btnCreateBackup;
        private System.Windows.Forms.Button btnRestoreBackup;
        private System.Windows.Forms.Button btnDeleteBackup;
        private System.Windows.Forms.Button btnDeleteAllBackups;
        private System.Windows.Forms.Button btnExportBackup;
        private System.Windows.Forms.Button btnOpenBackupFolder;
        private System.Windows.Forms.Button btnBackupAll;
        private System.Windows.Forms.Button btnCleanupOldBackups;
        private System.Windows.Forms.NumericUpDown numMaxBackups;
        private System.Windows.Forms.Label lblKeep;
        private System.Windows.Forms.Label lblBackups;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblBackupCount;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblNoBackups;
    }
}