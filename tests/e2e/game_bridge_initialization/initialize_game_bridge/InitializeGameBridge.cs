using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameBridgeInitialization
{
    [TestClass]
    public class InitializeGameBridge : GameBridgeInitializationHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void DllLoadedInitSucceedsTransitionsToPolling()
        {
            // Given
            GivenGameBridgeWithInitializationState("initializing");
            GivenHookCostumeDllLoaded();

            // When
            WhenGameBridgeCallsInitGame();

            // Then
            ThenGameBridgeHasInitializationState("polling");
        }

        [TestMethod]
        public void DllLoadedInitReturnsFailureRemainsUninitialized()
        {
            // Given
            GivenGameBridgeWithInitializationState("initializing");
            GivenHookCostumeDllLoaded();
            GivenInitGameWillReturnFailure();

            // When
            WhenGameBridgeCallsInitGame();

            // Then
            ThenGameBridgeHasInitializationState("uninitialized");
        }

        [TestMethod]
        public void DllNotLoadedBeforeCallRejectedWithOrderingError()
        {
            // Given
            GivenGameBridgeWithInitializationState("initializing");
            GivenHookCostumeDllNotLoaded();

            // When
            WhenGameBridgeCallsInitGame();

            // Then
            ThenGameBridgeHasInitializationState("uninitialized");
            ThenGameBridgeReportsError("ordering");
        }

        [TestMethod]
        public void DuplicateCallAfterReadyReachedIsIgnored()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenHookCostumeDllLoaded();

            // When
            WhenGameBridgeCallsInitGame();

            // Then
            ThenGameBridgeHasInitializationState("ready");
        }
    }
}
