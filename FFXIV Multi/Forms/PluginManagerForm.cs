using System;
using System.Collections.Generic;
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
    public partial class PluginManagerForm : Form
    {
        private readonly ClientProfile _profile;
        private readonly PluginService _pluginService;
        private readonly ProfileManager _profileManager;
        private readonly LogHelper _logHelper;
        private List<PluginInfo> _plugins;
        private BindingSource _bindingSource;

        public PluginManagerForm(ClientProfile profile, PluginService pluginService, ProfileManager profileManager, LogHelper logHelper)
        {
            InitializeComponent();

            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            _logHelper = logHelper ?? throw new ArgumentNullException(nameof(logHelper));

            // Set form title
            Text = $"Plugin Manager - {profile.ProfileName}";

            // Set up data grid
            SetupPluginGrid();

            // Set up profile dropdown for sync
            FillProfileDropdown();
        }

        private void SetupPluginGrid()
        {
            dgvPlugins.AutoGenerateColumns = false;

            // Add columns
            var enabledColumn = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "IsEnabled",
                HeaderText = "Enabled",
                Width = 60
            };

            var nameColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                HeaderText = "Plugin Name",
                Width = 150
            };

            var authorColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Author",
                HeaderText = "Author",
                Width = 120
            };

            var versionColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Version",
                HeaderText = "Version",
                Width = 80
            };

            var typeColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Type",
                Width = 80
            };

            var sizeColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "FormattedSize",
                HeaderText = "Size",
                Width = 70
            };

            dgvPlugins.Columns.AddRange(new DataGridViewColumn[]
            {
                enabledColumn,
                nameColumn,
                authorColumn,
                versionColumn,
                typeColumn,
                sizeColumn
            });

            // Set up binding source
            _bindingSource = new BindingSource();
            dgvPlugins.DataSource = _bindingSource;

            // Handle formatting for specific columns
            dgvPlugins.CellFormatting += DgvPlugins_CellFormatting;

            // Handle cell value changed event for the enabled checkbox
            dgvPlugins.CellValueChanged += DgvPlugins_CellValueChanged;
            dgvPlugins.CurrentCellDirtyStateChanged += DgvPlugins_CurrentCellDirtyStateChanged;
        }

        private void DgvPlugins_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            // Format the Type column
            if (dgvPlugins.Columns[e.ColumnIndex].HeaderText == "Type")
            {
                var plugin = _plugins[e.RowIndex];
                e.Value = plugin.IsDev ? "Dev" : "Installed";
                e.FormattingApplied = true;
            }
        }

        private void DgvPlugins_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // Commit changes immediately when the checkbox value changes
            if (dgvPlugins.IsCurrentCellDirty && dgvPlugins.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgvPlugins.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private async void DgvPlugins_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            // Handle Enabled column changes
            if (dgvPlugins.Columns[e.ColumnIndex].HeaderText == "Enabled")
            {
                var plugin = _plugins[e.RowIndex];

                // Update the profile to track enabled/disabled plugins
                if (plugin.IsEnabled)
                {
                    _profile.DisabledPluginIds.Remove(plugin.InternalName);
                    if (!_profile.EnabledPluginIds.Contains(plugin.InternalName))
                        _profile.EnabledPluginIds.Add(plugin.InternalName);
                }
                else
                {
                    _profile.EnabledPluginIds.Remove(plugin.InternalName);
                    if (!_profile.DisabledPluginIds.Contains(plugin.InternalName))
                        _profile.DisabledPluginIds.Add(plugin.InternalName);
                }

                // Save the profile
                await _profileManager.SaveProfileAsync(_profile);
            }
        }

        private void FillProfileDropdown()
        {
            cboProfiles.Items.Clear();

            // Add profiles except for the current one
            var otherProfiles = _profileManager.GetAllProfilesAsync().Result
                .Where(p => p.Id != _profile.Id)
                .ToArray();

            cboProfiles.Items.AddRange(otherProfiles);
            cboProfiles.DisplayMember = "ProfileName";
            cboProfiles.ValueMember = "Id";

            if (otherProfiles.Length > 0)
                cboProfiles.SelectedIndex = 0;

            btnCopyToProfile.Enabled = otherProfiles.Length > 0;
            btnSyncFromProfile.Enabled = otherProfiles.Length > 0;
        }

        private async void PluginManagerForm_Load(object sender, EventArgs e)
        {
            await LoadPluginsAsync();
        }

        private async Task LoadPluginsAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Loading plugins...";

                _plugins = await _pluginService.GetInstalledPluginsAsync(_profile);

                // Set the IsEnabled property based on profile's enabled/disabled lists
                foreach (var plugin in _plugins)
                {
                    if (_profile.DisabledPluginIds.Contains(plugin.InternalName))
                        plugin.IsEnabled = false;
                    else
                        plugin.IsEnabled = true;
                }

                // Rebind the data source
                _bindingSource.DataSource = null;
                _bindingSource.DataSource = _plugins;

                // Update UI
                lblPluginCount.Text = $"{_plugins.Count} plugins found";
                lblStatus.Text = "Plugins loaded successfully.";

                if (_plugins.Count == 0)
                    lblNoPlugins.Visible = true;
                else
                    lblNoPlugins.Visible = false;

                btnRemovePlugin.Enabled = _plugins.Count > 0;

                Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                lblStatus.Text = "Error loading plugins.";
                _logHelper.LogError($"Error loading plugins: {ex.Message}", ex);

                MessageBox.Show(
                    $"Error loading plugins: {ex.Message}",
                    "Plugin Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void btnCopyToProfile_Click(object sender, EventArgs e)
        {
            if (cboProfiles.SelectedItem == null || _plugins.Count == 0)
                return;

            var targetProfile = (ClientProfile)cboProfiles.SelectedItem;

            // Get selected plugins or use all if none selected
            var selectedPlugins = new List<string>();

            if (dgvPlugins.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvPlugins.SelectedRows)
                {
                    var plugin = _plugins[row.Index];
                    selectedPlugins.Add(plugin.InternalName);
                }
            }

            if (selectedPlugins.Count == 0)
            {
                if (MessageBox.Show(
                    $"No plugins are selected. Do you want to copy all plugins to profile '{targetProfile.ProfileName}'?",
                    "Copy All Plugins",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
            }
            else
            {
                if (MessageBox.Show(
                    $"Are you sure you want to copy {selectedPlugins.Count} selected plugin(s) to profile '{targetProfile.ProfileName}'?",
                    "Copy Selected Plugins",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = $"Copying plugins to profile '{targetProfile.ProfileName}'...";

                bool success = await _pluginService.CopyPluginsAsync(_profile, targetProfile, selectedPlugins.Count > 0 ? selectedPlugins : null);

                Cursor = Cursors.Default;

                if (success)
                {
                    lblStatus.Text = $"Plugins copied successfully to profile '{targetProfile.ProfileName}'.";

                    MessageBox.Show(
                        $"Plugins were successfully copied to profile '{targetProfile.ProfileName}'.",
                        "Copy Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    lblStatus.Text = $"Failed to copy plugins to profile '{targetProfile.ProfileName}'.";

                    MessageBox.Show(
                        $"Failed to copy plugins to profile '{targetProfile.ProfileName}'.",
                        "Copy Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                lblStatus.Text = "Error copying plugins.";

                _logHelper.LogError($"Error copying plugins: {ex.Message}", ex);

                MessageBox.Show(
                    $"Error copying plugins: {ex.Message}",
                    "Copy Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void btnSyncFromProfile_Click(object sender, EventArgs e)
        {
            if (cboProfiles.SelectedItem == null)
                return;

            var sourceProfile = (ClientProfile)cboProfiles.SelectedItem;

            if (MessageBox.Show(
                $"Are you sure you want to synchronize plugins from profile '{sourceProfile.ProfileName}'?\n\n" +
                $"This will add any plugins that are in '{sourceProfile.ProfileName}' but not in '{_profile.ProfileName}', " +
                $"and remove any plugins that are in '{_profile.ProfileName}' but not in '{sourceProfile.ProfileName}'.",
                "Confirm Sync",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = $"Synchronizing plugins from profile '{sourceProfile.ProfileName}'...";

                bool success = await _pluginService.SyncPluginsAsync(sourceProfile, _profile);

                Cursor = Cursors.Default;

                if (success)
                {
                    lblStatus.Text = $"Plugins synchronized successfully from profile '{sourceProfile.ProfileName}'.";

                    MessageBox.Show(
                        $"Plugins were successfully synchronized from profile '{sourceProfile.ProfileName}'.",
                        "Sync Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // Reload the plugins list
                    await LoadPluginsAsync();
                }
                else
                {
                    lblStatus.Text = $"Failed to synchronize plugins from profile '{sourceProfile.ProfileName}'.";

                    MessageBox.Show(
                        $"Failed to synchronize plugins from profile '{sourceProfile.ProfileName}'.",
                        "Sync Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                lblStatus.Text = "Error synchronizing plugins.";

                _logHelper.LogError($"Error synchronizing plugins: {ex.Message}", ex);

                MessageBox.Show(
                    $"Error synchronizing plugins: {ex.Message}",
                    "Sync Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void btnRemovePlugin_Click(object sender, EventArgs e)
        {
            if (dgvPlugins.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Please select a plugin to remove.",
                    "No Selection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var selectedPlugins = new List<PluginInfo>();

            foreach (DataGridViewRow row in dgvPlugins.SelectedRows)
            {
                selectedPlugins.Add(_plugins[row.Index]);
            }

            if (MessageBox.Show(
                $"Are you sure you want to remove {selectedPlugins.Count} selected plugin(s) from profile '{_profile.ProfileName}'?\n\n" +
                "This will delete the plugin files from disk and cannot be undone.",
                "Confirm Remove",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Removing selected plugins...";

                int successCount = 0;
                int failCount = 0;

                foreach (var plugin in selectedPlugins)
                {
                    bool success = await _pluginService.RemovePluginAsync(_profile, plugin.InternalName);

                    if (success)
                        successCount++;
                    else
                        failCount++;
                }

                Cursor = Cursors.Default;

                if (failCount == 0)
                {
                    lblStatus.Text = $"Successfully removed {successCount} plugin(s).";

                    MessageBox.Show(
                        $"Successfully removed {successCount} plugin(s).",
                        "Remove Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    lblStatus.Text = $"Removed {successCount} plugin(s), failed to remove {failCount} plugin(s).";

                    MessageBox.Show(
                        $"Removed {successCount} plugin(s), failed to remove {failCount} plugin(s).",
                        "Remove Partial",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // Reload the plugins list
                await LoadPluginsAsync();
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                lblStatus.Text = "Error removing plugins.";

                _logHelper.LogError($"Error removing plugins: {ex.Message}", ex);

                MessageBox.Show(
                    $"Error removing plugins: {ex.Message}",
                    "Remove Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnViewDetails_Click(object sender, EventArgs e)
        {
            if (dgvPlugins.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Please select a plugin to view details.",
                    "No Selection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var plugin = _plugins[dgvPlugins.SelectedRows[0].Index];

            MessageBox.Show(
                $"Plugin Name: {plugin.Name}\n" +
                $"Internal Name: {plugin.InternalName}\n" +
                $"Author: {plugin.Author}\n" +
                $"Version: {plugin.Version}\n" +
                $"Type: {(plugin.IsDev ? "Developer" : "Installed")}\n" +
                $"Size: {plugin.FormattedSize}\n" +
                $"Description: {plugin.Description}\n" +
                $"Repository URL: {plugin.RepoUrl}\n" +
                $"Has Config: {plugin.HasConfig}\n" +
                $"Path: {plugin.InstallPath}",
                $"Plugin Details - {plugin.Name}",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await LoadPluginsAsync();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
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
            this.dgvPlugins = new System.Windows.Forms.DataGridView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblSyncHeader = new System.Windows.Forms.Label();
            this.btnSyncFromProfile = new System.Windows.Forms.Button();
            this.btnCopyToProfile = new System.Windows.Forms.Button();
            this.cboProfiles = new System.Windows.Forms.ComboBox();
            this.lblPluginsHeader = new System.Windows.Forms.Label();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblPluginCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnRemovePlugin = new System.Windows.Forms.Button();
            this.btnViewDetails = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblNoPlugins = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlugins)).BeginInit();
            this.panel1.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvPlugins
            // 
            this.dgvPlugins.AllowUserToAddRows = false;
            this.dgvPlugins.AllowUserToDeleteRows = false;
            this.dgvPlugins.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvPlugins.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvPlugins.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPlugins.Location = new System.Drawing.Point(12, 30);
            this.dgvPlugins.Name = "dgvPlugins";
            this.dgvPlugins.RowHeadersVisible = false;
            this.dgvPlugins.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPlugins.Size = new System.Drawing.Size(560, 295);
            this.dgvPlugins.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.lblSyncHeader);
            this.panel1.Controls.Add(this.btnSyncFromProfile);
            this.panel1.Controls.Add(this.btnCopyToProfile);
            this.panel1.Controls.Add(this.cboProfiles);
            this.panel1.Location = new System.Drawing.Point(578, 30);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(194, 120);
            this.panel1.TabIndex = 1;
            // 
            // lblSyncHeader
            // 
            this.lblSyncHeader.AutoSize = true;
            this.lblSyncHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSyncHeader.Location = new System.Drawing.Point(3, 9);
            this.lblSyncHeader.Name = "lblSyncHeader";
            this.lblSyncHeader.Size = new System.Drawing.Size(133, 13);
            this.lblSyncHeader.TabIndex = 3;
            this.lblSyncHeader.Text = "Sync with other profile";
            // 
            // btnSyncFromProfile
            // 
            this.btnSyncFromProfile.Location = new System.Drawing.Point(6, 85);
            this.btnSyncFromProfile.Name = "btnSyncFromProfile";
            this.btnSyncFromProfile.Size = new System.Drawing.Size(183, 23);
            this.btnSyncFromProfile.TabIndex = 2;
            this.btnSyncFromProfile.Text = "Sync From Selected Profile";
            this.btnSyncFromProfile.UseVisualStyleBackColor = true;
            this.btnSyncFromProfile.Click += new System.EventHandler(this.btnSyncFromProfile_Click);
            // 
            // btnCopyToProfile
            // 
            this.btnCopyToProfile.Location = new System.Drawing.Point(6, 56);
            this.btnCopyToProfile.Name = "btnCopyToProfile";
            this.btnCopyToProfile.Size = new System.Drawing.Size(183, 23);
            this.btnCopyToProfile.TabIndex = 1;
            this.btnCopyToProfile.Text = "Copy To Selected Profile";
            this.btnCopyToProfile.UseVisualStyleBackColor = true;
            this.btnCopyToProfile.Click += new System.EventHandler(this.btnCopyToProfile_Click);
            // 
            // cboProfiles
            // 
            this.cboProfiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboProfiles.FormattingEnabled = true;
            this.cboProfiles.Location = new System.Drawing.Point(6, 29);
            this.cboProfiles.Name = "cboProfiles";
            this.cboProfiles.Size = new System.Drawing.Size(183, 21);
            this.cboProfiles.TabIndex = 0;
            // 
            // lblPluginsHeader
            // 
            this.lblPluginsHeader.AutoSize = true;
            this.lblPluginsHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPluginsHeader.Location = new System.Drawing.Point(12, 14);
            this.lblPluginsHeader.Name = "lblPluginsHeader";
            this.lblPluginsHeader.Size = new System.Drawing.Size(107, 13);
            this.lblPluginsHeader.TabIndex = 2;
            this.lblPluginsHeader.Text = "Installed Plugins:";
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.lblPluginCount});
            this.statusStrip.Location = new System.Drawing.Point(0, 369);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(784, 22);
            this.statusStrip.TabIndex = 3;
            this.statusStrip.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(39, 17);
            this.lblStatus.Text = "Ready";
            // 
            // lblPluginCount
            // 
            this.lblPluginCount.Name = "lblPluginCount";
            this.lblPluginCount.Size = new System.Drawing.Size(96, 17);
            this.lblPluginCount.Text = "No plugins found";
            // 
            // btnRemovePlugin
            // 
            this.btnRemovePlugin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemovePlugin.Location = new System.Drawing.Point(578, 165);
            this.btnRemovePlugin.Name = "btnRemovePlugin";
            this.btnRemovePlugin.Size = new System.Drawing.Size(194, 23);
            this.btnRemovePlugin.TabIndex = 4;
            this.btnRemovePlugin.Text = "Remove Selected Plugin";
            this.btnRemovePlugin.UseVisualStyleBackColor = true;
            this.btnRemovePlugin.Click += new System.EventHandler(this.btnRemovePlugin_Click);
            // 
            // btnViewDetails
            // 
            this.btnViewDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnViewDetails.Location = new System.Drawing.Point(578, 194);
            this.btnViewDetails.Name = "btnViewDetails";
            this.btnViewDetails.Size = new System.Drawing.Size(194, 23);
            this.btnViewDetails.TabIndex = 5;
            this.btnViewDetails.Text = "View Plugin Details";
            this.btnViewDetails.UseVisualStyleBackColor = true;
            this.btnViewDetails.Click += new System.EventHandler(this.btnViewDetails_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefresh.Location = new System.Drawing.Point(578, 223);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(194, 23);
            this.btnRefresh.TabIndex = 6;
            this.btnRefresh.Text = "Refresh Plugin List";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(697, 334);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 7;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // lblNoPlugins
            // 
            this.lblNoPlugins.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblNoPlugins.AutoSize = true;
            this.lblNoPlugins.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNoPlugins.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblNoPlugins.Location = new System.Drawing.Point(201, 171);
            this.lblNoPlugins.Name = "lblNoPlugins";
            this.lblNoPlugins.Size = new System.Drawing.Size(182, 16);
            this.lblNoPlugins.TabIndex = 8;
            this.lblNoPlugins.Text = "No plugins found for this profile";
            this.lblNoPlugins.Visible = false;
            // 
            // PluginManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 391);
            this.Controls.Add(this.lblNoPlugins);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnViewDetails);
            this.Controls.Add(this.btnRemovePlugin);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.lblPluginsHeader);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.dgvPlugins);
            this.MinimumSize = new System.Drawing.Size(800, 430);
            this.Name = "PluginManagerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Plugin Manager";
            this.Load += new System.EventHandler(this.PluginManagerForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlugins)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.DataGridView dgvPlugins;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblPluginsHeader;
        private System.Windows.Forms.Label lblSyncHeader;
        private System.Windows.Forms.Button btnSyncFromProfile;
        private System.Windows.Forms.Button btnCopyToProfile;
        private System.Windows.Forms.ComboBox cboProfiles;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblPluginCount;
        private System.Windows.Forms.Button btnRemovePlugin;
        private System.Windows.Forms.Button btnViewDetails;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblNoPlugins;
    }
}