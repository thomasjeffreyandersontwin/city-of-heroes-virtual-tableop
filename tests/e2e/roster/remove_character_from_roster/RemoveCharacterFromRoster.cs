using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.Roster
{
    [TestClass]
    public class RemoveCharacterFromRoster : RosterHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenSessionActive();
            GivenRosterHasEntries();
        }

        [TestMethod]
        public void SpawnedDespawnThenRemove()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            WhenGmRemovesFromRoster("Guard_Captain_01");
            ThenRosterEntryNotExists("Guard_Captain_01");
        }

        [TestMethod]
        public void NotSpawnedRemoveOnly()
        {
            GivenRosterEntry("Villain_Boss_03", "false", "hidden");
            WhenGmRemovesFromRoster("Villain_Boss_03");
            ThenRosterEntryNotExists("Villain_Boss_03");
        }

        [TestMethod]
        public void DespawnFailsStillRemoved()
        {
            GivenRosterEntry("Healer_01", "true", "hidden");
            WhenGmRemovesFromRoster("Healer_01");
            ThenRosterEntryNotExists("Healer_01");
        }

        [TestMethod]
        public void GangMemberGangDeactivatedFirst()
        {
            GivenRosterEntry("Guard_A", "true", "visible");
            WhenGmRemovesFromRoster("Guard_A");
            ThenRosterEntryNotExists("Guard_A");
        }

        [TestMethod]
        public void LastEntryEmptyPlaceholderShown()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            WhenGmRemovesFromRoster("Guard_Captain_01");
            ThenRosterEntryNotExists("Guard_Captain_01");
        }
    }
}
