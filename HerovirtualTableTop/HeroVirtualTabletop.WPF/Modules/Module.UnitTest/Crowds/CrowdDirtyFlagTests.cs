using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Utility;

namespace Module.UnitTest.Crowds
{
    // ========================================================================
    // STORY: Crowd dirty flag
    //
    // CRC invariant: IsDirty is set on any structural change (rename, add/remove
    // member, add/remove nested crowd, saved-position change); cleared only when
    // the crowd is written to its source file.
    // ========================================================================

    [TestClass]
    public class TestCrowdDirtyFlag
    {
        private const int AwaitTimeoutMs = 5000;

        private string _dataDirectory;
        private CrowdRepository _repository;

        [TestInitialize]
        public void Init()
        {
            _dataDirectory = Path.Combine(
                Path.GetTempPath(), "coh-vtt-dirty-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDirectory);
            _repository = new CrowdRepository { DataDirectory = _dataDirectory };
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_dataDirectory))
                    Directory.Delete(_dataDirectory, true);
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // Scenario: Dirty flag is set when a member is added

        [TestMethod]
        public void WhenMemberAdded_ThenCrowdIsDirty()
        {
            CrowdModel crowd = GivenCrowdModel("Patrol Alpha");
            crowd.IsDirty = false;

            WhenMemberAddedToCrowd(crowd, "Recruit 1");

            ThenCrowdIsDirty(crowd);
        }

        // ------------------------------------------------------------------
        // Scenario: Dirty flag is set when a member is removed

        [TestMethod]
        public void WhenMemberRemoved_ThenCrowdIsDirty()
        {
            CrowdModel crowd = GivenCrowdModel("Patrol Alpha");
            CrowdMemberModel member = new CrowdMemberModel { Name = "Recruit 1" };
            crowd.Add(member);
            crowd.IsDirty = false;

            crowd.Remove(member);

            ThenCrowdIsDirty(crowd);
        }

        // ------------------------------------------------------------------
        // Scenario: Dirty flag is set when a member's name changes

        [TestMethod]
        public void WhenMemberRenamed_ThenCrowdIsDirty()
        {
            CrowdModel crowd = GivenCrowdModel("Patrol Alpha");
            CrowdMemberModel member = new CrowdMemberModel { Name = "Recruit 1" };
            crowd.Add(member);
            crowd.IsDirty = false;

            member.Name = "Recruit One";

            ThenCrowdIsDirty(crowd);
        }

        // ------------------------------------------------------------------
        // Scenario: Dirty flag is cleared when saved; starts false when loaded

        [TestMethod]
        public void GivenCrowdLoadedFromFile_WhenNoChanges_ThenCrowdIsClean()
        {
            GivenCrowdFileOnDisk("heroes.json", "Freedom Phalanx", "Statesman");
            GivenActiveCrowdListContains("heroes.json");

            List<CrowdModel> loaded = AwaitLoadActiveCrowdFiles();

            CrowdModel crowd = loaded.FirstOrDefault(c => c.Name == "Freedom Phalanx");
            Assert.IsNotNull(crowd, "Freedom Phalanx not found in loaded crowds.");
            Assert.IsFalse(crowd.IsDirty,
                "A freshly loaded crowd must not be dirty before any changes are made.");
        }

        [TestMethod]
        public void WhenCrowdSaved_ThenDirtyFlagIsCleared()
        {
            GivenCrowdFileOnDisk("heroes.json", "Freedom Phalanx", "Statesman");
            GivenActiveCrowdListContains("heroes.json");
            List<CrowdModel> loaded = AwaitLoadActiveCrowdFiles();

            CrowdModel crowd = loaded.FirstOrDefault(c => c.Name == "Freedom Phalanx");
            Assert.IsNotNull(crowd);
            crowd.Name = "Freedom Phalanx Reformed";
            crowd.IsDirty = true;
            Assert.IsTrue(crowd.IsDirty, "Crowd must be dirty before save.");

            AwaitSaveDirtyCrowds();

            Assert.IsFalse(crowd.IsDirty,
                "Dirty flag must be cleared after a successful save.");
        }

        // ------------------------------------------------------------------
        // Scenario: Adding a second member also marks dirty

        [TestMethod]
        public void WhenSecondMemberAdded_ThenCrowdRemainsOrBecomesMarkedDirty()
        {
            CrowdModel crowd = GivenCrowdModel("Patrol Alpha");
            crowd.Add(new CrowdMemberModel { Name = "Recruit 1" });
            crowd.IsDirty = false;

            WhenMemberAddedToCrowd(crowd, "Recruit 2");

            ThenCrowdIsDirty(crowd);
        }

        // ------------------------------------------------------------------
        // Helpers

        private CrowdModel GivenCrowdModel(string name)
        {
            return new CrowdModel { Name = name };
        }

        private void WhenMemberAddedToCrowd(CrowdModel crowd, string memberName)
        {
            crowd.Add(new CrowdMemberModel { Name = memberName });
        }

        private void ThenCrowdIsDirty(CrowdModel crowd)
        {
            Assert.IsTrue(crowd.IsDirty,
                "CrowdModel.IsDirty must be true after a structural change to '" + crowd.Name + "'.");
        }

        private void GivenCrowdFileOnDisk(string filename, string crowdName, params string[] members)
        {
            CrowdModel crowd = new CrowdModel { Name = crowdName };
            foreach (string m in members)
                crowd.Add(new CrowdMemberModel { Name = m });
            Helper.SerializeObjectAsJSONToFile(PathFor(filename), new List<CrowdModel> { crowd });
        }

        private void GivenActiveCrowdListContains(params string[] filenames)
        {
            Helper.SerializeObjectAsJSONToFile(
                Path.Combine(_dataDirectory, "active-crowds.json"),
                filenames.Select(PathFor).ToList());
        }

        private List<CrowdModel> AwaitLoadActiveCrowdFiles()
        {
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                List<CrowdModel> result = null;
                _repository.LoadActiveCrowdFiles(loaded => { result = new List<CrowdModel>(loaded); done.Set(); });
                Assert.IsTrue(done.Wait(AwaitTimeoutMs), "LoadActiveCrowdFiles timed out.");
                return result ?? new List<CrowdModel>();
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

        private string PathFor(string filename)
        {
            return Path.Combine(_dataDirectory, filename);
        }
    }
}
