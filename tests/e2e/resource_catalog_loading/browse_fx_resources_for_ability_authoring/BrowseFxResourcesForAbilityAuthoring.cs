using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ResourceCatalogLoading
{
    [TestClass]
    public class BrowseFxResourcesForAbilityAuthoring : ResourceCatalogLoadingHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void FxResourcePickerShowsFireBlast()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("FX");
            GivenAbilityEditorOpenForAbility("TestAbility");

            // When
            WhenGmSelectsAddResource("FX");

            // Then
            ThenResourcePickerShowsEntry("Fire Blast", "FX_FireBlast_01");
        }

        [TestMethod]
        public void FxResourcePickerShowsIceShield()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("FX");
            GivenAbilityEditorOpenForAbility("TestAbility");

            // When
            WhenGmSelectsAddResource("FX");

            // Then
            ThenResourcePickerShowsEntry("Ice Shield", "FX_IceShield_02");
        }

        [TestMethod]
        public void GmSelectsFxResourceAndConfirms()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("FX");
            GivenAbilityEditorOpenForAbility("TestAbility");
            GivenResourcePickerShowing("FX");

            // When
            WhenGmSelectsResourceAndConfirms("FX", "Fire Blast");

            // Then
            ThenElementAdded("FX", "Fire Blast");
            ThenElementAtBottom("Fire Blast");
        }

        [TestMethod]
        public void GmDismissesPickerWithoutSelectingFx()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("FX");
            GivenAbilityEditorOpenForAbility("TestAbility");
            GivenResourcePickerShowing("FX");

            // When
            WhenGmDismissesPicker();

            // Then
            ThenNoElementAdded();
        }

        [TestMethod]
        public void EmptyFxResourceCatalog()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("FX");
            GivenCatalogEmpty("FX");
            GivenAbilityEditorOpenForAbility("TestAbility");

            // When
            WhenGmSelectsAddResource("FX");

            // Then
            ThenPickerShowsEmptyState();
        }
    }
}
