using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.HcsIntegration
{
    [TestClass]
    public class ReadActiveCharacterFromInfoFile : HcsIntegrationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
            GivenGameBridgeReady();
            GivenHcsFileWatcherActive();
        }

        [TestMethod]
        public void CharacterMatched()
        {
            WhenInfoFileArrives("active_character", "Guard_Captain_01");
            ThenActiveCharacterHcs("Guard_Captain_01");
        }

        [TestMethod]
        public void CharacterNotInRosterNoChange()
        {
            WhenInfoFileArrives("active_character", "Unknown_NPC");
            ThenActiveCharacterUnchanged();
            ThenWarningLogged();
        }

        [TestMethod]
        public void DesignationAbsentNoChange()
        {
            WhenInfoFileArrives("active_character", "");
            ThenActiveCharacterUnchanged();
        }
    }
}
