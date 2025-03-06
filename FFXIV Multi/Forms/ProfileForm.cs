using System;
using System.IO;
using System.Windows.Forms;
using FFXIV_Multi.Models;
using FFXIVClientManager.Models;

namespace FFXIVClientManager.Forms
{
    public partial class ProfileForm : Form
    {
        public ClientProfile Profile { get; private set; }

        public ProfileForm(ClientProfile existingProfile = null)
        {
            InitializeComponent();

            if (existingProfile != null)
            {
                Profile = existingProfile;
                LoadProfileData();
                Text = $"Edit Profile - {Profile.ProfileName}";
            }
            else
            {
                Profile = new ClientProfile();

                // Set default paths
                string basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "FFXIVClientManager", "Clients", "NewProfile");

                txtConfigPath.Text = Path.Combine(basePath, "Config");
                txtPluginPath.Text = Path.Combine(basePath, "Plugins");

                chkDX11.Checked = true;
                chkEnableDalamud.Checked = true;
                chkAutoBackup.Checked = true;

                Text = "Create New Profile";
            }
        }

        private void LoadProfileData()
        {
            // General settings
            txtProfileName.Text = Profile.ProfileName;
            txtConfigPath.Text = Profile.ConfigPath;
            txtPluginPath.Text = Profile.PluginPath;
            txtGamePath.Text = Profile.GamePath;

            // Launch options
            chkSteam.Checked = Profile.IsSteam;
            chkDX11.Checked = Profile.ForceDX11;
            chkNoAutoLogin.Checked = Profile.NoAutoLogin;
            chkOTP.Checked = Profile.UseOTP;
            chkEnableDalamud.Checked = Profile.EnableDalamud;
            txtAdditionalArgs.Text = Profile.AdditionalArgs;

            // Character details
            txtCharacterName.Text = Profile.Character.CharacterName;
            txtWorld.Text = Profile.Character.WorldName;
            cboJobClass.Text = Profile.Character.JobClass;

            // Backup settings
            chkAutoBackup.Checked = Profile.AutoBackup;
        }

        private void SaveProfileData()
        {
            // General settings
            Profile.ProfileName = txtProfileName.Text.Trim();
            Profile.ConfigPath = txtConfigPath.Text.Trim();
            Profile.PluginPath = txtPluginPath.Text.Trim();
            Profile.GamePath = txtGamePath.Text.Trim();

            // Launch options
            Profile.IsSteam = chkSteam.Checked;
            Profile.ForceDX11 = chkDX11.Checked;
            Profile.NoAutoLogin = chkNoAutoLogin.Checked;
            Profile.UseOTP = chkOTP.Checked;
            Profile.EnableDalamud = chkEnableDalamud.Checked;
            Profile.AdditionalArgs = txtAdditionalArgs.Text.Trim();

            // Character details
            Profile.Character.CharacterName = txtCharacterName.Text.Trim();
            Profile.Character.WorldName = txtWorld.Text.Trim();
            Profile.Character.JobClass = cboJobClass.Text.Trim();

            // Backup settings
            Profile.AutoBackup = chkAutoBackup.Checked;
        }

