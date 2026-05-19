namespace HeroVirtualTabletop.InstallationHelper
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.radioButtonAlreadyInstalled = new System.Windows.Forms.RadioButton();
            this.radioButtonInstallCOH = new System.Windows.Forms.RadioButton();
            this.buttonCurrentLocation = new System.Windows.Forms.Button();
            this.textBoxLocation = new System.Windows.Forms.TextBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.buttonLocateCOHArchive = new System.Windows.Forms.Button();
            this.buttonLocateSoundArchive = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.labelProgress = new System.Windows.Forms.Label();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(556, 42);
            this.label1.TabIndex = 0;
            this.label1.Text = "To complete setup, some files need to be copied to your CIty of Heroes game direc" +
    "tory. Please choose one of the following options";
            // 
            // radioButtonAlreadyInstalled
            // 
            this.radioButtonAlreadyInstalled.AutoSize = true;
            this.radioButtonAlreadyInstalled.Location = new System.Drawing.Point(15, 50);
            this.radioButtonAlreadyInstalled.Name = "radioButtonAlreadyInstalled";
            this.radioButtonAlreadyInstalled.Size = new System.Drawing.Size(239, 21);
            this.radioButtonAlreadyInstalled.TabIndex = 1;
            this.radioButtonAlreadyInstalled.TabStop = true;
            this.radioButtonAlreadyInstalled.Text = "City of Heroes is already installed";
            this.radioButtonAlreadyInstalled.UseVisualStyleBackColor = true;
            this.radioButtonAlreadyInstalled.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioButtonInstallCOH
            // 
            this.radioButtonInstallCOH.AutoSize = true;
            this.radioButtonInstallCOH.Location = new System.Drawing.Point(15, 77);
            this.radioButtonInstallCOH.Name = "radioButtonInstallCOH";
            this.radioButtonInstallCOH.Size = new System.Drawing.Size(158, 21);
            this.radioButtonInstallCOH.TabIndex = 2;
            this.radioButtonInstallCOH.TabStop = true;
            this.radioButtonInstallCOH.Text = "Install City of Heroes";
            this.radioButtonInstallCOH.UseVisualStyleBackColor = true;
            this.radioButtonInstallCOH.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // buttonCurrentLocation
            // 
            this.buttonCurrentLocation.Location = new System.Drawing.Point(18, 106);
            this.buttonCurrentLocation.Name = "buttonCurrentLocation";
            this.buttonCurrentLocation.Size = new System.Drawing.Size(155, 30);
            this.buttonCurrentLocation.TabIndex = 4;
            this.buttonCurrentLocation.Text = "Set Current Location";
            this.buttonCurrentLocation.UseVisualStyleBackColor = true;
            this.buttonCurrentLocation.Click += new System.EventHandler(this.buttonCurrentLocation_Click);
            // 
            // textBoxLocation
            // 
            this.textBoxLocation.Location = new System.Drawing.Point(179, 110);
            this.textBoxLocation.Name = "textBoxLocation";
            this.textBoxLocation.ReadOnly = true;
            this.textBoxLocation.Size = new System.Drawing.Size(396, 22);
            this.textBoxLocation.TabIndex = 5;
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(506, 141);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(69, 30);
            this.buttonOK.TabIndex = 7;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "Archive Files|*.zip;*.rar;*.7z";
            // 
            // buttonLocateCOHArchive
            // 
            this.buttonLocateCOHArchive.Location = new System.Drawing.Point(277, 141);
            this.buttonLocateCOHArchive.Name = "buttonLocateCOHArchive";
            this.buttonLocateCOHArchive.Size = new System.Drawing.Size(206, 30);
            this.buttonLocateCOHArchive.TabIndex = 8;
            this.buttonLocateCOHArchive.Text = "Locate City of Heroes Archive";
            this.buttonLocateCOHArchive.UseVisualStyleBackColor = true;
            this.buttonLocateCOHArchive.Visible = false;
            this.buttonLocateCOHArchive.Click += new System.EventHandler(this.buttonLocateArchive_Click);
            // 
            // buttonLocateSoundArchive
            // 
            this.buttonLocateSoundArchive.Location = new System.Drawing.Point(65, 142);
            this.buttonLocateSoundArchive.Name = "buttonLocateSoundArchive";
            this.buttonLocateSoundArchive.Size = new System.Drawing.Size(206, 30);
            this.buttonLocateSoundArchive.TabIndex = 9;
            this.buttonLocateSoundArchive.Text = "Locate Sound Archive";
            this.buttonLocateSoundArchive.UseVisualStyleBackColor = true;
            this.buttonLocateSoundArchive.Visible = false;
            this.buttonLocateSoundArchive.Click += new System.EventHandler(this.buttonLocateSoundArchive_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(450, 50);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(100, 23);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 10;
            this.progressBar1.Visible = false;
            // 
            // labelProgress
            // 
            this.labelProgress.AutoSize = true;
            this.labelProgress.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelProgress.Location = new System.Drawing.Point(320, 52);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(124, 17);
            this.labelProgress.TabIndex = 11;
            this.labelProgress.Text = "Extracting Game...";
            this.labelProgress.Visible = false;
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(580, 176);
            this.Controls.Add(this.labelProgress);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.buttonLocateSoundArchive);
            this.Controls.Add(this.buttonLocateCOHArchive);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.textBoxLocation);
            this.Controls.Add(this.buttonCurrentLocation);
            this.Controls.Add(this.radioButtonInstallCOH);
            this.Controls.Add(this.radioButtonAlreadyInstalled);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Hero Virtual Desktop Setup";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radioButtonAlreadyInstalled;
        private System.Windows.Forms.RadioButton radioButtonInstallCOH;
        private System.Windows.Forms.Button buttonCurrentLocation;
        private System.Windows.Forms.TextBox textBoxLocation;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button buttonLocateCOHArchive;
        private System.Windows.Forms.Button buttonLocateSoundArchive;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label labelProgress;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
    }
}

