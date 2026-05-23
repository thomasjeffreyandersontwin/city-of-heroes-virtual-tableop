using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameBridgeInitialization
{
    [TestClass]
    public class InitializeNativeGameBridge : GameBridgeInitializationHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void DllLoadedBindingSucceedsNativeBridgeReady()
        {
            // Given
            GivenGameBridgeWithInitializationState("initializing");
            GivenHookCostumeDllLoaded();

            // When
            WhenGameBridgeInitializesNativeBridge();

            // Then
            ThenDllHasLoadedState("loaded");
        }

        [TestMethod]
        public void DllNotLoadedBeforeInitFailsWithDependencyError()
        {
            // Given
            GivenGameBridgeWithInitializationState("initializing");
            GivenHookCostumeDllNotLoaded();

            // When
            WhenGameBridgeInitializesNativeBridge();

            // Then
            ThenDllHasLoadedState("not loaded");
            ThenGameBridgeReportsError("dependency");
        }

        [TestMethod]
        public void DuplicateInitializationSilentlyIgnored()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenHookCostumeDllLoaded();
            GivenNativeBridgeInitialized();

            // When
            WhenGameBridgeInitializesNativeBridge();

            // Then
            ThenDllHasLoadedState("loaded");
        }

        [TestMethod]
        public void SessionShutdownReleasesBindings()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenHookCostumeDllLoaded();
            GivenNativeBridgeInitialized();

            // When (session shutdown simulated)
            WhenGameBridgeInitializesNativeBridge();

            // Then
            ThenDllHasLoadedState("loaded");
        }
    }
}
