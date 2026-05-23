using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ModelBrowser
{
    [TestClass]
    public class SelectModelsToIncludeInCrowd : ModelBrowserHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SelectModelMarkedInBrowser()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01", "Clockwork_Gear_01");
            GivenModelBrowserOpen();

            // When
            WhenGmSelectsModel("Skull_Lt_01");

            // Then
            ThenModelMarkedAsSelected("Skull_Lt_01");
            ThenCreateCrowdButtonEnabled();
        }

        [TestMethod]
        public void SelectAnotherModelBothSelected()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01", "Clockwork_Gear_01");
            GivenModelBrowserOpen();
            GivenModelSelected("Skull_Lt_01");

            // When
            WhenGmSelectsModel("Clockwork_Gear_01");

            // Then
            ThenModelMarkedAsSelected("Skull_Lt_01");
            ThenModelMarkedAsSelected("Clockwork_Gear_01");
            ThenCreateCrowdButtonEnabled();
        }

        [TestMethod]
        public void DeselectRemovesFromSelection()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01");
            GivenModelBrowserOpen();
            GivenModelSelected("Skull_Lt_01");

            // When
            WhenGmDeselectsModel("Skull_Lt_01");

            // Then
            ThenModelNotSelected("Skull_Lt_01");
            ThenCreateCrowdButtonDisabled();
        }

        [TestMethod]
        public void FilterPreservesSelections()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01", "Clockwork_Gear_01");
            GivenModelBrowserOpen();
            GivenModelSelected("Skull_Lt_01");

            // When — filter hides Skull_Lt_01
            WhenGmEntersFilter("Clockwork");

            // Then — still selected even though hidden
            WhenGmClearsFilter();
            ThenModelMarkedAsSelected("Skull_Lt_01");
        }
    }
}
