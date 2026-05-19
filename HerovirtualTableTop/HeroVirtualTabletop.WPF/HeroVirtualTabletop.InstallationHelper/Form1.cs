using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Framework.WPF.Extensions;
using System.Diagnostics;

namespace HeroVirtualTabletop.InstallationHelper
{
    public partial class Form1 : Form
    {
        private bool installRequired = false;
        private string gameDir = "";
        private bool gameExtracted = false;
        private bool soundExtracted = false;
        private bool costumesExtracted = false;
        private bool hookCostumeCopied = false;
        private bool repoCopied = false;
        private bool success = false;
        private string installDir = "";
        private string cohZipLocation = "cityofheroes.zip";
        private string costumesZipLocation = "costumes.zip";
        private string soundZipLocation = "sound.zip";
        private string repoLocation = "CrowdRepo.data";
        private string hookCostumeDllLocation = "HookCostume.xyz";
        private string xnaMsiLocation = "xnafx40_redist.msi";
        public Form1(string installDir)
        {
            InitializeComponent();
            this.installDir = installDir;
            if(!string.IsNullOrEmpty(installDir))
            {
                cohZipLocation = Path.Combine(installDir, cohZipLocation);
                costumesZipLocation = Path.Combine(installDir, costumesZipLocation);
                soundZipLocation = Path.Combine(installDir, soundZipLocation);
                repoLocation = Path.Combine(installDir, repoLocation);
                hookCostumeDllLocation = Path.Combine(installDir, hookCostumeDllLocation);
                xnaMsiLocation = Path.Combine(installDir, xnaMsiLocation);
            }
        }

        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonAlreadyInstalled.Checked)
            {
                buttonCurrentLocation.Text = "Set Current Location";
                installRequired = false;
            }
            else
            {
                buttonCurrentLocation.Text = "Set Install Location";
                installRequired = true;
            }
        }

        private void buttonCurrentLocation_Click(object sender, EventArgs e)
        {
            var dialogResult = folderBrowserDialog1.ShowDialog();
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                if (!installRequired && !File.Exists(Path.Combine(folderBrowserDialog1.SelectedPath, "cityofheroes.exe")))
                {
                    MessageBox.Show("Invalid directory! Please select the directory where cityofheroes.exe is located");
                }
                else
                {
                    gameDir = textBoxLocation.Text = folderBrowserDialog1.SelectedPath;
                    buttonOK.Enabled = true;
                }
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            ExtractResources();
        }

        private void buttonLocateArchive_Click(object sender, EventArgs e)
        {
            var dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                var zipFile = ZipFile.OpenRead(openFileDialog1.FileName);
                bool validZip = zipFile.Entries.Any(entry => entry.Name.EndsWith("cityofheroes.exe"));
                if (!validZip)
                {
                    MessageBox.Show("Invalid archive! Please select one that contains City of Heroes files.");
                }
                else
                {
                    buttonLocateCOHArchive.Visible = false;
                    ExtractGame(openFileDialog1.FileName);
                }
            }
        }

        private void buttonLocateSoundArchive_Click(object sender, EventArgs e)
        {
            var dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                var zipFile = ZipFile.OpenRead(openFileDialog1.FileName);
                bool validZip = zipFile.Entries.Any(entry => entry.FullName.EndsWith("sound/"));
                if (!validZip)
                {
                    MessageBox.Show("Invalid archive! Please select proper sound archive.");
                }
                else
                {
                    buttonLocateSoundArchive.Visible = false;
                    ExtractSound(openFileDialog1.FileName);
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] args = e.Argument as string[];
            if(args != null && args.Length >= 2)
            {
                string archiveObj = args[0];
                string filePathOrDirectory = args[1];
                if (archiveObj == "Game" || archiveObj == "Sound" || archiveObj == "Costumes")
                {
                    ZipArchive archive = ZipFile.OpenRead(filePathOrDirectory);
                    archive.ExtractToDirectory(gameDir, true);
                }
                else if(archiveObj == "Repo")
                {
                    string repoFilePath = args[2];
                    File.Copy(repoFilePath, filePathOrDirectory, true);
                }
                e.Result = archiveObj;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // check error, check cancel, then use result
            if (e.Error != null)
            {
                // handle the error
            }
            else if (e.Cancelled)
            {
                // handle cancellation
            }
            else
            {
                Action d = delegate()
                {
                    string result = (string)e.Result;
                    if (result == "Game")
                    {
                        gameExtracted = true;
                        ShowProgress("Game", false);
                    }
                    else if (result == "Sound")
                    {
                        soundExtracted = true;
                        ShowProgress("Sound", false);
                    }
                    else if (result == "Repo")
                    {
                        repoCopied = true;
                        ShowProgress("Repo", false);
                    }
                    else if (result == "Costumes")
                    {
                        costumesExtracted = true;
                        ShowProgress("Costumes", false);
                    }
                    ExtractResources();
                };

                this.Invoke(d);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!success)
            {
                var dialogRes = MessageBox.Show(this, "Closing this form without completing the actions might cause the Hero Virual Desktop application to not run properly. Are you sure?", "Close", MessageBoxButtons.YesNo);
                if (dialogRes == System.Windows.Forms.DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void ExtractResources()
        {
            if (!string.IsNullOrEmpty(gameDir))
            {
                if (!installRequired)
                    gameExtracted = true;
                radioButtonAlreadyInstalled.Enabled = false;
                radioButtonInstallCOH.Enabled = false;
                buttonCurrentLocation.Enabled = false;
                buttonOK.Enabled = false;
                if (installRequired && !gameExtracted)
                    ExtractGame();
                else if (gameExtracted && !soundExtracted)
                    ExtractSound();
                else if (soundExtracted && !repoCopied)
                    CopyRepo();
                else if (repoCopied && !costumesExtracted)
                    ExtractCostumes();
                else if (!hookCostumeCopied)
                    CopyHookCostumeDll();
            }
            else
            {
                MessageBox.Show("Pleae select game directory first");
            }
            if (gameExtracted && soundExtracted && repoCopied && hookCostumeCopied && costumesExtracted)
            {
                success = true;
                try
                {
                    if(File.Exists(xnaMsiLocation))
                        Process.Start(xnaMsiLocation); 
                }
                catch
                {

                }
                if (MessageBox.Show("All the files were copied successfully. You can now launch Hero Virtual Desktop. Enjoy!") == System.Windows.Forms.DialogResult.OK) ;
                    Application.Exit();
            }
        }

        private void ExtractGame(string archiveLocation = null)
        {
            string archivePath = archiveLocation ?? cohZipLocation;
            if (!File.Exists(archivePath))
            {
                MessageBox.Show(this, "City of Heroes archive was not found. Please locate it.");
                buttonLocateCOHArchive.Visible = true;
            }
            else
            {
                ShowProgress("Game", true);
                backgroundWorker1.RunWorkerAsync(new string[]{"Game", archivePath});
            }
        }

        private string GetDataDirectory()
        {
            string dataDir = Path.Combine(gameDir, "data");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);
            return dataDir;
        }

        private void CopyRepo()
        {
            string dataDir = GetDataDirectory();
            string crowdRepoFileName = "CrowdRepo.data";
            string destFileName = Path.Combine(dataDir, crowdRepoFileName);
            if (File.Exists(repoLocation))
            {
                ShowProgress("Repo", true);
                backgroundWorker1.RunWorkerAsync(new string[] { "Repo", destFileName, repoLocation });
            }
            else
                repoCopied = true;
        }

        private void ExtractSound(string archiveLocation = null)
        {
            string archivePath = archiveLocation ?? soundZipLocation;
            if (!File.Exists(archivePath))
            {
                MessageBox.Show(this, "Sound archive was not found. Please locate it.");
                buttonLocateSoundArchive.Visible = true;
            }
            else
            {
                ShowProgress("Sound", true);
                backgroundWorker1.RunWorkerAsync(new string[]{"Sound", archivePath});
            }
        }

        private void ExtractCostumes(string archiveLocation = null)
        {
            string archivePath = archiveLocation ?? costumesZipLocation;
            if (File.Exists(archivePath))
            {
                ShowProgress("Costumes", true);
                backgroundWorker1.RunWorkerAsync(new string[] { "Costumes", archivePath });
            }
            else
            {
                costumesExtracted = true;
            }
        }

        private void CopyHookCostumeDll()
        {
            string hookCostumeFileName = "HookCostume.xyz";
            string hookCostumeDllFileName = "HookCostume.dll";
            string hookCostumeExisting = Path.Combine(gameDir, hookCostumeDllFileName);
            if (File.Exists(hookCostumeExisting))
                File.Delete(hookCostumeExisting);
            string destFileName = Path.Combine(gameDir, hookCostumeFileName);
            if (File.Exists(hookCostumeDllLocation))
            {
                File.Copy(hookCostumeDllLocation, destFileName, true);
                File.Move(destFileName, Path.ChangeExtension(destFileName, ".dll")); 
                hookCostumeCopied = true;
            }
            else
                hookCostumeCopied = true;
        }

        private void ShowProgress(string gameObj, bool show)
        {
            labelProgress.Visible = show;
            if (gameObj == "Game")
                labelProgress.Text = "Extracting Game...";
            else if (gameObj == "Sound")
                labelProgress.Text = "Extracting Sound...";
            else if (gameObj == "Repo")
                labelProgress.Text = "Copying Crowds Data...";
            else if (gameObj == "Costumes")
                labelProgress.Text = "Extracting Costumes...";
            progressBar1.Visible = show;
            buttonLocateCOHArchive.Enabled = !show;
            buttonLocateSoundArchive.Enabled = !show;
        }
    }
}
