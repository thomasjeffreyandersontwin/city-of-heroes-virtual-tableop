using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameStateQuery
{
    [TestClass]
    public class ExecuteLoadMapCommand : GameStateQueryHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
        }

        [TestMethod]
        public void ValidMapTransitionSucceeds()
        {
            GivenGameStateQueryAvailable();
            WhenGmTriggersLoadMap("valid_map_01");
            ThenGameDoneState("false");
            ThenLoadMapSucceeded();
        }

        [TestMethod]
        public void InvalidMapCohRejectsNoStateModified()
        {
            GivenGameStateQueryAvailable();
            WhenGmTriggersLoadMap("invalid_map_99");
            ThenGameDoneState("false");
        }

        [TestMethod]
        public void GameBridgeNotInitializedCommandBlocked()
        {
            GivenGameStateQueryUnavailable();
            WhenGmTriggersLoadMap("valid_map_01");
            ThenLoadMapBlocked();
        }
    }
}
