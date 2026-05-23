using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Utility;

namespace Module.UnitTest.Crowds
{
    // ========================================================================
    // STORY: Create Daily Backup of Crowd Repository
    //
    // SBE scenarios:
    //  - First save of the day creates a dated backup before overwriting
    //    (already covered in TestSaveDirtyCrowdsToSourceFiles; included here
    //     for completeness with the DailyBackup-specific invariant variants)
    //  - A second save on the same day does not create a second backup
    //  - A second save on the same day leaves the existing backup unchanged
    // ========================================================================

    [TestClass]
    public class TestDailyBackup
    {
        private const int AwaitTimeoutMs = 5000;
        private const string ActiveListFilename = "active-crowds.json";

        private string _dataDirectory;
        private CrowdRepository _repository;
        private string _today;
        private string _backupSuffix;

        [TestInitialize]
        public void Init()
        {
            _dataDirectory = Path.Combine(
                Path.GetTempPath(), "coh-vtt-backup-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDirectory);
            _repository = new CrowdRepository { DataDirectory = _dataDirectory };
            _today = DateTime.Today.ToString("yyyyMMdd");
            _backupSuffix = "." + _today + ".bak";
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                foreach (string f in Directory.GetFiles(_dataDirectory))
                    File.SetAttributes(f, FileAttributes.Normal);
                Directory.Delete(_dataDirectory, true);
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // Scenario: First save of the day creates a dated backup

        [TestMethod]
        public void GivenNoPriorBackup_WhenSaved_ThenDatedBackupCreated()
        {
            GivenCrowdFileOnDisk("heroes.json", f => f.TopLevel("Freedom Phalanx"));
            GivenNoDailyBackupExistsFor("heroes.json");
            GivenActiveCrowdListContains("heroes.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();

            WhenGmRenamesTopLevelCrowd("Freedom Phalanx", "Freedom Phalanx Reformed");
            AwaitSaveDirtyCrowds();

            ThenDailyBackupExistsFor("heroes.json");
        }

        // ------------------------------------------------------------------
        // Scenario: A second save on the same day does not create a second backup

        [TestMethod]
        public void GivenExistingBackupForToday_WhenSavedSecondTime_ThenNoSecondBackupCreated()
        {
            GivenCrowdFileOnDisk("heroes.json", f => f.TopLevel("Freedom Phalanx"));
            GivenActiveCrowdListContains("heroes.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();

            // First save: creates the backup
            WhenGmRenamesTopLevelCrowd("Freedom Phalanx", "Freedom Phalanx Reformed");
            AwaitSaveDirtyCrowds();
            ThenDailyBackupExistsFor("heroes.json");

            // Capture the backup content after the first save
            string backupPath = BackupPathFor("heroes.json");
            byte[] backupAfterFirstSave = File.ReadAllBytes(backupPath);

            // Second save: backup must not be replaced or duplicated
            WhenGmRenamesTopLevelCrowd("Freedom Phalanx Reformed", "Freedom Phalanx Reformed v2");
            AwaitSaveDirtyCrowds();

            byte[] backupAfterSecondSave = File.ReadAllBytes(backupPath);
            CollectionAssert.AreEqual(backupAfterFirstSave, backupAfterSecondSave,
                "The existing daily backup must not be replaced or changed by a second save on the same day.");

            // Confirm no additional backup files exist for heroes.json
            string stem = Path.GetFileNameWithoutExtension("heroes.json");
            int backupCount = Directory.GetFiles(_dataDirectory, stem + "*.bak").Length;
            Assert.AreEqual(1, backupCount,
                "Exactly one backup file must exist for heroes.json; found " + backupCount + ".");
        }

        // ------------------------------------------------------------------
        // Scenario: Backup created with the correct date-stamp format

        [TestMethod]
        public void GivenDirtyFile_WhenSaved_ThenBackupHasCorrectDateStampFormat()
        {
            GivenCrowdFileOnDisk("patrol.json", f => f.TopLevel("Patrol Alpha"));
            GivenActiveCrowdListContains("patrol.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();

            WhenGmRenamesTopLevelCrowd("Patrol Alpha", "Patrol Alpha Modified");
            AwaitSaveDirtyCrowds();

            // Expected: patrol.<yyyyMMdd>.bak
            string expectedBackup = PathFor("patrol" + _backupSuffix);
            Assert.IsTrue(File.Exists(expectedBackup),
                "Expected backup file '" + Path.GetFileName(expectedBackup) + "' to exist. " +
                "Today suffix: " + _today);
        }

        // ------------------------------------------------------------------
        // Scenario: Backup content is the pre-save file content

        [TestMethod]
        public void GivenDirtyFile_WhenSaved_ThenBackupContainsPreSaveContent()
        {
            GivenCrowdFileOnDisk("heroes.json", f => f.TopLevel("Freedom Phalanx").WithCharacter("Statesman"));
            byte[] preSaveContent = File.ReadAllBytes(PathFor("heroes.json"));
            GivenActiveCrowdListContains("heroes.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();

            WhenGmRenamesTopLevelCrowd("Freedom Phalanx", "Freedom Phalanx Reformed");
            AwaitSaveDirtyCrowds();

            byte[] backupContent = File.ReadAllBytes(BackupPathFor("heroes.json"));
            CollectionAssert.AreEqual(preSaveContent, backupContent,
                "The daily backup must contain the pre-save content of the crowd file.");
        }

        // ------------------------------------------------------------------
        // Helpers

        private void GivenCrowdFileOnDisk(string filename, Action<CrowdFileBuilder> build)
        {
            CrowdFileBuilder b = new CrowdFileBuilder();
            build(b);
            Helper.SerializeObjectAsJSONToFile(PathFor(filename), b.Build());
        }

        private void GivenNoDailyBackupExistsFor(string filename)
        {
            string backupPath = BackupPathFor(filename);
            if (File.Exists(backupPath)) File.Delete(backupPath);
        }

        private void GivenActiveCrowdListContains(params string[] filenames)
        {
            Helper.SerializeObjectAsJSONToFile(
                Path.Combine(_dataDirectory, ActiveListFilename),
                filenames.Select(PathFor).ToList());
        }

        private void GivenCharacterCrowdMainWorkspaceIsOpen()
        {
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                _repository.LoadActiveCrowdFiles(_ => done.Set());
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "LoadActiveCrowdFiles timed out.");
            }
        }

        private void WhenGmRenamesTopLevelCrowd(string oldName, string newName)
        {
            List<CrowdModel> crowds = AwaitGetCrowdCollection();
            CrowdModel crowd = crowds.FirstOrDefault(c => c.Name == oldName);
            Assert.IsNotNull(crowd, "Crowd '" + oldName + "' not found.");
            crowd.Name = newName;
            crowd.IsDirty = true;
        }

        private void AwaitSaveDirtyCrowds()
        {
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                _repository.SaveDirtyCrowds(_ => done.Set());
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "SaveDirtyCrowds timed out.");
            }
        }

        private List<CrowdModel> AwaitGetCrowdCollection()
        {
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                List<CrowdModel> result = null;
                _repository.GetCrowdCollection(crowds => { result = crowds; done.Set(); });
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "GetCrowdCollection timed out.");
                return result ?? new List<CrowdModel>();
            }
        }

        private void ThenDailyBackupExistsFor(string filename)
        {
            string backupPath = BackupPathFor(filename);
            Assert.IsTrue(File.Exists(backupPath),
                "Expected daily backup '" + Path.GetFileName(backupPath) + "' to exist.");
        }

        private string BackupPathFor(string filename)
        {
            string stem = Path.GetFileNameWithoutExtension(filename);
            return PathFor(stem + _backupSuffix);
        }

        private string PathFor(string filename)
        {
            return Path.Combine(_dataDirectory, filename);
        }
    }
}
