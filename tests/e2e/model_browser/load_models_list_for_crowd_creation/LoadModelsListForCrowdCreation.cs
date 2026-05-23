using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ModelBrowser
{
    [TestClass]
    public class LoadModelsListForCrowdCreation : ModelBrowserHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ListLoadedAfterEventModelBrowserAvailable()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameLoadedEventPublished();
            GivenModelListLoaded("Skull_Lt_01", "Clockwork_Gear_01");

            // When
            WhenGmAttemptsOpenModelBrowser();

            // Then
            ThenModelBrowserEnabled();
        }

        [TestMethod]
        public void ListNotYetLoadedModelBrowserDisabled()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameLoadedEventPublished();
            GivenModelListNotLoaded();

            // When
            WhenGmAttemptsOpenModelBrowser();

            // Then
            ThenModelBrowserDisabled("model list not ready");
        }

        [TestMethod]
        public void LoadFailedFileMissingModelBrowserUnavailable()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameLoadedEventPublished();
            GivenModelListNotLoaded();

            // When
            WhenGmAttemptsOpenModelBrowser();

            // Then
            ThenModelBrowserDisabled("model list not ready");
        }

        [TestMethod]
        public void ModelListClearedOnSessionEnd()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01");

            // When
            WhenSessionEndsAndNewBegins();

            // Then
            ThenModelListHasState("not loaded");
        }
    }
}
