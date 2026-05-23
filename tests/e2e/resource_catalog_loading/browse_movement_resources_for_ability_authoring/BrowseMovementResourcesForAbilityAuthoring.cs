using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ResourceCatalogLoading
{
    [TestClass]
    public class BrowseMovementResourcesForAbilityAuthoring : ResourceCatalogLoadingHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void MovementResourcePickerShowsFly()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("Movement");
            GivenAbilityEditorOpenForAbility("TestAbility");

            // When
            WhenGmSelectsAddResource("Movement");

            // Then
            ThenResourcePickerShowsEntry("Fly", "MOV_Fly_01");
        }

        [TestMethod]
        public void MovementResourcePickerShowsSuperJump()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("Movement");
            GivenAbilityEditorOpenForAbility("TestAbility");

            // When
            WhenGmSelectsAddResource("Movement");

            // Then
            ThenResourcePickerShowsEntry("Super Jump", "MOV_SuperJump_01");
        }

        [TestMethod]
        public void GmSelectsMovementResourceAndConfirms()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("Movement");
            GivenAbilityEditorOpenForAbility("TestAbility");
            GivenResourcePickerShowing("Movement");

            // When
            WhenGmSelectsResourceAndConfirms("Movement", "Fly");

            // Then
            ThenElementAdded("Movement", "Fly");
            ThenElementAtBottom("Fly");
        }

        [TestMethod]
        public void GmDismissesPickerWithoutSelectingMovement()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("Movement");
            GivenAbilityEditorOpenForAbility("TestAbility");
            GivenResourcePickerShowing("Movement");

            // When
            WhenGmDismissesPicker();

            // Then
            ThenNoElementAdded();
        }

        [TestMethod]
        public void MovementResourceCatalogNotYetLoaded()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogNotLoaded("Movement");
            GivenAbilityEditorOpenForAbility("TestAbility");

            // Then
            ThenResourcePickerDisabledOrNotReady("Movement");
        }
    }
}
