using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FFXIV_Multi.Models;
using FFXIVClientManager.Models;
using FFXIVClientManager.Services;

namespace FFXIVClientManager.Forms
{
    public partial class BackupForm : Form
    {
        private readonly ClientProfile _profile;
        private readonly List<BackupInfo> _backups;
        private BindingSource _bindingSource;

        public BackupInfo SelectedBackup { get; private set; }

        public BackupForm(ClientProfile profile, List<BackupInfo> backups)
        {
            InitializeComponent();

            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _backups = backups ?? new List<BackupInfo>();

            // Set the form title
            Text = $"Backup Manager - {profile.ProfileName}";

            // Set up the data grid
            SetupDataGrid();

            // Populate the backup list
            _bindingSource = new BindingSource();
            _bindingSource.DataSource = _backups;
            dgvBackups.DataSource = _bindingSource;
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
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (dgvBackups.CurrentRow == null)
            {
                MessageBox.Show("Please select a backup to restore.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedBackup = (BackupInfo)dgvBackups.CurrentRow.DataBoundItem;

            if (MessageBox.Show(
                $"Are you sure you want to restore profile '{_profile.ProfileName}' from backup '{SelectedBackup.FileName}'?\n\nThis will overwrite the current profile configuration.",
                "Confirm Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnViewBackupLocation_Click(object sender, EventArgs e)
        {
            if (dgvBackups.CurrentRow == null || _backups.Count == 0)
            {
                MessageBox.Show("No backups available.", "No Backups", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var backup = (BackupInfo)dgvBackups.CurrentRow.DataBoundItem;
                string directory = Path.GetDirectoryName(backup.FilePath);

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
                MessageBox.Show(
                    $"Error opening backup location: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            this.dgvBackups = new System.Windows.Forms.DataGridView();
            this.lblBackupsHeader = new System.Windows.Forms.Label();
            this.btnRestore = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnViewBackupLocation = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBackups)).BeginInit();
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
            this.dgvBackups.Location = new System.Drawing.Point(12, 29);
            this.dgvBackups.MultiSelect = false;
            this.dgvBackups.Name = "dgvBackups";
            this.dgvBackups.ReadOnly = true;
            this.dgvBackups.RowHeadersVisible = false;
            this.dgvBackups.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvBackups.Size = new System.Drawing.Size(560, 271);
            this.dgvBackups.TabIndex = 0;
            // 
            // lblBackupsHeader
            // 
            this.lblBackupsHeader.AutoSize = true;
            this.lblBackupsHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBackupsHeader.Location = new System.Drawing.Point(12, 13);
            this.lblBackupsHeader.Name = "lblBackupsHeader";
            this.lblBackupsHeader.Size = new System.Drawing.Size(124, 13);
            this.lblBackupsHeader.TabIndex = 1;
            this.lblBackupsHeader.Text = "Available Backups:";
            // 
            // btnRestore
            // 
            this.btnRestore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRestore.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRestore.Location = new System.Drawing.Point(416, 306);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(75, 23);
            this.btnRestore.TabIndex = 2;
            this.btnRestore.Text = "Restore";
            this.btnRestore.UseVisualStyleBackColor = true;
            this.btnRestore.Click += new System.EventHandler(this.btnRestore_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(497, 306);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnViewBackupLocation
            // 
            this.btnViewBackupLocation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnViewBackupLocation.Location = new System.Drawing.Point(12, 306);
            this.btnViewBackupLocation.Name = "btnViewBackupLocation";
            this.btnViewBackupLocation.Size = new System.Drawing.Size(124, 23);
            this.btnViewBackupLocation.TabIndex = 4;
            this.btnViewBackupLocation.Text = "View Backup Location";
            this.btnViewBackupLocation.UseVisualStyleBackColor = true;
            this.btnViewBackupLocation.Click += new System.EventHandler(this.btnViewBackupLocation_Click);
            // 
            // BackupForm
            // 
            this.AcceptButton = this.btnRestore;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(584, 341);
            this.Controls.Add(this.btnViewBackupLocation);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnRestore);
            this.Controls.Add(this.lblBackupsHeader);
            this.Controls.Add(this.dgvBackups);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 300);
            this.Name = "BackupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Backup Manager";
            ((System.ComponentModel.ISupportInitialize)(this.dgvBackups)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.DataGridView dgvBackups;
        private System.Windows.Forms.Label lblBackupsHeader;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnViewBackupLocation;
    }
}