using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.Roster
{
    [TestClass]
    public class ClearCharacterFromDesktop : RosterHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenSessionActive();
            GivenRosterHasEntries();
        }

        [TestMethod]
        public void SpawnedDespawnSucceeds()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            WhenGmClearsFromDesktop("Guard_Captain_01");
            ThenSpawnedState("Guard_Captain_01", "false");
        }

        [TestMethod]
        public void AlreadyNotSpawnedNoOp()
        {
            GivenRosterEntry("Guard_Captain_01", "false", "hidden");
            WhenGmClearsFromDesktop("Guard_Captain_01");
            ThenSpawnedState("Guard_Captain_01", "false");
        }

        [TestMethod]
        public void DespawnCommandFailsRemainsTrue()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            GivenDespawnWillFail();
            WhenGmClearsFromDesktop("Guard_Captain_01");
            ThenSpawnedState("Guard_Captain_01", "true");
        }

        [TestMethod]
        public void ClearedCharacterWasActiveDesignationRemoved()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            GivenActiveCharacter("Guard_Captain_01");
            WhenGmClearsFromDesktop("Guard_Captain_01");
            ThenActiveCharacter("none");
        }

        [TestMethod]
        public void ClearedCharacterWasNotActiveDesignationUnchanged()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            GivenActiveCharacter("Villain_Boss_03");
            WhenGmClearsFromDesktop("Guard_Captain_01");
            ThenActiveCharacter("Villain_Boss_03");
        }
    }
}
