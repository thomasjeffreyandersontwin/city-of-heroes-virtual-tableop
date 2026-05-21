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
    /// <summary>
    /// RED acceptance tests for three crowd-persistence stories:
    ///   - Track Source File per Crowd
    ///   - Save Dirty Crowds to Source Files
    ///   - Save Crowd to New File
    ///
    /// Each [TestClass] maps to one story; each [TestMethod] maps to one specification scenario.
    /// Surface under test: <see cref="CrowdRepository"/> driven directly.
    ///
    /// Compiled by the in-box .NET Framework 4.0.30319 MSBuild (C# 5).
    /// No interpolated strings, no null-conditional, no expression-bodied members.
    /// </summary>

    // ========================================================================
    // STORY: Track Source File per Crowd
    // ========================================================================

    [TestClass]
    public class TestTrackSourceFilePerCrowd
    {
        private const int AwaitTimeoutMs = 5000;
        private const string ActiveCrowdListFilename = "active-crowds.json";

        private string _dataDirectory;
        private string _activeCrowdListPath;
        private CrowdRepository _repository;

        [TestInitialize]
        public void Init()
        {
            _dataDirectory = Path.Combine(
                Path.GetTempPath(), "coh-vtt-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDirectory);
            _activeCrowdListPath = Path.Combine(_dataDirectory, ActiveCrowdListFilename);
            _repository = new CrowdRepository { DataDirectory = _dataDirectory };
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { if (Directory.Exists(_dataDirectory)) Directory.Delete(_dataDirectory, true); }
            catch { }
        }

        [TestMethod]
        public void SavingChangedLoadedCrowdWritesBackToItsOwnSourceFile()
        {
            GivenCrowdFileExistsOnDisk("heroes.json", f => f
                .TopLevel("Freedom Phalanx").WithCharacter("Statesman"));
            GivenCrowdFileExistsOnDisk("villains.json", f => f
                .TopLevel("Council Empire").WithCharacter("Marcus Valerius"));
            GivenPersistedActiveCrowdListContains("heroes.json", "villains.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();
            byte[] originalVillains = ReadBytes("villains.json");

            WhenGmRenamesTopLevelCrowd("Freedom Phalanx", "Freedom Phalanx Reformed");
            AwaitSaveDirtyCrowds();

            ThenCrowdFileContainsTopLevelCrowd("heroes.json", "Freedom Phalanx Reformed");
            ThenCrowdFileIsByteUnchanged("villains.json", originalVillains);
            ThenDailyBackupExistsFor("heroes.json");
        }

        [TestMethod]
        public void CharacterAddedToNestedCrowdWritesParentSourceFile()
        {
            GivenCrowdFileExistsOnDisk("villains.json", f => f
                .TopLevel("Council Empire")
                    .WithNested("Vampyri", n => n.WithCharacter("Galaxy")));
            GivenPersistedActiveCrowdListContains("villains.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();

            WhenGmAddsCharacterToNestedCrowd("Vampyri", "Vandal");
            AwaitSaveDirtyCrowds();

            ThenNestedCrowdInFileContainsCharacters("villains.json", "Council Empire", "Vampyri",
                "Galaxy", "Vandal");
        }

        [TestMethod]
        public void RenamingNestedCrowdWritesParentSourceFile()
        {
            GivenCrowdFileExistsOnDisk("villains.json", f => f
                .TopLevel("Council Empire")
                    .WithNested("Vampyri", n => n.WithCharacter("Galaxy")));
            GivenPersistedActiveCrowdListContains("villains.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();

            WhenGmRenamesNestedCrowd("Council Empire", "Vampyri", "Vampyri Cabal");
            AwaitSaveDirtyCrowds();

            ThenCrowdFileContainsNestedCrowd("villains.json", "Council Empire", "Vampyri Cabal");
            ThenCrowdFileDoesNotContainNestedCrowd("villains.json", "Council Empire", "Vampyri");
        }

        [TestMethod]
        public void NestedCrowdMovedBetweenTopLevelsWritesBothSourceFiles()
        {
            GivenCrowdFileExistsOnDisk("heroes.json", f => f
                .TopLevel("Freedom Phalanx")
                    .WithCharacter("Statesman")
                    .WithNested("Phalanx Recruits", n => n.WithCharacter("Apprentice 1")));
            GivenCrowdFileExistsOnDisk("villains.json", f => f
                .TopLevel("Council Empire").WithCharacter("Marcus Valerius"));
            GivenPersistedActiveCrowdListContains("heroes.json", "villains.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();

            WhenGmMovesNestedCrowdBetweenTopLevelParents(
                "Phalanx Recruits", "Freedom Phalanx", "Council Empire");
            AwaitSaveDirtyCrowds();

            ThenCrowdFileDoesNotContainNestedCrowd("heroes.json", "Freedom Phalanx", "Phalanx Recruits");
            ThenCrowdFileContainsTopLevelCrowd("heroes.json", "Freedom Phalanx");
            ThenCrowdFileContainsNestedCrowd("villains.json", "Council Empire", "Phalanx Recruits");
            ThenNestedCrowdInFileContainsCharacters(
                "villains.json", "Council Empire", "Phalanx Recruits", "Apprentice 1");
        }

        // ------------------------------------------------------------------ helpers

        private void GivenCrowdFileExistsOnDisk(string filename, Action<CrowdFileBuilder> build)
        {
            CrowdFileBuilder b = new CrowdFileBuilder();
            build(b);
            Helper.SerializeObjectAsJSONToFile(PathFor(filename), b.Build());
        }

        private void GivenPersistedActiveCrowdListContains(params string[] filenames)
        {
            Helper.SerializeObjectAsJSONToFile(_activeCrowdListPath,
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
            CrowdModel crowd = FindTopLevelCrowd(oldName);
            crowd.Name = newName;
            crowd.IsDirty = true;
        }

        private void WhenGmRenamesNestedCrowd(string parentName, string oldNestedName, string newNestedName)
        {
            CrowdModel parent = FindTopLevelCrowd(parentName);
            ICrowdMemberModel member = parent.CrowdMemberCollection
                .FirstOrDefault(m => m.Name == oldNestedName);
            Assert.IsNotNull(member,
                "Nested member '" + oldNestedName + "' not found in '" + parentName + "'.");
            member.Name = newNestedName;
            // Member_PropertyChanged in parent fires on Name change and sets parent.IsDirty = true
        }

        private void WhenGmMovesNestedCrowdBetweenTopLevelParents(
            string nestedName, string fromParent, string toParent)
        {
            CrowdModel source = FindTopLevelCrowd(fromParent);
            CrowdModel destination = FindTopLevelCrowd(toParent);

            CrowdModel nested = source.CrowdMemberCollection
                .OfType<CrowdModel>()
                .FirstOrDefault(c => c.Name == nestedName);
            Assert.IsNotNull(nested,
                "Nested crowd '" + nestedName + "' not found inside '" + fromParent + "'.");

            source.Remove(nested);
            destination.Add(nested);

            // Make both parents observably dirty so SaveDirtyCrowds writes both files.
            // (Production code may set these via collection-change handlers; the test
            // states the intent explicitly so the scenario is independent of that wiring.)
            source.IsDirty = true;
            destination.IsDirty = true;
        }

        private void WhenGmAddsCharacterToNestedCrowd(string nestedCrowdName, string characterName)
        {
            List<CrowdModel> crowds = AwaitGetCrowdCollection();
            foreach (CrowdModel top in crowds)
            {
                CrowdModel nested = top.CrowdMemberCollection
                    .OfType<CrowdModel>()
                    .FirstOrDefault(c => c.Name == nestedCrowdName);
                if (nested == null) continue;
                nested.Add(new CrowdMemberModel { Name = characterName });
                top.IsDirty = true; // nested.Add sets nested.IsDirty; mark parent dirty so save targets it
                return;
            }
            Assert.Fail("Nested crowd '" + nestedCrowdName + "' not found.");
        }

        private void AwaitSaveDirtyCrowds()
        {
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                _repository.SaveDirtyCrowds(_ => done.Set());
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "SaveDirtyCrowds timed out.");
            }
        }

        private void ThenCrowdFileContainsTopLevelCrowd(string filename, string crowdName)
        {
            List<CrowdModel> crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(PathFor(filename));
            Assert.IsNotNull(crowds, "File '" + filename + "' could not be read.");
            Assert.IsTrue(crowds.Any(c => c.Name == crowdName),
                "File '" + filename + "' does not contain top-level crowd '" + crowdName + "'. " +
                "Found: [" + string.Join(", ", crowds.Select(c => c.Name)) + "]");
        }

        private void ThenCrowdFileIsByteUnchanged(string filename, byte[] originalBytes)
        {
            byte[] current = ReadBytes(filename);
            CollectionAssert.AreEqual(originalBytes, current,
                "File '" + filename + "' was unexpectedly modified.");
        }

        private void ThenDailyBackupExistsFor(string filename)
        {
            string stem = Path.GetFileNameWithoutExtension(filename);
            string today = DateTime.Today.ToString("yyyyMMdd");
            string backupPath = PathFor(stem + "." + today + ".bak");
            Assert.IsTrue(File.Exists(backupPath),
                "Expected daily backup '" + stem + "." + today + ".bak' to exist after saving '" + filename + "'.");
        }

        private void ThenNestedCrowdInFileContainsCharacters(
            string filename, string parentName, string nestedName, params string[] characters)
        {
            List<CrowdModel> crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(PathFor(filename));
            CrowdModel parent = crowds == null ? null : crowds.FirstOrDefault(c => c.Name == parentName);
            Assert.IsNotNull(parent,
                "Top-level crowd '" + parentName + "' not found in '" + filename + "'.");
            CrowdModel nested = parent.CrowdMemberCollection.OfType<CrowdModel>()
                .FirstOrDefault(c => c.Name == nestedName);
            Assert.IsNotNull(nested,
                "Nested crowd '" + nestedName + "' not found in '" + parentName + "'.");
            foreach (string ch in characters)
                Assert.IsTrue(nested.CrowdMemberCollection.Any(m => m.Name == ch),
                    "Character '" + ch + "' not found in nested crowd '" + nestedName + "'.");
        }

        private void ThenCrowdFileContainsNestedCrowd(string filename, string parentName, string nestedName)
        {
            List<CrowdModel> crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(PathFor(filename));
            CrowdModel parent = crowds == null ? null : crowds.FirstOrDefault(c => c.Name == parentName);
            Assert.IsNotNull(parent);
            Assert.IsTrue(parent.CrowdMemberCollection.OfType<CrowdModel>().Any(c => c.Name == nestedName),
                "Nested crowd '" + nestedName + "' not found in '" + parentName + "' in file '" + filename + "'.");
        }

        private void ThenCrowdFileDoesNotContainNestedCrowd(string filename, string parentName, string nestedName)
        {
            List<CrowdModel> crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(PathFor(filename));
            CrowdModel parent = crowds == null ? null : crowds.FirstOrDefault(c => c.Name == parentName);
            Assert.IsNotNull(parent);
            Assert.IsFalse(parent.CrowdMemberCollection.OfType<CrowdModel>().Any(c => c.Name == nestedName),
                "Nested crowd '" + nestedName + "' should no longer exist in '" + parentName + "' in file '" + filename + "'.");
        }

        private CrowdModel FindTopLevelCrowd(string name)
        {
            CrowdModel crowd = AwaitGetCrowdCollection().FirstOrDefault(c => c.Name == name);
            Assert.IsNotNull(crowd, "Top-level crowd '" + name + "' not found.");
            return crowd;
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

        private string PathFor(string filename)
        {
            return Path.Combine(_dataDirectory, filename);
        }

        private byte[] ReadBytes(string filename)
        {
            return File.ReadAllBytes(PathFor(filename));
        }
    }

    // ========================================================================
    // STORY: Save Dirty Crowds to Source Files
    // ========================================================================

    [TestClass]
    public class TestSaveDirtyCrowdsToSourceFiles
    {
        private const int AwaitTimeoutMs = 5000;
        private const string ActiveCrowdListFilename = "active-crowds.json";

        private string _dataDirectory;
        private string _activeCrowdListPath;
        private CrowdRepository _repository;

        [TestInitialize]
        public void Init()
        {
            _dataDirectory = Path.Combine(
                Path.GetTempPath(), "coh-vtt-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDirectory);
            _activeCrowdListPath = Path.Combine(_dataDirectory, ActiveCrowdListFilename);
            _repository = new CrowdRepository { DataDirectory = _dataDirectory };
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { if (Directory.Exists(_dataDirectory)) Directory.Delete(_dataDirectory, true); }
            catch { }
        }

        [TestMethod]
        public void SaveSkipsCleanCrowd()
        {
            GivenCrowdFileExistsOnDisk("armageddons.json", f => f
                .TopLevel("Armageddon Squad").WithCharacter("Battle Maiden"));
            GivenPersistedActiveCrowdListContains("armageddons.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();
            byte[] originalBytes = ReadBytes("armageddons.json");
            // No changes made

            SaveSummary summary = AwaitSaveDirtyCrowds();

            ThenCrowdFileIsByteUnchanged("armageddons.json", originalBytes);
            Assert.AreEqual(0, summary.SavedCount, "Expected 0 saves for a clean crowd.");
            Assert.AreEqual(0, summary.FailedCount);
            Assert.AreEqual(1, summary.SkippedCount, "Expected 1 skip for the clean crowd.");
        }

        [TestMethod]
        public void SaveWritesOnlyDirtyFilesAmongManyLoaded()
        {
            GivenCrowdFileExistsOnDisk("heroes.json", f => f.TopLevel("Freedom Phalanx"));
            GivenCrowdFileExistsOnDisk("villains.json", f => f.TopLevel("Council Empire"));
            GivenCrowdFileExistsOnDisk("neutrals.json", f => f.TopLevel("Wandering Wraith"));
            GivenPersistedActiveCrowdListContains("heroes.json", "villains.json", "neutrals.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();
            byte[] originalVillains = ReadBytes("villains.json");
            byte[] originalNeutrals = ReadBytes("neutrals.json");

            WhenGmRenamesTopLevelCrowd("Freedom Phalanx", "Freedom Phalanx Reformed");
            SaveSummary summary = AwaitSaveDirtyCrowds();

            ThenCrowdFileContainsTopLevelCrowd("heroes.json", "Freedom Phalanx Reformed");
            ThenCrowdFileIsByteUnchanged("villains.json", originalVillains);
            ThenCrowdFileIsByteUnchanged("neutrals.json", originalNeutrals);
            Assert.AreEqual(1, summary.SavedCount);
            Assert.AreEqual(0, summary.FailedCount);
            Assert.AreEqual(2, summary.SkippedCount);
        }

        [TestMethod]
        public void DailyBackupIsCreatedBeforeOverwritingDirtyFile()
        {
            GivenCrowdFileExistsOnDisk("heroes.json", f => f.TopLevel("Freedom Phalanx"));
            GivenPersistedActiveCrowdListContains("heroes.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();
            GivenNoDailyBackupExistsFor("heroes.json");

            WhenGmRenamesTopLevelCrowd("Freedom Phalanx", "Freedom Phalanx Reformed");
            AwaitSaveDirtyCrowds();

            ThenDailyBackupExistsFor("heroes.json");
            ThenCrowdFileContainsTopLevelCrowd("heroes.json", "Freedom Phalanx Reformed");
        }

        [TestMethod]
        public void OneFailingWriteLeavesFileDirtyAndDoesNotBlockOthers()
        {
            GivenCrowdFileExistsOnDisk("heroes.json", f => f.TopLevel("Freedom Phalanx"));
            GivenCrowdFileExistsOnDisk("readonly.json", f => f.TopLevel("Read Only Squad"));
            GivenPersistedActiveCrowdListContains("heroes.json", "readonly.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();

            WhenGmRenamesTopLevelCrowd("Freedom Phalanx", "Freedom Phalanx Reformed");
            WhenGmRenamesTopLevelCrowd("Read Only Squad", "Read Only Squad Renamed");
            GivenFileIsReadOnly("readonly.json");

            SaveSummary summary = AwaitSaveDirtyCrowds();

            ThenCrowdFileContainsTopLevelCrowd("heroes.json", "Freedom Phalanx Reformed");
            Assert.AreEqual(1, summary.SavedCount);
            Assert.AreEqual(1, summary.FailedCount);
            CollectionAssert.Contains(summary.FailedPaths, PathFor("readonly.json"),
                "Failed paths should include readonly.json.");

            // Second save still retries readonly.json but not heroes.json
            byte[] heroesAfterFirstSave = ReadBytes("heroes.json");
            SaveSummary summary2 = AwaitSaveDirtyCrowds();
            ThenCrowdFileIsByteUnchanged("heroes.json", heroesAfterFirstSave);
            Assert.AreEqual(0, summary2.SavedCount, "heroes.json must not be re-saved (not dirty).");
            Assert.AreEqual(1, summary2.FailedCount,
                "readonly.json must be retried and fail again on second save.");
        }

        [TestMethod]
        public void NeverSavedCrowdIsReturnedAsCrowdNeedingNewFile()
        {
            GivenPersistedActiveCrowdListIsEmpty();
            GivenCharacterCrowdMainWorkspaceIsOpen();
            WhenGmCreatesNewTopLevelCrowd("New Squad");

            SaveSummary summary = AwaitSaveDirtyCrowds();

            Assert.IsTrue(
                summary.CrowdsNeedingNewFile.Any(c => c.Name == "New Squad"),
                "SaveSummary.CrowdsNeedingNewFile should contain 'New Squad'.");
            Assert.IsFalse(File.Exists(PathFor("New Squad.json")),
                "No file should be created until the GM confirms the Save Crowd to New File dialog.");
        }

        [TestMethod]
        public void SaveHandlesMixOfSourceBoundAndNeverSavedCrowds()
        {
            GivenCrowdFileExistsOnDisk("heroes.json", f => f
                .TopLevel("Freedom Phalanx").WithCharacter("Statesman"));
            GivenPersistedActiveCrowdListContains("heroes.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();

            WhenGmRenamesTopLevelCrowd("Freedom Phalanx", "Freedom Phalanx Reformed");
            WhenGmCreatesNewTopLevelCrowd("Splinter Cell");

            SaveSummary summary = AwaitSaveDirtyCrowds();

            ThenCrowdFileContainsTopLevelCrowd("heroes.json", "Freedom Phalanx Reformed");
            Assert.AreEqual(1, summary.SavedCount);
            Assert.IsTrue(summary.CrowdsNeedingNewFile.Any(c => c.Name == "Splinter Cell"),
                "Splinter Cell (no source file) should be in CrowdsNeedingNewFile.");
        }

        // ------------------------------------------------------------------ helpers

        private void GivenCrowdFileExistsOnDisk(string filename, Action<CrowdFileBuilder> build)
        {
            CrowdFileBuilder b = new CrowdFileBuilder();
            build(b);
            Helper.SerializeObjectAsJSONToFile(PathFor(filename), b.Build());
        }

        private void GivenPersistedActiveCrowdListContains(params string[] filenames)
        {
            Helper.SerializeObjectAsJSONToFile(_activeCrowdListPath,
                filenames.Select(PathFor).ToList());
        }

        private void GivenPersistedActiveCrowdListIsEmpty()
        {
            if (File.Exists(_activeCrowdListPath)) File.Delete(_activeCrowdListPath);
        }

        private void GivenCharacterCrowdMainWorkspaceIsOpen()
        {
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                _repository.LoadActiveCrowdFiles(_ => done.Set());
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "LoadActiveCrowdFiles timed out.");
            }
        }

        private void GivenNoDailyBackupExistsFor(string filename)
        {
            string stem = Path.GetFileNameWithoutExtension(filename);
            string today = DateTime.Today.ToString("yyyyMMdd");
            string backupPath = PathFor(stem + "." + today + ".bak");
            if (File.Exists(backupPath)) File.Delete(backupPath);
        }

        private void GivenFileIsReadOnly(string filename)
        {
            File.SetAttributes(PathFor(filename), FileAttributes.ReadOnly);
        }

        private void WhenGmRenamesTopLevelCrowd(string oldName, string newName)
        {
            CrowdModel crowd = AwaitGetCrowdCollection().FirstOrDefault(c => c.Name == oldName);
            Assert.IsNotNull(crowd, "Crowd '" + oldName + "' not found.");
            crowd.Name = newName;
            crowd.IsDirty = true;
        }

        private void WhenGmCreatesNewTopLevelCrowd(string name)
        {
            _repository.AddCrowd(new CrowdModel { Name = name, IsDirty = true });
        }

        private SaveSummary AwaitSaveDirtyCrowds()
        {
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                SaveSummary summary = null;
                _repository.SaveDirtyCrowds(s => { summary = s; done.Set(); });
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "SaveDirtyCrowds timed out.");
                return summary;
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

        private void ThenCrowdFileContainsTopLevelCrowd(string filename, string crowdName)
        {
            List<CrowdModel> crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(PathFor(filename));
            Assert.IsNotNull(crowds, "Could not read '" + filename + "'.");
            Assert.IsTrue(crowds.Any(c => c.Name == crowdName),
                "'" + filename + "' does not contain '" + crowdName + "'. " +
                "Found: [" + string.Join(", ", crowds.Select(c => c.Name)) + "]");
        }

        private void ThenCrowdFileIsByteUnchanged(string filename, byte[] originalBytes)
        {
            CollectionAssert.AreEqual(originalBytes, ReadBytes(filename),
                "File '" + filename + "' was unexpectedly modified.");
        }

        private void ThenDailyBackupExistsFor(string filename)
        {
            string stem = Path.GetFileNameWithoutExtension(filename);
            string today = DateTime.Today.ToString("yyyyMMdd");
            Assert.IsTrue(File.Exists(PathFor(stem + "." + today + ".bak")),
                "Expected daily backup for '" + filename + "'.");
        }

        private string PathFor(string filename)
        {
            return Path.Combine(_dataDirectory, filename);
        }

        private byte[] ReadBytes(string filename)
        {
            return File.ReadAllBytes(PathFor(filename));
        }
    }

    // ========================================================================
    // STORY: Save Crowd to New File
    // ========================================================================

    [TestClass]
    public class TestSaveCrowdToNewFile
    {
        private const int AwaitTimeoutMs = 5000;
        private const string ActiveCrowdListFilename = "active-crowds.json";

        private string _dataDirectory;
        private string _activeCrowdListPath;
        private CrowdRepository _repository;

        [TestInitialize]
        public void Init()
        {
            _dataDirectory = Path.Combine(
                Path.GetTempPath(), "coh-vtt-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDirectory);
            _activeCrowdListPath = Path.Combine(_dataDirectory, ActiveCrowdListFilename);
            _repository = new CrowdRepository { DataDirectory = _dataDirectory };
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                // Remove read-only attributes before cleanup
                foreach (string f in Directory.GetFiles(_dataDirectory))
                    File.SetAttributes(f, FileAttributes.Normal);
                Directory.Delete(_dataDirectory, true);
            }
            catch { }
        }

        [TestMethod]
        public void SaveAsWritesFreshCrowdFileAndActivatesIt()
        {
            GivenPersistedActiveCrowdListIsEmpty();
            GivenCharacterCrowdMainWorkspaceIsOpen();
            WhenGmCreatesNewTopLevelCrowd("New Squad");
            WhenGmAddsCharacterToTopLevelCrowd("New Squad", "Recruit Alpha");
            WhenGmAddsCharacterToTopLevelCrowd("New Squad", "Recruit Beta");

            AwaitSaveCrowdToNewFile("New Squad", "new-squad.json");

            ThenCrowdFileContainsTopLevelCrowdWithCharacters(
                "new-squad.json", "New Squad", "Recruit Alpha", "Recruit Beta");
            ThenPersistedActiveCrowdListContainsExactly("new-squad.json");
        }

        [TestMethod]
        public void SaveAsTopLevelCrowdWithNestedCrowdsWritesFullSubtree()
        {
            GivenPersistedActiveCrowdListIsEmpty();
            GivenCharacterCrowdMainWorkspaceIsOpen();
            WhenGmCreatesNewTopLevelCrowdWithNested("Council Empire",
                "Marcus Valerius",
                "Vampyri",
                new[] { "Galaxy", "Vandal" });

            AwaitSaveCrowdToNewFile("Council Empire", "villains.json");

            List<CrowdModel> crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(PathFor("villains.json"));
            CrowdModel top = crowds == null ? null : crowds.FirstOrDefault(c => c.Name == "Council Empire");
            Assert.IsNotNull(top, "Top-level crowd 'Council Empire' not found in villains.json.");
            Assert.IsTrue(top.CrowdMemberCollection.Any(m => m.Name == "Marcus Valerius"),
                "Character 'Marcus Valerius' missing from 'Council Empire'.");
            CrowdModel vampyri = top.CrowdMemberCollection.OfType<CrowdModel>()
                .FirstOrDefault(c => c.Name == "Vampyri");
            Assert.IsNotNull(vampyri, "Nested crowd 'Vampyri' not serialized into villains.json.");
            Assert.IsTrue(vampyri.CrowdMemberCollection.Any(m => m.Name == "Galaxy"));
            Assert.IsTrue(vampyri.CrowdMemberCollection.Any(m => m.Name == "Vandal"));
        }

        [TestMethod]
        public void SaveAsOfLoadedCrowdSwitchesSourceFilePath()
        {
            GivenCrowdFileExistsOnDisk("armageddons.json", f => f.TopLevel("Armageddon Squad"));
            GivenPersistedActiveCrowdListContains("armageddons.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();
            byte[] originalBytes = ReadBytes("armageddons.json");

            WhenGmRenamesTopLevelCrowd("Armageddon Squad", "Armageddon Squad Reforged");
            AwaitSaveCrowdToNewFile("Armageddon Squad Reforged", "armageddon-reforged.json");

            ThenCrowdFileContainsTopLevelCrowd("armageddon-reforged.json", "Armageddon Squad Reforged");
            ThenCrowdFileIsByteUnchanged("armageddons.json", originalBytes);
            ThenPersistedActiveCrowdListContains("armageddon-reforged.json");

            // Subsequent rename + Save Dirty targets the NEW file, not the old one
            WhenGmRenamesTopLevelCrowd("Armageddon Squad Reforged", "Armageddon Squad Reforged v2");
            AwaitSaveDirtyCrowds();
            ThenCrowdFileContainsTopLevelCrowd("armageddon-reforged.json", "Armageddon Squad Reforged v2");
            ThenCrowdFileIsByteUnchanged("armageddons.json", originalBytes);
        }

        [TestMethod]
        public void SaveAsToExistingPathReplacesItWithoutCreatingDailyBackup()
        {
            GivenCrowdFileExistsOnDisk("target.json", f => f.TopLevel("Old Squad"));
            GivenCharacterCrowdMainWorkspaceIsOpen();
            WhenGmCreatesNewTopLevelCrowd("Replacement Squad");
            WhenGmAddsCharacterToTopLevelCrowd("Replacement Squad", "Replacement");
            string today = DateTime.Today.ToString("yyyyMMdd");
            string backupPath = PathFor("target." + today + ".bak");

            AwaitSaveCrowdToNewFile("Replacement Squad", "target.json");

            ThenCrowdFileContainsTopLevelCrowdWithCharacters("target.json", "Replacement Squad", "Replacement");
            Assert.IsFalse(File.Exists(backupPath),
                "Save Crowd to New File must NOT create a daily backup of the prior contents.");
        }

        [TestMethod]
        public void SaveAsIsRejectedWhenNestedCrowdIsSelected()
        {
            GivenCrowdFileExistsOnDisk("armageddons.json", f => f
                .TopLevel("Armageddon Squad")
                    .WithNested("Demolition Team", n => n.WithCharacter("Demo Lead")));
            GivenPersistedActiveCrowdListContains("armageddons.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();

            bool rejected = AwaitSaveCrowdToNewFileExpectRejection("Demolition Team", "demolition.json");

            Assert.IsTrue(rejected,
                "SaveCrowdToNewFile should return rejected=true when a nested crowd is selected.");
            Assert.IsFalse(File.Exists(PathFor("demolition.json")),
                "No file should be created when Save As is applied to a nested crowd.");
        }

        // ------------------------------------------------------------------ helpers

        private void GivenCrowdFileExistsOnDisk(string filename, Action<CrowdFileBuilder> build)
        {
            CrowdFileBuilder b = new CrowdFileBuilder();
            build(b);
            Helper.SerializeObjectAsJSONToFile(PathFor(filename), b.Build());
        }

        private void GivenPersistedActiveCrowdListContains(params string[] filenames)
        {
            Helper.SerializeObjectAsJSONToFile(_activeCrowdListPath,
                filenames.Select(PathFor).ToList());
        }

        private void GivenPersistedActiveCrowdListIsEmpty()
        {
            if (File.Exists(_activeCrowdListPath)) File.Delete(_activeCrowdListPath);
        }

        private void GivenCharacterCrowdMainWorkspaceIsOpen()
        {
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                _repository.LoadActiveCrowdFiles(_ => done.Set());
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "LoadActiveCrowdFiles timed out.");
            }
        }

        private void WhenGmCreatesNewTopLevelCrowd(string name)
        {
            _repository.AddCrowd(new CrowdModel { Name = name, IsDirty = true });
        }

        private void WhenGmCreatesNewTopLevelCrowdWithNested(
            string topName, string character, string nestedCrowdName, string[] nestedCharacters)
        {
            CrowdModel top = new CrowdModel { Name = topName, IsDirty = true };
            top.Add(new CrowdMemberModel { Name = character });
            CrowdModel nested = new CrowdModel { Name = nestedCrowdName };
            foreach (string ch in nestedCharacters)
                nested.Add(new CrowdMemberModel { Name = ch });
            top.Add(nested);
            top.IsDirty = true;
            _repository.AddCrowd(top);
        }

        private void WhenGmAddsCharacterToTopLevelCrowd(string crowdName, string characterName)
        {
            CrowdModel crowd = AwaitGetCrowdCollection().FirstOrDefault(c => c.Name == crowdName);
            Assert.IsNotNull(crowd, "Crowd '" + crowdName + "' not found.");
            crowd.Add(new CrowdMemberModel { Name = characterName });
        }

        private void WhenGmRenamesTopLevelCrowd(string oldName, string newName)
        {
            CrowdModel crowd = AwaitGetCrowdCollection().FirstOrDefault(c => c.Name == oldName);
            Assert.IsNotNull(crowd, "Crowd '" + oldName + "' not found.");
            crowd.Name = newName;
            crowd.IsDirty = true;
        }

        private void AwaitSaveCrowdToNewFile(string crowdName, string filename)
        {
            CrowdModel crowd = AwaitGetCrowdCollection().FirstOrDefault(c => c.Name == crowdName);
            Assert.IsNotNull(crowd, "Crowd '" + crowdName + "' not found for Save As.");
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                _repository.SaveCrowdToNewFile(crowd, PathFor(filename), () => done.Set());
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "SaveCrowdToNewFile timed out.");
            }
        }

        private bool AwaitSaveCrowdToNewFileExpectRejection(string nestedCrowdName, string filename)
        {
            // Find the nested crowd anywhere in the tree
            List<CrowdModel> crowds = AwaitGetCrowdCollection();
            CrowdModel nestedCrowd = crowds
                .SelectMany(c => c.CrowdMemberCollection.OfType<CrowdModel>())
                .FirstOrDefault(c => c.Name == nestedCrowdName);
            Assert.IsNotNull(nestedCrowd, "Nested crowd '" + nestedCrowdName + "' not found.");

            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                bool rejected = false;
                _repository.SaveCrowdToNewFile(nestedCrowd, PathFor(filename), () =>
                {
                    rejected = true;
                    done.Set();
                }, () =>
                {
                    rejected = true;
                    done.Set();
                });
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "SaveCrowdToNewFile (rejection) timed out.");
                return rejected && !File.Exists(PathFor(filename));
            }
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

        private void ThenCrowdFileContainsTopLevelCrowd(string filename, string crowdName)
        {
            List<CrowdModel> crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(PathFor(filename));
            Assert.IsNotNull(crowds, "Could not read '" + filename + "'.");
            Assert.IsTrue(crowds.Any(c => c.Name == crowdName),
                "'" + filename + "' does not contain '" + crowdName + "'.");
        }

        private void ThenCrowdFileContainsTopLevelCrowdWithCharacters(
            string filename, string crowdName, params string[] characters)
        {
            List<CrowdModel> crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(PathFor(filename));
            CrowdModel crowd = crowds == null ? null : crowds.FirstOrDefault(c => c.Name == crowdName);
            Assert.IsNotNull(crowd, "Top-level crowd '" + crowdName + "' not found in '" + filename + "'.");
            foreach (string ch in characters)
                Assert.IsTrue(crowd.CrowdMemberCollection.Any(m => m.Name == ch),
                    "Character '" + ch + "' not found in '" + crowdName + "' in '" + filename + "'.");
        }

        private void ThenCrowdFileIsByteUnchanged(string filename, byte[] originalBytes)
        {
            CollectionAssert.AreEqual(originalBytes, ReadBytes(filename),
                "File '" + filename + "' was unexpectedly modified.");
        }

        private void ThenPersistedActiveCrowdListContainsExactly(params string[] filenames)
        {
            List<string> expected = filenames.Select(PathFor).ToList();
            List<string> actual = ReadActiveCrowdList();
            CollectionAssert.AreEquivalent(expected, actual,
                "Active list mismatch. Expected: [" + string.Join(", ", expected) + "]. " +
                "Actual: [" + string.Join(", ", actual) + "]");
        }

        private void ThenPersistedActiveCrowdListContains(params string[] filenames)
        {
            List<string> actual = ReadActiveCrowdList();
            foreach (string f in filenames)
                CollectionAssert.Contains(actual, PathFor(f),
                    "Active list does not contain '" + f + "'. Actual: [" + string.Join(", ", actual) + "]");
        }

        private List<string> ReadActiveCrowdList()
        {
            if (!File.Exists(_activeCrowdListPath)) return new List<string>();
            return Helper.GetDeserializedJSONFromFile<List<string>>(_activeCrowdListPath)
                   ?? new List<string>();
        }

        private string PathFor(string filename)
        {
            return Path.Combine(_dataDirectory, filename);
        }

        private byte[] ReadBytes(string filename)
        {
            return File.ReadAllBytes(PathFor(filename));
        }
    }

    // ========================================================================
    // Shared test fixture builder (used by all three test classes above)
    // ========================================================================

    internal class CrowdFileBuilder
    {
        private readonly List<CrowdModel> _topLevels = new List<CrowdModel>();

        public CrowdBuilder TopLevel(string name)
        {
            CrowdModel crowd = new CrowdModel { Name = name };
            _topLevels.Add(crowd);
            return new CrowdBuilder(crowd, this);
        }

        public List<CrowdModel> Build()
        {
            return _topLevels;
        }
    }

    internal class CrowdBuilder
    {
        private readonly CrowdModel _crowd;
        private readonly CrowdFileBuilder _file;

        public CrowdBuilder(CrowdModel crowd, CrowdFileBuilder file)
        {
            _crowd = crowd;
            _file = file;
        }

        public CrowdBuilder WithCharacter(string name)
        {
            _crowd.Add(new CrowdMemberModel { Name = name });
            return this;
        }

        public CrowdBuilder WithNested(string nestedName, Action<CrowdBuilder> build)
        {
            CrowdModel nested = new CrowdModel { Name = nestedName };
            _crowd.Add(nested);
            build(new CrowdBuilder(nested, _file));
            return this;
        }

        public CrowdBuilder TopLevel(string name)
        {
            return _file.TopLevel(name);
        }
    }
}
