using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.HcsIntegration
{
    [TestClass]
    public class ExecuteSweepResultsFromHcs : HcsIntegrationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
            GivenGameBridgeReady();
            GivenHcsFileWatcherActive();
        }

        [TestMethod]
        public void AllDefendersMatched()
        {
            WhenInfoFileArrives("sweep_results", "Villain_A:Hit,Villain_B:Miss");
            ThenSweepResultsDispatched("Villain_A:Hit,Villain_B:Miss");
        }

        [TestMethod]
        public void OneDefenderUnmatched()
        {
            WhenInfoFileArrives("sweep_results", "Villain_A:Hit,Unknown_X:Hit");
            ThenWarningLogged();
        }

        [TestMethod]
        public void AllResolvedIndicatorsUpdated()
        {
            WhenInfoFileArrives("sweep_results", "Villain_A:Stunned,Villain_B:no_effect");
            ThenSweepResultsDispatched("Villain_A:Stunned,Villain_B:no_effect");
        }

        [TestMethod]
        public void EmptyPayloadWarning()
        {
            WhenInfoFileArrives("sweep_results", "");
            ThenWarningLogged();
        }
    }
}
