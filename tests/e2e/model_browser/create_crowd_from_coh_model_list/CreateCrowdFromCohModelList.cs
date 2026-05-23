using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ModelBrowser
{
    [TestClass]
    public class CreateCrowdFromCohModelList : ModelBrowserHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void TwoModelsSelectedCrowdCreatedWithCharacters()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01", "Clockwork_Gear_01");
            GivenModelBrowserOpen();
            GivenModelSelected("Skull_Lt_01");
            GivenModelSelected("Clockwork_Gear_01");

            // When
            WhenGmChoosesCreateCrowdFromSelection();

            // Then
            ThenCrowdCreatedWithCharacterCount(2);
        }

        [TestMethod]
        public void NoModelsSelectedActionDisabled()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01");
            GivenModelBrowserOpen();
            GivenNoModelsSelected();

            // Then
            ThenCreateCrowdButtonDisabled();
        }

        [TestMethod]
        public void CrowdNameConflictsWithExistingPromptsForUniqueName()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01");
            GivenModelBrowserOpen();
            GivenModelSelected("Skull_Lt_01");
            GivenExistingCrowdWithName("New Crowd");

            // When
            WhenGmChoosesCreateCrowdFromSelection();

            // Then — user prompted (validation message)
            ThenErrorReported("unique crowd name");
        }

        [TestMethod]
        public void CancelModelBrowserNoCrowdCreated()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01");
            GivenModelBrowserOpen();
            GivenModelSelected("Skull_Lt_01");

            // When
            WhenGmCancelsModelBrowser();

            // Then
            ThenCrowdRepositoryUnchanged();
        }
    }
}
