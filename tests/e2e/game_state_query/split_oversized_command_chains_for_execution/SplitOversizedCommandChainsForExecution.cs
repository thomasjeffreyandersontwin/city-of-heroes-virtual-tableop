using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameStateQuery
{
    [TestClass]
    public class SplitOversizedCommandChainsForExecution : GameStateQueryHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
            GivenGameBridgeInitialized();
        }

        [TestMethod]
        public void WithinLimitSingleBatchNotDetected()
        {
            GivenCommandChain(new[] { "cmd_A", "cmd_B", "cmd_C" });
            WhenApplicationAssemblesAndDeliversChain();
            ThenOversizedChainDetected("not detected");
        }

        [TestMethod]
        public void OversizedSplitIntoSubChainsDetected()
        {
            GivenCommandChain(new[] { "cmd_A", "cmd_B", "cmd_C", "cmd_D", "cmd_E", "cmd_F", "cmd_G", "cmd_H", "cmd_I", "cmd_J", "cmd_K", "cmd_L", "cmd_M", "cmd_N", "cmd_O", "cmd_P", "cmd_Q", "cmd_R", "cmd_S", "cmd_T", "cmd_U", "cmd_V", "cmd_W", "cmd_X", "cmd_Y", "cmd_Z" });
            WhenApplicationAssemblesAndDeliversChain();
            ThenOversizedChainDetected("detected");
        }

        [TestMethod]
        public void SplitWithDeliveryFailureDetected()
        {
            GivenCommandChain(new[] { "cmd_A", "cmd_B", "cmd_C", "cmd_D", "cmd_E", "cmd_F", "cmd_G", "cmd_H", "cmd_I", "cmd_J", "cmd_K", "cmd_L", "cmd_M", "cmd_N", "cmd_O", "cmd_P", "cmd_Q", "cmd_R", "cmd_S", "cmd_T", "cmd_U", "cmd_V", "cmd_W", "cmd_X", "cmd_Y", "cmd_Z" });
            WhenApplicationAssemblesAndDeliversChain();
            ThenOversizedChainDetected("detected");
        }

        [TestMethod]
        public void ZeroCommandSubChainSkipped()
        {
            GivenCommandChain(new[] { "cmd_A", "", "cmd_C", "cmd_D" });
            WhenApplicationAssemblesAndDeliversChain();
            ThenOversizedChainDetected("detected");
        }
    }
}
