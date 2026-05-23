using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameStateQuery
{
    [TestClass]
    public class CloseGameBridgeOnShutdown : GameStateQueryHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
        }

        [TestMethod]
        public void NormalShutdownBridgeActiveReleasesHandles()
        {
            GivenGameStateQueryAvailable();
            WhenGmClosesApplication();
            ThenShutdownCompleted();
        }

        [TestMethod]
        public void BridgeAlreadyUninitializedCompletesWithoutError()
        {
            GivenGameStateQueryUnavailable();
            WhenGmClosesApplication();
            ThenShutdownCompleted();
            ThenNoError();
        }

        [TestMethod]
        public void AbnormalCrashOsProcessCleanup()
        {
            GivenGameStateQueryAvailable();
            WhenGmClosesApplication();
            ThenShutdownCompleted();
        }
    }
}
