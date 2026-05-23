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
    // STORY: Load Active Crowd Files on Startup
    //
    // SBE scenarios:
    //  - An empty Active Crowd List loads no Crowds and no defaults
    //  - Loading active Crowd Files on startup restores their crowd structure
    //  - A missing path on disk is reported and skipped, others still load
    //  - A malformed active Crowd File is reported and skipped
    //
    // Note: the "Back Up Repository on Load" story is not yet implemented in
    // LoadActiveCrowdFiles and is therefore excluded from this suite.
    // ========================================================================

    [TestClass]
    public class TestLoadActiveCrowdFilesOnStartup
    {
        private const int AwaitTimeoutMs = 5000;
        private const string ActiveListFilename = "active-crowds.json";

        private string _dataDirectory;
        private CrowdRepository _repository;

        [TestInitialize]
        public void Init()
        {
            _dataDirectory = Path.Combine(
                Path.GetTempPath(), "coh-vtt-load-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDirectory);
            _repository = new CrowdRepository { DataDirectory = _dataDirectory };
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { if (Directory.Exists(_dataDirectory)) Directory.Delete(_dataDirectory, true); }
            catch { }
        }

        // ------------------------------------------------------------------
        // Scenario: An empty Active Crowd List loads no Crowds and no defaults

        [TestMethod]
        public void GivenEmptyActiveCrowdList_WhenWorkspaceOpens_ThenNoCrowdsLoaded()
        {
            // Given: active-crowds.json does not exist
            // (no call to GivenActiveCrowdListContains)

            List<CrowdModel> loaded = AwaitLoadActiveCrowdFiles();

            Assert.AreEqual(0, loaded.Count,
                "No crowds should be loaded when the active crowd list is absent.");
            Assert.IsFalse(File.Exists(Path.Combine(_dataDirectory, "active-crowds.json")),
                "No active-crowds.json should be created by the load step alone.");
        }

        // ------------------------------------------------------------------
        // Scenario Outline (Single nested file): hierarchy is restored

        [TestMethod]
        public void GivenSingleFileWithNestedCrowd_WhenWorkspaceOpens_ThenHierarchyRestored()
        {
            // Given: villains.json with Council Empire → Vampyri → Galaxy, Vandal
            GivenCrowdFileOnDisk("villains.json", f => f
                .TopLevel("Council Empire")
                    .WithCharacter("Marcus Valerius")
                    .WithNested("Vampyri", n => n
                        .WithCharacter("Galaxy")
                        .WithCharacter("Vandal")));
            GivenActiveCrowdListContains("villains.json");

            List<CrowdModel> loaded = AwaitLoadActiveCrowdFiles();

            // Then: Council Empire at root with nested Vampyri
            CrowdModel councilEmpire = loaded.FirstOrDefault(c => c.Name == "Council Empire");
            Assert.IsNotNull(councilEmpire, "Council Empire should be loaded.");

            CrowdModel vampyri = councilEmpire.CrowdMemberCollection
                .OfType<CrowdModel>()
                .FirstOrDefault(c => c.Name == "Vampyri");
            Assert.IsNotNull(vampyri, "Nested crowd 'Vampyri' should be present under 'Council Empire'.");

            bool hasGalaxy = vampyri.CrowdMemberCollection.Any(m => m.Name == "Galaxy");
            bool hasVandal = vampyri.CrowdMemberCollection.Any(m => m.Name == "Vandal");
            Assert.IsTrue(hasGalaxy, "Character 'Galaxy' should be in 'Vampyri'.");
            Assert.IsTrue(hasVandal, "Character 'Vandal' should be in 'Vampyri'.");
        }

        // ------------------------------------------------------------------
        // Scenario Outline (Two files list order): list order is preserved

        [TestMethod]
        public void GivenTwoFilesInListOrder_WhenWorkspaceOpens_ThenHeroesBeforeVillains()
        {
            // Given: heroes.json before villains.json in the active list
            GivenCrowdFileOnDisk("heroes.json", f => f
                .TopLevel("Freedom Phalanx").WithCharacter("Statesman"));
            GivenCrowdFileOnDisk("villains.json", f => f
                .TopLevel("Council Empire").WithCharacter("Marcus Valerius"));
            GivenActiveCrowdListContains("heroes.json", "villains.json");

            List<CrowdModel> loaded = AwaitLoadActiveCrowdFiles();

            // Order is determined by Order property set during load (based on list position)
            CrowdModel freedomPhalanx = loaded.FirstOrDefault(c => c.Name == "Freedom Phalanx");
            CrowdModel councilEmpire = loaded.FirstOrDefault(c => c.Name == "Council Empire");
            Assert.IsNotNull(freedomPhalanx, "Freedom Phalanx must be loaded.");
            Assert.IsNotNull(councilEmpire, "Council Empire must be loaded.");

            Assert.IsTrue(freedomPhalanx.Order < councilEmpire.Order,
                "Freedom Phalanx (from heroes.json) must precede Council Empire (from villains.json) in list order.");
        }

        // ------------------------------------------------------------------
        // Scenario: A missing path on disk is reported and skipped, others load

        [TestMethod]
        public void GivenMissingPathInList_WhenWorkspaceOpens_ThenSkippedAndPathKeptInList()
        {
            // Given: heroes.json exists, missing.json does not
            GivenCrowdFileOnDisk("heroes.json", f => f
                .TopLevel("Freedom Phalanx").WithCharacter("Statesman"));
            string missingPath = PathFor("missing.json");
            GivenActiveCrowdListContains("heroes.json", "missing.json");

            List<CrowdModel> loaded = AwaitLoadActiveCrowdFiles();

            // Then: Freedom Phalanx loaded; missing.json path kept in active list for GM action
            Assert.IsTrue(loaded.Any(c => c.Name == "Freedom Phalanx"),
                "Freedom Phalanx from heroes.json must still load.");
            Assert.IsFalse(loaded.Any(c => c.Name == "missing"),
                "No crowd from missing.json should appear.");

            List<string> activeList = ReadActiveCrowdList();
            Assert.IsTrue(activeList.Contains(missingPath),
                "The missing path must remain in the active crowd list for GM action.");
        }

        // ------------------------------------------------------------------
        // Scenario: A malformed active Crowd File is reported and skipped

        [TestMethod]
        public void GivenMalformedFile_WhenWorkspaceOpens_ThenSkippedAndRemovedFromList()
        {
            // Given: heroes.json valid, corrupt.json malformed JSON
            GivenCrowdFileOnDisk("heroes.json", f => f.TopLevel("Freedom Phalanx"));
            GivenMalformedCrowdFileOnDisk("corrupt.json");
            GivenActiveCrowdListContains("heroes.json", "corrupt.json");

            List<CrowdModel> loaded = AwaitLoadActiveCrowdFiles();

            // Then: Freedom Phalanx loads; no crowd from corrupt.json
            Assert.IsTrue(loaded.Any(c => c.Name == "Freedom Phalanx"),
                "Freedom Phalanx must load despite the malformed file.");
            Assert.AreEqual(1, loaded.Count,
                "Only the valid crowd should appear in the loaded list.");

            // Malformed path is cleaned from the active list
            List<string> activeList = ReadActiveCrowdList();
            Assert.IsFalse(activeList.Contains(PathFor("corrupt.json")),
                "The malformed path should be removed from the active crowd list.");
        }

        // ------------------------------------------------------------------
        // Scenario: Source file path is set on loaded crowds

        [TestMethod]
        public void GivenLoadedFile_WhenWorkspaceOpens_ThenSourceFilePathSet()
        {
            GivenCrowdFileOnDisk("heroes.json", f => f.TopLevel("Freedom Phalanx"));
            GivenActiveCrowdListContains("heroes.json");

            List<CrowdModel> loaded = AwaitLoadActiveCrowdFiles();

            CrowdModel crowd = loaded.FirstOrDefault(c => c.Name == "Freedom Phalanx");
            Assert.IsNotNull(crowd);
            Assert.AreEqual(PathFor("heroes.json"), crowd.SourceFilePath,
                "Loaded crowd must have SourceFilePath set to its crowd file.");
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
            File.WriteAllText(PathFor(filename), "{ this is not valid json ]]]");
        }

        private void GivenActiveCrowdListContains(params string[] filenames)
        {
            Helper.SerializeObjectAsJSONToFile(
                Path.Combine(_dataDirectory, ActiveListFilename),
                filenames.Select(PathFor).ToList());
        }

        private List<CrowdModel> AwaitLoadActiveCrowdFiles()
        {
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                List<CrowdModel> result = null;
                _repository.LoadActiveCrowdFiles(loaded =>
                {
                    result = new List<CrowdModel>(loaded);
                    done.Set();
                });
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "LoadActiveCrowdFiles timed out.");
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
