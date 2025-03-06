namespace FFXIVClientManager.Forms
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // UI control declarations
        private System.Windows.Forms.TextBox txtXIVLauncherPath;
        private System.Windows.Forms.TextBox txtBackupPath;
        private System.Windows.Forms.TextBox txtLogPath;
        private System.Windows.Forms.NumericUpDown numLaunchDelay;
        private System.Windows.Forms.CheckBox chkBackupPlugins;
        private System.Windows.Forms.CheckBox chkRestorePlugins;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblXIVLauncherPath;
        private System.Windows.Forms.Label lblBackupPath;
        private System.Windows.Forms.Label lblLogPath;
        private System.Windows.Forms.Label lblLaunchDelay;
        private System.Windows.Forms.Label lblOptions;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Method required for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtXIVLauncherPath = new System.Windows.Forms.TextBox();
            this.txtBackupPath = new System.Windows.Forms.TextBox();
            this.txtLogPath = new System.Windows.Forms.TextBox();
            this.numLaunchDelay = new System.Windows.Forms.NumericUpDown();
            this.chkBackupPlugins = new System.Windows.Forms.CheckBox();
            this.chkRestorePlugins = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblXIVLauncherPath = new System.Windows.Forms.Label();
            this.lblBackupPath = new System.Windows.Forms.Label();
            this.lblLogPath = new System.Windows.Forms.Label();
            this.lblLaunchDelay = new System.Windows.Forms.Label();
            this.lblOptions = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numLaunchDelay)).BeginInit();
            this.SuspendLayout();
            // 
            // lblXIVLauncherPath
            // 
            this.lblXIVLauncherPath.AutoSize = true;
            this.lblXIVLauncherPath.Location = new System.Drawing.Point(12, 15);
            this.lblXIVLauncherPath.Name = "lblXIVLauncherPath";
            this.lblXIVLauncherPath.Size = new System.Drawing.Size(100, 15);
            this.lblXIVLauncherPath.TabIndex = 0;
            this.lblXIVLauncherPath.Text = "XIVLauncher Path:";
            // 
            // txtXIVLauncherPath
            // 
            this.txtXIVLauncherPath.Location = new System.Drawing.Point(130, 12);
            this.txtXIVLauncherPath.Name = "txtXIVLauncherPath";
            this.txtXIVLauncherPath.Size = new System.Drawing.Size(300, 23);
            this.txtXIVLauncherPath.TabIndex = 1;
            // 
            // lblBackupPath
            // 
            this.lblBackupPath.AutoSize = true;
            this.lblBackupPath.Location = new System.Drawing.Point(12, 50);
            this.lblBackupPath.Name = "lblBackupPath";
            this.lblBackupPath.Size = new System.Drawing.Size(78, 15);
            this.lblBackupPath.TabIndex = 2;
            this.lblBackupPath.Text = "Backup Path:";
            // 
            // txtBackupPath
            // 
            this.txtBackupPath.Location = new System.Drawing.Point(130, 47);
            this.txtBackupPath.Name = "txtBackupPath";
            this.txtBackupPath.Size = new System.Drawing.Size(300, 23);
            this.txtBackupPath.TabIndex = 3;
            // 
            // lblLogPath
            // 
            this.lblLogPath.AutoSize = true;
            this.lblLogPath.Location = new System.Drawing.Point(12, 85);
            this.lblLogPath.Name = "lblLogPath";
            this.lblLogPath.Size = new System.Drawing.Size(56, 15);
            this.lblLogPath.TabIndex = 4;
            this.lblLogPath.Text = "Log Path:";
            // 
            // txtLogPath
            // 
            this.txtLogPath.Location = new System.Drawing.Point(130, 82);
            this.txtLogPath.Name = "txtLogPath";
            this.txtLogPath.Size = new System.Drawing.Size(300, 23);
            this.txtLogPath.TabIndex = 5;
            // 
            // lblLaunchDelay
            // 
            this.lblLaunchDelay.AutoSize = true;
            this.lblLaunchDelay.Location = new System.Drawing.Point(12, 120);
            this.lblLaunchDelay.Name = "lblLaunchDelay";
            this.lblLaunchDelay.Size = new System.Drawing.Size(77, 15);
            this.lblLaunchDelay.TabIndex = 6;
            this.lblLaunchDelay.Text = "Launch Delay:";
            // 
            // numLaunchDelay
            // 
            this.numLaunchDelay.Location = new System.Drawing.Point(130, 118);
            this.numLaunchDelay.Name = "numLaunchDelay";
            this.numLaunchDelay.Size = new System.Drawing.Size(60, 23);
            this.numLaunchDelay.TabIndex = 7;
            // 
            // lblOptions
            // 
            this.lblOptions.AutoSize = true;
            this.lblOptions.Location = new System.Drawing.Point(12, 155);
            this.lblOptions.Name = "lblOptions";
            this.lblOptions.Size = new System.Drawing.Size(54, 15);
            this.lblOptions.TabIndex = 8;
            this.lblOptions.Text = "Options:";
            // 
            // chkBackupPlugins
            // 
            this.chkBackupPlugins.AutoSize = true;
            this.chkBackupPlugins.Location = new System.Drawing.Point(130, 153);
            this.chkBackupPlugins.Name = "chkBackupPlugins";
            this.chkBackupPlugins.Size = new System.Drawing.Size(122, 19);
            this.chkBackupPlugins.TabIndex = 9;
            this.chkBackupPlugins.Text = "Backup Plugins";
            this.chkBackupPlugins.UseVisualStyleBackColor = true;
            // 
            // chkRestorePlugins
            // 
            this.chkRestorePlugins.AutoSize = true;
            this.chkRestorePlugins.Location = new System.Drawing.Point(130, 178);
            this.chkRestorePlugins.Name = "chkRestorePlugins";
            this.chkRestorePlugins.Size = new System.Drawing.Size(123, 19);
            this.chkRestorePlugins.TabIndex = 10;
            this.chkRestorePlugins.Text = "Restore Plugins";
            this.chkRestorePlugins.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(274, 220);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 11;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(355, 220);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(442, 255);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.chkRestorePlugins);
            this.Controls.Add(this.chkBackupPlugins);
            this.Controls.Add(this.lblOptions);
            this.Controls.Add(this.numLaunchDelay);
            this.Controls.Add(this.lblLaunchDelay);
            this.Controls.Add(this.txtLogPath);
            this.Controls.Add(this.lblLogPath);
            this.Controls.Add(this.txtBackupPath);
            this.Controls.Add(this.lblBackupPath);
            this.Controls.Add(this.txtXIVLauncherPath);
            this.Controls.Add(this.lblXIVLauncherPath);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)(this.numLaunchDelay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
