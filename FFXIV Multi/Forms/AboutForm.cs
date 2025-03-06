using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace FFXIVClientManager.Forms
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            LoadAppInfo();
        }

        private void LoadAppInfo()
        {
            // Set application name and version
            lblAppName.Text = "FFXIV Client Manager";

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            lblVersion.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";

            // Set the build date based on the linked time/date stamp for the assembly
            DateTime buildDate = GetBuildDate();
            lblBuildDate.Text = $"Build Date: {buildDate:yyyy-MM-dd}";
        }

        private DateTime GetBuildDate()
        {
            // Get the build date from assembly information
            try
            {
                string filePath = Assembly.GetExecutingAssembly().Location;
                const int PeHeaderOffset = 60;
                const int LinkerTimestampOffset = 8;

                byte[] buffer = new byte[2048];

                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    stream.Read(buffer, 0, 2048);
                }

                int offset = BitConverter.ToInt32(buffer, PeHeaderOffset);
                int secondsSince1970 = BitConverter.ToInt32(buffer, offset + LinkerTimestampOffset);
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                return epoch.AddSeconds(secondsSince1970).ToLocalTime();
            }
            catch
            {
                // If we can't get the build date, use the current date
                return DateTime.Today;
            }
        }

        private void linkWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/yourusername/FFXIVClientManager");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening link: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblAppName = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblBuildDate = new System.Windows.Forms.Label();
            this.lblCopyright = new System.Windows.Forms.Label();
            this.linkWebsite = new System.Windows.Forms.LinkLabel();
            this.lblDescription = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(64, 64);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // lblAppName
            // 
            this.lblAppName.AutoSize = true;
            this.lblAppName.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAppName.Location = new System.Drawing.Point(82, 12);
            this.lblAppName.Name = "lblAppName";
            this.lblAppName.Size = new System.Drawing.Size(210, 24);
            this.lblAppName.TabIndex = 1;
            this.lblAppName.Text = "FFXIV Client Manager";
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(83, 36);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(69, 13);
            this.lblVersion.TabIndex = 2;
            this.lblVersion.Text = "Version 1.0.0";
            // 
            // lblBuildDate
            // 
            this.lblBuildDate.AutoSize = true;
            this.lblBuildDate.Location = new System.Drawing.Point(83, 53);
            this.lblBuildDate.Name = "lblBuildDate";
            this.lblBuildDate.Size = new System.Drawing.Size(123, 13);
            this.lblBuildDate.TabIndex = 3;
            this.lblBuildDate.Text = "Build Date: 2024-01-01";
            // 
            // lblCopyright
            // 
            this.lblCopyright.AutoSize = true;
            this.lblCopyright.Location = new System.Drawing.Point(12, 171);
            this.lblCopyright.Name = "lblCopyright";
            this.lblCopyright.Size = new System.Drawing.Size(163, 13);
            this.lblCopyright.TabIndex = 4;
            this.lblCopyright.Text = "© 2024 Your Name. All rights reserved.";
            // 
            // linkWebsite
            // 
            this.linkWebsite.AutoSize = true;
            this.linkWebsite.Location = new System.Drawing.Point(12, 188);
            this.linkWebsite.Name = "linkWebsite";
            this.linkWebsite.Size = new System.Drawing.Size(246, 13);
            this.linkWebsite.TabIndex = 5;
            this.linkWebsite.TabStop = true;
            this.linkWebsite.Text = "https://github.com/yourusername/FFXIVClientManager";
            this.linkWebsite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkWebsite_LinkClicked);
            // 
            // lblDescription
            // 
            this.lblDescription.Location = new System.Drawing.Point(12, 89);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(360, 73);
            this.lblDescription.TabIndex = 6;
            this.lblDescription.Text = resources.GetString("lblDescription.Text");
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(297, 183);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 7;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // AboutForm
            // 
            this.AcceptButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(384, 218);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.linkWebsite);
            this.Controls.Add(this.lblCopyright);
            this.Controls.Add(this.lblBuildDate);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.lblAppName);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About FFXIV Client Manager";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblAppName;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblBuildDate;
        private System.Windows.Forms.Label lblCopyright;
        private System.Windows.Forms.LinkLabel linkWebsite;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Button btnClose;
    }
}