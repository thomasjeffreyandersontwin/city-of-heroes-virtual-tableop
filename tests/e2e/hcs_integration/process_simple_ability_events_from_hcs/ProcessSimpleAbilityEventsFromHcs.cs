using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.HcsIntegration
{
    [TestClass]
    public class ProcessSimpleAbilityEventsFromHcs : HcsIntegrationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
            GivenGameBridgeReady();
            GivenHcsFileWatcherActive();
        }

        [TestMethod]
        public void MatchedAbilityPlayed()
        {
            WhenInfoFileArrives("simple_ability", "Guard_Captain_01:heal_burst");
            ThenSimpleAbilityPlayed("Guard_Captain_01", "heal_burst");
        }

        [TestMethod]
        public void CharacterNotInRosterSkipped()
        {
            WhenInfoFileArrives("simple_ability", "Unknown_NPC:heal_burst");
            ThenWarningLogged();
        }

        [TestMethod]
        public void AbilityNotFoundWarning()
        {
            WhenInfoFileArrives("simple_ability", "Guard_Captain_01:nonexistent_skill");
            ThenWarningLogged();
        }

        [TestMethod]
        public void NonAttackLockActiveBlocked()
        {
            GivenNonAttackLockActive("Guard_Captain_01");
            WhenInfoFileArrives("simple_ability", "Guard_Captain_01:heal_burst");
            ThenSimpleAbilityBlocked("Guard_Captain_01");
            ThenWarningLogged();
        }
    }
}
