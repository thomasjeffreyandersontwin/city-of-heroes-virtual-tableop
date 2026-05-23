using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameBridgeInitialization
{
    [TestClass]
    public class LoadHookCostumeDllFromGameDirectory : GameBridgeInitializationHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void DllPresentAndValidLoadSucceeds()
        {
            // Given
            GivenGameBridgeWithInitializationState("uninitialized");
            GivenCohGameDirectoryBasePath(@"C:\Games\CoH");

            // When
            WhenGameBridgeAttemptsLoadHookCostumeDll(@"C:\Games\CoH");

            // Then
            ThenDllHasLoadedState("loaded");
            ThenGameBridgeHasInitializationState("initializing");
        }

        [TestMethod]
        public void DllAbsentFromDirectoryLoadFails()
        {
            // Given
            GivenGameBridgeWithInitializationState("uninitialized");
            GivenCohGameDirectoryBasePath(@"C:\Games\CoH");
            GivenDllLoadWillFail();

            // When
            WhenGameBridgeAttemptsLoadHookCostumeDll(@"C:\Games\CoH");

            // Then
            ThenDllHasLoadedState("not loaded");
            ThenGameBridgeHasInitializationState("uninitialized");
        }

        [TestMethod]
        public void DllPresentButWrongArchitectureLoadFails()
        {
            // Given
            GivenGameBridgeWithInitializationState("uninitialized");
            GivenCohGameDirectoryBasePath(@"C:\Games\CoH");
            GivenDllLoadWillFail();

            // When
            WhenGameBridgeAttemptsLoadHookCostumeDll(@"C:\Games\CoH");

            // Then
            ThenDllHasLoadedState("not loaded");
            ThenGameBridgeHasInitializationState("uninitialized");
        }

        [TestMethod]
        public void LoadDeferredUntilCohGameDirectoryValidated()
        {
            // Given
            GivenGameBridgeWithInitializationState("uninitialized");
            GivenCohGameDirectoryNotValidated();

            // When
            WhenGameBridgeAttemptsLoadHookCostumeDll();

            // Then
            ThenNoLoadAttemptMade();
        }
    }
}
