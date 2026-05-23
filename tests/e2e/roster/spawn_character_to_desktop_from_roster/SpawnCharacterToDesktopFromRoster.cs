using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.Roster
{
    [TestClass]
    public class SpawnCharacterToDesktopFromRoster : RosterHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenSessionActive();
            GivenGameBridgeInitialized();
        }

        [TestMethod]
        public void NotSpawnedSpawnSucceeds()
        {
            GivenRosterEntry("Guard_Captain_01", "false", "hidden");
            WhenGmSpawnsFromRoster("Guard_Captain_01");
            ThenSpawnedState("Guard_Captain_01", "true");
        }

        [TestMethod]
        public void AlreadySpawnedNoOp()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            WhenGmSpawnsFromRoster("Guard_Captain_01");
            ThenSpawnedState("Guard_Captain_01", "true");
        }

        [TestMethod]
        public void SpawnCommandFailsRemainsFalse()
        {
            GivenRosterEntry("Villain_Boss_03", "false", "hidden");
            GivenSpawnWillFail();
            WhenGmSpawnsFromRoster("Villain_Boss_03");
            ThenSpawnedState("Villain_Boss_03", "false");
        }

        [TestMethod]
        public void MultipleSpawnsInSequence()
        {
            GivenRosterEntry("Healer_01", "false", "hidden");
            WhenGmSpawnsFromRoster("Healer_01");
            ThenSpawnedState("Healer_01", "true");
        }
    }
}
