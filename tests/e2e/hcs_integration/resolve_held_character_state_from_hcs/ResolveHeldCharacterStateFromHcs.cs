using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.HcsIntegration
{
    [TestClass]
    public class ResolveHeldCharacterStateFromHcs : HcsIntegrationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
            GivenGameBridgeReady();
            GivenHcsFileWatcherActive();
        }

        [TestMethod]
        public void CharacterHeldStateUpdated()
        {
            WhenInfoFileArrives("held_state", "Guard_Captain_01:held");
            ThenHeldState("Guard_Captain_01", "held");
        }

        [TestMethod]
        public void CharacterNotInRosterSkipped()
        {
            WhenInfoFileArrives("held_state", "Unknown_NPC:held");
            ThenWarningLogged();
        }

        [TestMethod]
        public void NoLongerHeldDesignationRemoved()
        {
            WhenInfoFileArrives("held_state", "Guard_Captain_01:released");
            ThenHeldState("Guard_Captain_01", "released");
        }
    }
}
