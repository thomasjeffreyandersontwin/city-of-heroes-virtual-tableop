using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.HcsIntegration
{
    [TestClass]
    public class ReadChronometerTurnStateFromInfoFile : HcsIntegrationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
            GivenGameBridgeReady();
            GivenHcsFileWatcherActive();
        }

        [TestMethod]
        public void PhaseReadCombatStateUpdated()
        {
            WhenInfoFileArrives("chronometer", "Guard_Captain_01:active");
            ThenChronometerPhase("Guard_Captain_01", "active");
        }

        [TestMethod]
        public void PhaseChangesToHeld()
        {
            WhenInfoFileArrives("chronometer", "Guard_Captain_01:held");
            ThenChronometerPhase("Guard_Captain_01", "held");
        }

        [TestMethod]
        public void CharacterNotInRosterSkipped()
        {
            WhenInfoFileArrives("chronometer", "Unknown_NPC:active");
            ThenWarningLogged();
        }
    }
}