        private void btnBrowseConfig_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Config Directory";
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(txtConfigPath.Text) && Directory.Exists(txtConfigPath.Text))
                    dialog.SelectedPath = txtConfigPath.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtConfigPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnBrowsePlugin_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Plugin Directory";
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(txtPluginPath.Text) && Directory.Exists(txtPluginPath.Text))
                    dialog.SelectedPath = txtPluginPath.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtPluginPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnBrowseGame_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select FFXIV Game Directory";
                dialog.ShowNewFolderButton = false;

                if (!string.IsNullOrEmpty(txtGamePath.Text) && Directory.Exists(txtGamePath.Text))
                    dialog.SelectedPath = txtGamePath.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtGamePath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProfileName.Text))
            {
                MessageBox.Show(
                    "Profile name is required.",
                    "Missing Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtProfileName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtConfigPath.Text))
            {
                MessageBox.Show(
                    "Configuration path is required.",
                    "Missing Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtConfigPath.Focus();
                return;
            }

            if (chkEnableDalamud.Checked && string.IsNullOrWhiteSpace(txtPluginPath.Text))
            {
                MessageBox.Show(
                    "Plugin path is required when Dalamud is enabled.",
                    "Missing Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtPluginPath.Focus();
                return;
            }

            SaveProfileData();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void chkEnableDalamud_CheckedChanged(object sender, EventArgs e)
        {
            // Enable/disable plugin path controls based on whether Dalamud is enabled
            txtPluginPath.Enabled = chkEnableDalamud.Checked;
            btnBrowsePlugin.Enabled = chkEnableDalamud.Checked;
            lblPluginPath.Enabled = chkEnableDalamud.Checked;
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
            this.lblProfileName = new System.Windows.Forms.Label();
            this.txtProfileName = new System.Windows.Forms.TextBox();
            this.lblConfigPath = new System.Windows.Forms.Label();
            this.txtConfigPath = new System.Windows.Forms.TextBox();
            this.btnBrowseConfig = new System.Windows.Forms.Button();
            this.lblPluginPath = new System.Windows.Forms.Label();
            this.txtPluginPath = new System.Windows.Forms.TextBox();
            this.btnBrowsePlugin = new System.Windows.Forms.Button();
            this.lblGamePath = new System.Windows.Forms.Label();
            this.txtGamePath = new System.Windows.Forms.TextBox();
            this.btnBrowseGame = new System.Windows.Forms.Button();
            this.chkSteam = new System.Windows.Forms.CheckBox();
            this.chkDX11 = new System.Windows.Forms.CheckBox();
            this.chkNoAutoLogin = new System.Windows.Forms.CheckBox();
            this.chkOTP = new System.Windows.Forms.CheckBox();
            this.chkEnableDalamud = new System.Windows.Forms.CheckBox();
            this.lblAdditionalArgs = new System.Windows.Forms.Label();
            this.txtAdditionalArgs = new System.Windows.Forms.TextBox();
            this.grpCharacter = new System.Windows.Forms.GroupBox();
            this.cboJobClass = new System.Windows.Forms.ComboBox();
            this.lblJobClass = new System.Windows.Forms.Label();
            this.txtWorld = new System.Windows.Forms.TextBox();
            this.lblWorld = new System.Windows.Forms.Label();
            this.txtCharacterName = new System.Windows.Forms.TextBox();
            this.lblCharacterName = new System.Windows.Forms.Label();
            this.grpLaunchOptions = new System.Windows.Forms.GroupBox();
            this.grpGeneral = new System.Windows.Forms.GroupBox();
            this.chkAutoBackup = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpCharacter.SuspendLayout();
            this.grpLaunchOptions.SuspendLayout();
            this.grpGeneral.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblProfileName
            // 
            this.lblProfileName.AutoSize = true;
            this.lblProfileName.Location = new System.Drawing.Point(16, 27);
            this.lblProfileName.Name = "lblProfileName";
            this.lblProfileName.Size = new System.Drawing.Size(71, 13);
            this.lblProfileName.TabIndex = 0;
            this.lblProfileName.Text = "Profile Name:";
            // 
            // txtProfileName
            // 
            this.txtProfileName.Location = new System.Drawing.Point(93, 24);
            this.txtProfileName.Name = "txtProfileName";
            this.txtProfileName.Size = new System.Drawing.Size(285, 20);
            this.txtProfileName.TabIndex = 1;
            // 
            // lblConfigPath
            // 
            this.lblConfigPath.AutoSize = true;
            this.lblConfigPath.Location = new System.Drawing.Point(19, 54);
            this.lblConfigPath.Name = "lblConfigPath";
            this.lblConfigPath.Size = new System.Drawing.Size(68, 13);
            this.lblConfigPath.TabIndex = 2;
            this.lblConfigPath.Text = "Config Path:";
            // 
            // txtConfigPath
            // 
            this.txtConfigPath.Location = new System.Drawing.Point(93, 51);
            this.txtConfigPath.Name = "txtConfigPath";
            this.txtConfigPath.Size = new System.Drawing.Size(285, 20);
            this.txtConfigPath.TabIndex = 3;
            // 
            // btnBrowseConfig
            // 
            this.btnBrowseConfig.Location = new System.Drawing.Point(384, 49);
            this.btnBrowseConfig.Name = "btnBrowseConfig";
            this.btnBrowseConfig.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseConfig.TabIndex = 4;
            this.btnBrowseConfig.Text = "Browse...";
            this.btnBrowseConfig.UseVisualStyleBackColor = true;
            this.btnBrowseConfig.Click += new System.EventHandler(this.btnBrowseConfig_Click);
            // 
            // lblPluginPath
            // 
            this.lblPluginPath.AutoSize = true;
            this.lblPluginPath.Location = new System.Drawing.Point(24, 81);
            this.lblPluginPath.Name = "lblPluginPath";
            this.lblPluginPath.Size = new System.Drawing.Size(63, 13);
            this.lblPluginPath.TabIndex = 5;
            this.lblPluginPath.Text = "Plugin Path:";
            // 
            // txtPluginPath
            // 
            this.txtPluginPath.Location = new System.Drawing.Point(93, 78);
            this.txtPluginPath.Name = "txtPluginPath";
            this.txtPluginPath.Size = new System.Drawing.Size(285, 20);
            this.txtPluginPath.TabIndex = 6;
            // 
            // btnBrowsePlugin
            // 
            this.btnBrowsePlugin.Location = new System.Drawing.Point(384, 76);
            this.btnBrowsePlugin.Name = "btnBrowsePlugin";
            this.btnBrowsePlugin.Size = new System.Drawing.Size(75, 23);
            this.btnBrowsePlugin.TabIndex = 7;
            this.btnBrowsePlugin.Text = "Browse...";
            this.btnBrowsePlugin.UseVisualStyleBackColor = true;
            this.btnBrowsePlugin.Click += new System.EventHandler(this.btnBrowsePlugin_Click);
            // 
            // lblGamePath
            // 
            this.lblGamePath.AutoSize = true;
            this.lblGamePath.Location = new System.Drawing.Point(24, 108);
            this.lblGamePath.Name = "lblGamePath";
            this.lblGamePath.Size = new System.Drawing.Size(65, 13);
            this.lblGamePath.TabIndex = 8;
            this.lblGamePath.Text = "Game Path:";
            // 
            // txtGamePath
            // 
            this.txtGamePath.Location = new System.Drawing.Point(93, 105);
            this.txtGamePath.Name = "txtGamePath";
            this.txtGamePath.Size = new System.Drawing.Size(285, 20);
            this.txtGamePath.TabIndex = 9;
            // 
            // btnBrowseGame
            // 
            this.btnBrowseGame.Location = new System.Drawing.Point(384, 103);
            this.btnBrowseGame.Name = "btnBrowseGame";
            this.btnBrowseGame.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseGame.TabIndex = 10;
            this.btnBrowseGame.Text = "Browse...";
            this.btnBrowseGame.UseVisualStyleBackColor = true;
            this.btnBrowseGame.Click += new System.EventHandler(this.btnBrowseGame_Click);
            // 
            // chkSteam
            // 
            this.chkSteam.AutoSize = true;
            this.chkSteam.Location = new System.Drawing.Point(17, 22);
            this.chkSteam.Name = "chkSteam";
            this.chkSteam.Size = new System.Drawing.Size(101, 17);
            this.chkSteam.TabIndex = 11;
            this.chkSteam.Text = "Steam Account";
            this.chkSteam.UseVisualStyleBackColor = true;
            // 
            // chkDX11
            // 
            this.chkDX11.AutoSize = true;
            this.chkDX11.Location = new System.Drawing.Point(17, 45);
            this.chkDX11.Name = "chkDX11";
            this.chkDX11.Size = new System.Drawing.Size(102, 17);
            this.chkDX11.TabIndex = 12;
            this.chkDX11.Text = "Force DirectX 11";
            this.chkDX11.UseVisualStyleBackColor = true;
            // 
            // chkNoAutoLogin
            // 
            this.chkNoAutoLogin.AutoSize = true;
            this.chkNoAutoLogin.Location = new System.Drawing.Point(17, 68);
            this.chkNoAutoLogin.Name = "chkNoAutoLogin";
            this.chkNoAutoLogin.Size = new System.Drawing.Size(121, 17);
            this.chkNoAutoLogin.TabIndex = 13;
            this.chkNoAutoLogin.Text = "Disable Auto-Login";
            this.chkNoAutoLogin.UseVisualStyleBackColor = true;
            // 
            // chkOTP
            // 
            this.chkOTP.AutoSize = true;
            this.chkOTP.Location = new System.Drawing.Point(130, 22);
            this.chkOTP.Name = "chkOTP";
            this.chkOTP.Size = new System.Drawing.Size(90, 17);
            this.chkOTP.TabIndex = 14;
            this.chkOTP.Text = "Enable OTP";
            this.chkOTP.UseVisualStyleBackColor = true;
            // 
            // chkEnableDalamud
            // 
            this.chkEnableDalamud.AutoSize = true;
            this.chkEnableDalamud.Location = new System.Drawing.Point(130, 45);
            this.chkEnableDalamud.Name = "chkEnableDalamud";
            this.chkEnableDalamud.Size = new System.Drawing.Size(107, 17);
            this.chkEnableDalamud.TabIndex = 15;
            this.chkEnableDalamud.Text = "Enable Dalamud";
            this.chkEnableDalamud.UseVisualStyleBackColor = true;
            this.chkEnableDalamud.CheckedChanged += new System.EventHandler(this.chkEnableDalamud_CheckedChanged);
            // 
            // lblAdditionalArgs
            // 
            this.lblAdditionalArgs.AutoSize = true;
            this.lblAdditionalArgs.Location = new System.Drawing.Point(14, 98);
            this.lblAdditionalArgs.Name = "lblAdditionalArgs";
            this.lblAdditionalArgs.Size = new System.Drawing.Size(122, 13);
            this.lblAdditionalArgs.TabIndex = 16;
            this.lblAdditionalArgs.Text = "Additional Args (optional):";
            // 
            // txtAdditionalArgs
            // 
            this.txtAdditionalArgs.Location = new System.Drawing.Point(17, 114);
            this.txtAdditionalArgs.Name = "txtAdditionalArgs";
            this.txtAdditionalArgs.Size = new System.Drawing.Size(422, 20);
            this.txtAdditionalArgs.TabIndex = 17;
            // 
            // grpCharacter
            // 
            this.grpCharacter.Controls.Add(this.cboJobClass);
            this.grpCharacter.Controls.Add(this.lblJobClass);
            this.grpCharacter.Controls.Add(this.txtWorld);
            this.grpCharacter.Controls.Add(this.lblWorld);
            this.grpCharacter.Controls.Add(this.txtCharacterName);
            this.grpCharacter.Controls.Add(this.lblCharacterName);
            this.grpCharacter.Location = new System.Drawing.Point(12, 311);
            this.grpCharacter.Name = "grpCharacter";
            this.grpCharacter.Size = new System.Drawing.Size(471, 91);
            this.grpCharacter.TabIndex = 18;
            this.grpCharacter.TabStop = false;
            this.grpCharacter.Text = "Character Details (Optional)";
            // 
            // cboJobClass
            // 
            this.cboJobClass.FormattingEnabled = true;
            this.cboJobClass.Items.AddRange(new object[] {
            "PLD",
            "WAR",
            "DRK",
            "GNB",
            "WHM",
            "SCH",
            "AST",
            "SGE",
            "MNK",
            "DRG",
            "NIN",
            "SAM",
            "RPR",
            "BRD",
            "MCH",
            "DNC",
            "BLM",
            "SMN",
            "RDM",
            "BLU"});
            this.cboJobClass.Location = new System.Drawing.Point(368, 26);
            this.cboJobClass.Name = "cboJobClass";
            this.cboJobClass.Size = new System.Drawing.Size(91, 21);
            this.cboJobClass.TabIndex = 5;
            // 
            // lblJobClass
            // 
            this.lblJobClass.AutoSize = true;
            this.lblJobClass.Location = new System.Drawing.Point(340, 29);
            this.lblJobClass.Name = "lblJobClass";
            this.lblJobClass.Size = new System.Drawing.Size(27, 13);
            this.lblJobClass.TabIndex = 4;
            this.lblJobClass.Text = "Job:";
            // 
            // txtWorld
            // 
            this.txtWorld.Location = new System.Drawing.Point(222, 26);
            this.txtWorld.Name = "txtWorld";
            this.txtWorld.Size = new System.Drawing.Size(112, 20);
            this.txtWorld.TabIndex = 3;
            // 
            // lblWorld
            // 
            this.lblWorld.AutoSize = true;
            this.lblWorld.Location = new System.Drawing.Point(185, 29);
            this.lblWorld.Name = "lblWorld";
            this.lblWorld.Size = new System.Drawing.Size(38, 13);
            this.lblWorld.TabIndex = 2;
            this.lblWorld.Text = "World:";
            // 
            // txtCharacterName
            // 
            this.txtCharacterName.Location = new System.Drawing.Point(63, 26);
            this.txtCharacterName.Name = "txtCharacterName";
            this.txtCharacterName.Size = new System.Drawing.Size(116, 20);
            this.txtCharacterName.TabIndex = 1;
            // 
            // lblCharacterName
            // 
            this.lblCharacterName.AutoSize = true;
            this.lblCharacterName.Location = new System.Drawing.Point(6, 29);
            this.lblCharacterName.Name = "lblCharacterName";
            this.lblCharacterName.Size = new System.Drawing.Size(57, 13);
            this.lblCharacterName.TabIndex = 0;
            this.lblCharacterName.Text = "Character:";
            // 
            // grpLaunchOptions
            // 
            this.grpLaunchOptions.Controls.Add(this.chkSteam);
            this.grpLaunchOptions.Controls.Add(this.chkDX11);
            this.grpLaunchOptions.Controls.Add(this.txtAdditionalArgs);
            this.grpLaunchOptions.Controls.Add(this.chkNoAutoLogin);
            this.grpLaunchOptions.Controls.Add(this.lblAdditionalArgs);
            this.grpLaunchOptions.Controls.Add(this.chkOTP);
            this.grpLaunchOptions.Controls.Add(this.chkEnableDalamud);
            this.grpLaunchOptions.Location = new System.Drawing.Point(12, 156);
            this.grpLaunchOptions.Name = "grpLaunchOptions";
            this.grpLaunchOptions.Size = new System.Drawing.Size(471, 149);
            this.grpLaunchOptions.TabIndex = 19;
            this.grpLaunchOptions.TabStop = false;
            this.grpLaunchOptions.Text = "Launch Options";
            // 
            // grpGeneral
            // 
            this.grpGeneral.Controls.Add(this.chkAutoBackup);
            this.grpGeneral.Controls.Add(this.lblProfileName);
            this.grpGeneral.Controls.Add(this.txtProfileName);
            this.grpGeneral.Controls.Add(this.lblConfigPath);
            this.grpGeneral.Controls.Add(this.txtConfigPath);
            this.grpGeneral.Controls.Add(this.btnBrowseConfig);
            this.grpGeneral.Controls.Add(this.lblPluginPath);
            this.grpGeneral.Controls.Add(this.txtPluginPath);
            this.grpGeneral.Controls.Add(this.btnBrowsePlugin);
            this.grpGeneral.Controls.Add(this.lblGamePath);
            this.grpGeneral.Controls.Add(this.txtGamePath);
            this.grpGeneral.Controls.Add(this.btnBrowseGame);
            this.grpGeneral.Location = new System.Drawing.Point(12, 12);
            this.grpGeneral.Name = "grpGeneral";
            this.grpGeneral.Size = new System.Drawing.Size(471, 138);
            this.grpGeneral.TabIndex = 20;
            this.grpGeneral.TabStop = false;
            this.grpGeneral.Text = "General Settings";
            // 
            // chkAutoBackup
            // 
            this.chkAutoBackup.AutoSize = true;
            this.chkAutoBackup.Location = new System.Drawing.Point(384, 25);
            this.chkAutoBackup.Name = "chkAutoBackup";
            this.chkAutoBackup.Size = new System.Drawing.Size(88, 17);
            this.chkAutoBackup.TabIndex = 11;
            this.chkAutoBackup.Text = "Auto Backup";
            this.chkAutoBackup.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(164, 415);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 21;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(245, 415);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 22;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ProfileForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(495, 450);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.grpGeneral);
            this.Controls.Add(this.grpLaunchOptions);
            this.Controls.Add(this.grpCharacter);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProfileForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Profile Settings";
            this.grpCharacter.ResumeLayout(false);
            this.grpCharacter.PerformLayout();
            this.grpLaunchOptions.ResumeLayout(false);
            this.grpLaunchOptions.PerformLayout();
            this.grpGeneral.ResumeLayout(false);
            this.grpGeneral.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.Label lblProfileName;
        private System.Windows.Forms.TextBox txtProfileName;
        private System.Windows.Forms.Label lblConfigPath;
        private System.Windows.Forms.TextBox txtConfigPath;
        private System.Windows.Forms.Button btnBrowseConfig;
        private System.Windows.Forms.Label lblPluginPath;
        private System.Windows.Forms.TextBox txtPluginPath;
        private System.Windows.Forms.Button btnBrowsePlugin;
        private System.Windows.Forms.Label lblGamePath;
        private System.Windows.Forms.TextBox txtGamePath;
        private System.Windows.Forms.Button btnBrowseGame;
        private System.Windows.Forms.CheckBox chkSteam;
        private System.Windows.Forms.CheckBox chkDX11;
        private System.Windows.Forms.CheckBox chkNoAutoLogin;
        private System.Windows.Forms.CheckBox chkOTP;
        private System.Windows.Forms.CheckBox chkEnableDalamud;
        private System.Windows.Forms.Label lblAdditionalArgs;
        private System.Windows.Forms.TextBox txtAdditionalArgs;
        private System.Windows.Forms.GroupBox grpCharacter;
        private System.Windows.Forms.ComboBox cboJobClass;
        private System.Windows.Forms.Label lblJobClass;
        private System.Windows.Forms.TextBox txtWorld;
        private System.Windows.Forms.Label lblWorld;
        private System.Windows.Forms.TextBox txtCharacterName;
        private System.Windows.Forms.Label lblCharacterName;
        private System.Windows.Forms.GroupBox grpLaunchOptions;
        private System.Windows.Forms.GroupBox grpGeneral;
        private System.Windows.Forms.CheckBox chkAutoBackup;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}