using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.HcsIntegration
{
    [TestClass]
    public class StartHcsFileWatcherIntegration : HcsIntegrationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
        }

        [TestMethod]
        public void GameBridgeReadyStartSucceeds()
        {
            GivenGameBridgeReady();
            GivenOutputDirectoryExists(true);
            WhenGmTriggersStartHcsIntegration();
            ThenHcsIntegrationState("active");
            ThenFileWatcherState("monitoring");
        }

        [TestMethod]
        public void GameBridgeNotInitializedBlocked()
        {
            GivenGameBridgeNotInitialized();
            WhenGmTriggersStartHcsIntegration();
            ThenHcsIntegrationState("inactive");
            ThenFileWatcherState("not_monitoring");
        }

        [TestMethod]
        public void OutputDirectoryMissingBlocked()
        {
            GivenGameBridgeReady();
            GivenOutputDirectoryExists(false);
            WhenGmTriggersStartHcsIntegration();
            ThenHcsIntegrationState("inactive");
            ThenFileWatcherState("not_monitoring");
        }

        [TestMethod]
        public void AlreadyActiveNoOp()
        {
            GivenGameBridgeReady();
            GivenHcsIntegrationActive();
            WhenGmTriggersStartHcsIntegration();
            ThenHcsIntegrationState("active");
            ThenFileWatcherState("monitoring");
        }
    }
}
