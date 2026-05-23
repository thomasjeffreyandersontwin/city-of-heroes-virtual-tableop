using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.HcsIntegration
{
    [TestClass]
    public class ReadOnDeckCombatantsFromInfoFile : HcsIntegrationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
            GivenGameBridgeReady();
            GivenHcsFileWatcherActive();
        }

        [TestMethod]
        public void CharactersMatched()
        {
            WhenInfoFileArrives("on_deck", "Guard_A, Villain_B");
            ThenOnDeckCombatants(new[] { "Guard_A", "Villain_B" });
        }

        [TestMethod]
        public void OneCharacterUnmatched()
        {
            WhenInfoFileArrives("on_deck", "Guard_A, Unknown_X");
            ThenOnDeckCombatants(new[] { "Guard_A" });
            ThenWarningLogged();
        }

        [TestMethod]
        public void EmptyListNoHighlights()
        {
            WhenInfoFileArrives("on_deck", "");
            ThenNoOnDeckHighlights();
        }
    }
}
