using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.HcsIntegration
{
    [TestClass]
    public class ReadEligibleCombatantsFromInfoFile : HcsIntegrationHelper
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
            WhenInfoFileArrives("eligible", "Guard_A, Guard_B, Villain_C");
            ThenEligibleCombatants(new[] { "Guard_A", "Guard_B", "Villain_C" });
        }

        [TestMethod]
        public void OneCharacterUnmatched()
        {
            WhenInfoFileArrives("eligible", "Guard_A, Unknown_Y");
            ThenEligibleCombatants(new[] { "Guard_A" });
            ThenWarningLogged();
        }

        [TestMethod]
        public void EmptyListNoneEligible()
        {
            WhenInfoFileArrives("eligible", "");
            ThenEligibleCombatants(new string[0]);
        }
    }
}
