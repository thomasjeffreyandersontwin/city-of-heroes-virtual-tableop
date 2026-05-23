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
    // STORY: Browse and Activate Crowd Files
    //
    // SBE scenarios:
    //  - Activating crowd files loads their Crowd structures into the Crowd Tree
    //  - Cloning a Crowd File suffixes only top-level Crowd names
    //  - A malformed Crowd File is reported and skipped without aborting others
    //  - Re-activating an active Crowd File picks the next available integer suffix
    //    (First clone → (2); Second clone → (3); Fill-the-gap → fills lowest)
    // ========================================================================

    [TestClass]
    public class TestBrowseAndActivateCrowdFiles
    {
        private const int AwaitTimeoutMs = 5000;
        private const string ActiveListFilename = "active-crowds.json";

        private string _dataDirectory;
        private CrowdRepository _repository;

        [TestInitialize]
        public void Init()
        {
            _dataDirectory = Path.Combine(
                Path.GetTempPath(), "coh-vtt-browse-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDirectory);
            _repository = new CrowdRepository { DataDirectory = _dataDirectory };
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
        // Scenario: Activating a new crowd file loads its crowd structure

        [TestMethod]
        public void GivenNewFile_WhenActivated_ThenCrowdLoadedAndListUpdated()
        {
            GivenCrowdFileOnDisk("armageddons.json", f => f
                .TopLevel("Armageddon Squad")
                    .WithCharacter("Battle Maiden")
                    .WithCharacter("Manticore")
                    .WithNested("Demolition Team", n => n.WithCharacter("Demo Lead")));
            GivenActiveCrowdListIsEmpty();

            List<CrowdModel> newlyLoaded = AwaitBrowseAndActivate("armageddons.json");

            // Crowd tree shows Armageddon Squad with nested Demolition Team
            CrowdModel armageddon = newlyLoaded.FirstOrDefault(c => c.Name == "Armageddon Squad");
            Assert.IsNotNull(armageddon, "Armageddon Squad must be returned from Browse and Activate.");

            CrowdModel demolition = armageddon.CrowdMemberCollection
                .OfType<CrowdModel>()
                .FirstOrDefault(c => c.Name == "Demolition Team");
            Assert.IsNotNull(demolition, "Nested crowd 'Demolition Team' must be present.");

            // Active list updated
            List<string> activeList = ReadActiveCrowdList();
            Assert.IsTrue(activeList.Contains(PathFor("armageddons.json")),
                "armageddons.json must be appended to the active crowd list.");
        }

        // ------------------------------------------------------------------
        // Scenario: Cloning suffixes only top-level crowd names; nested crowd names unchanged

        [TestMethod]
        public void GivenAlreadyActiveFile_WhenReactivated_ThenCloneCreatedWithSuffix2()
        {
            GivenCrowdFileOnDisk("villains.json", f => f
                .TopLevel("Council Empire")
                    .WithNested("Vampyri", n => n.WithCharacter("Galaxy")));
            GivenActiveCrowdListContains("villains.json");

            // Re-activate the already-active file
            AwaitBrowseAndActivate("villains.json");

            // Clone file exists with (2) suffix
            string clonePath = PathFor("villains (2).json");
            Assert.IsTrue(File.Exists(clonePath),
                "A clone file 'villains (2).json' must be created on re-activation.");

            // Original file is byte-unchanged
            byte[] originalBefore = File.ReadAllBytes(PathFor("villains.json"));
            // Re-read after operation; must be same
            byte[] originalAfter = File.ReadAllBytes(PathFor("villains.json"));
            CollectionAssert.AreEqual(originalBefore, originalAfter,
                "Original 'villains.json' must be byte-unchanged.");

            // Clone: top-level crowd name has suffix; nested name unchanged
            List<CrowdModel> cloneContents =
                Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(clonePath);
            Assert.IsNotNull(cloneContents, "Clone file must be valid JSON.");
            CrowdModel clonedCouncil = cloneContents.FirstOrDefault(c => c.Name == "Council Empire (2)");
            Assert.IsNotNull(clonedCouncil,
                "Top-level crowd must be renamed to 'Council Empire (2)' in the clone.");

            // Nested crowd name is unchanged
            CrowdModel vampyri = clonedCouncil.CrowdMemberCollection
                .OfType<CrowdModel>()
                .FirstOrDefault(c => c.Name == "Vampyri");
            Assert.IsNotNull(vampyri,
                "Nested crowd 'Vampyri' must keep its original name in the clone.");

            // Active list updated with clone path
            List<string> activeList = ReadActiveCrowdList();
            Assert.IsTrue(activeList.Contains(clonePath),
                "Clone path 'villains (2).json' must be appended to the active crowd list.");
        }

        // ------------------------------------------------------------------
        // Scenario: Re-activating when (2) clone already exists → picks (3)

        [TestMethod]
        public void GivenAlreadyActiveWithClone2_WhenReactivated_ThenPicksSuffix3()
        {
            // Given: armageddons.json + armageddons (2).json both in active list
            GivenCrowdFileOnDisk("armageddons.json", f => f
                .TopLevel("Armageddon Squad").WithCharacter("Battle Maiden"));
            GivenCrowdFileOnDisk("armageddons (2).json", f => f
                .TopLevel("Armageddon Squad (2)").WithCharacter("Battle Maiden"));
            GivenActiveCrowdListContains("armageddons.json", "armageddons (2).json");

            AwaitBrowseAndActivate("armageddons.json");

            string clonePath = PathFor("armageddons (3).json");
            Assert.IsTrue(File.Exists(clonePath),
                "Clone 'armageddons (3).json' must be created when (2) is already taken.");

            List<CrowdModel> cloneContents =
                Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(clonePath);
            Assert.IsNotNull(cloneContents);
            Assert.IsTrue(cloneContents.Any(c => c.Name == "Armageddon Squad (3)"),
                "Top-level crowd in clone must be named 'Armageddon Squad (3)'.");
        }

        // ------------------------------------------------------------------
        // Scenario: Fill the gap — when (2) was deleted, re-activation fills it

        [TestMethod]
        public void GivenGapInClones_WhenReactivated_ThenFillsLowestAvailableGap()
        {
            // Given: active list contains armageddons.json and armageddons (3).json
            // (2) has been deleted out-of-band; the gap at (2) must be filled
            GivenCrowdFileOnDisk("armageddons.json", f => f
                .TopLevel("Armageddon Squad").WithCharacter("Battle Maiden"));
            GivenCrowdFileOnDisk("armageddons (3).json", f => f
                .TopLevel("Armageddon Squad (3)").WithCharacter("Battle Maiden"));
            GivenActiveCrowdListContains("armageddons.json", "armageddons (3).json");
            // Note: "armageddons (2).json" is absent from both disk and active list → gap

            AwaitBrowseAndActivate("armageddons.json");

            string gapPath = PathFor("armageddons (2).json");
            Assert.IsTrue(File.Exists(gapPath),
                "Clone must be created at 'armageddons (2).json' to fill the gap.");

            List<CrowdModel> gapContents =
                Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(gapPath);
            Assert.IsNotNull(gapContents);
            Assert.IsTrue(gapContents.Any(c => c.Name == "Armageddon Squad (2)"),
                "Top-level crowd in gap clone must be named 'Armageddon Squad (2)'.");
        }

        // ------------------------------------------------------------------
        // Scenario: Activating two files: first new, second malformed — good one loads

        [TestMethod]
        public void GivenMalformedFile_WhenActivatedAlongsideGoodFile_ThenMalformedSkippedGoodLoads()
        {
            GivenCrowdFileOnDisk("heroes.json", f => f
                .TopLevel("Freedom Phalanx").WithCharacter("Statesman"));
            GivenMalformedCrowdFileOnDisk("broken.json");
            GivenActiveCrowdListIsEmpty();

            // Activate broken.json first, then heroes.json
            List<CrowdModel> newlyLoaded = AwaitBrowseAndActivate("broken.json", "heroes.json");

            // Freedom Phalanx must be in the returned list
            Assert.IsTrue(newlyLoaded.Any(c => c.Name == "Freedom Phalanx"),
                "Freedom Phalanx must load even when broken.json was also selected.");

            // Active list must contain heroes.json but not broken.json
            List<string> activeList = ReadActiveCrowdList();
            Assert.IsTrue(activeList.Contains(PathFor("heroes.json")),
                "heroes.json must be in the active crowd list.");
            Assert.IsFalse(activeList.Contains(PathFor("broken.json")),
                "broken.json must not be added to the active crowd list after failure.");
        }

        // ------------------------------------------------------------------
        // Scenario: Single-file activation sets source file path on loaded crowd

        [TestMethod]
        public void GivenNewFile_WhenActivated_ThenSourceFilePathSetOnLoadedCrowd()
        {
            GivenCrowdFileOnDisk("armageddons.json", f => f
                .TopLevel("Armageddon Squad").WithCharacter("Battle Maiden"));
            GivenActiveCrowdListIsEmpty();

            List<CrowdModel> newlyLoaded = AwaitBrowseAndActivate("armageddons.json");

            CrowdModel crowd = newlyLoaded.FirstOrDefault(c => c.Name == "Armageddon Squad");
            Assert.IsNotNull(crowd);
            Assert.AreEqual(PathFor("armageddons.json"), crowd.SourceFilePath,
                "The activated crowd must have its source file path set.");
        }

        // ------------------------------------------------------------------
        // Helpers

        private void GivenCrowdFileOnDisk(string filename, Action<CrowdFileBuilder> build)
        {
            CrowdFileBuilder b = new CrowdFileBuilder();
            build(b);
            Helper.SerializeObjectAsJSONToFile(PathFor(filename), b.Build());
        }

        private void GivenMalformedCrowdFileOnDisk(string filename)
        {
            File.WriteAllText(PathFor(filename), "not valid json {{{");
        }

        private void GivenActiveCrowdListIsEmpty()
        {
            string path = Path.Combine(_dataDirectory, ActiveListFilename);
            if (File.Exists(path)) File.Delete(path);
        }

        private void GivenActiveCrowdListContains(params string[] filenames)
        {
            Helper.SerializeObjectAsJSONToFile(
                Path.Combine(_dataDirectory, ActiveListFilename),
                filenames.Select(PathFor).ToList());
        }

        private List<CrowdModel> AwaitBrowseAndActivate(params string[] filenames)
        {
            string[] paths = filenames.Select(PathFor).ToArray();
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                List<CrowdModel> result = null;
                _repository.BrowseAndActivate(paths, loaded =>
                {
                    result = new List<CrowdModel>(loaded);
                    done.Set();
                });
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "BrowseAndActivate timed out.");
                return result ?? new List<CrowdModel>();
            }
        }

        private List<string> ReadActiveCrowdList()
        {
            string path = Path.Combine(_dataDirectory, ActiveListFilename);
            if (!File.Exists(path)) return new List<string>();
            return Helper.GetDeserializedJSONFromFile<List<string>>(path) ?? new List<string>();
        }

        private string PathFor(string filename)
        {
            return Path.Combine(_dataDirectory, filename);
        }
    }
}
