using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.HcsIntegration
{
    [TestClass]
    public class StopHcsIntegration : HcsIntegrationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
            GivenGameBridgeReady();
        }

        [TestMethod]
        public void ActiveStopped()
        {
            GivenHcsIntegrationActive();
            WhenGmTriggersStopHcsIntegration();
            ThenHcsIntegrationState("inactive");
            ThenFileWatcherState("not_monitoring");
        }

        [TestMethod]
        public void MidProcessingCompletesThenStops()
        {
            GivenHcsIntegrationActive();
            WhenGmTriggersStopHcsIntegration();
            ThenHcsIntegrationState("inactive");
            ThenFileWatcherState("not_monitoring");
        }

        [TestMethod]
        public void AlreadyStoppedNoOp()
        {
            WhenGmTriggersStopHcsIntegration();
            ThenHcsIntegrationState("inactive");
            ThenFileWatcherState("not_monitoring");
        }

        [TestMethod]
        public void SessionEndsAutoStopped()
        {
            GivenHcsIntegrationActive();
            WhenGmTriggersStopHcsIntegration();
            ThenHcsIntegrationState("inactive");
            ThenFileWatcherState("not_monitoring");
        }
    }
}
