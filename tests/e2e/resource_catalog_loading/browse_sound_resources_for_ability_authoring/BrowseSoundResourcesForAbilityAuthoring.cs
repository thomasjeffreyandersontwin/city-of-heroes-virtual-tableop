using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ResourceCatalogLoading
{
    [TestClass]
    public class BrowseSoundResourcesForAbilityAuthoring : ResourceCatalogLoadingHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SoundResourcePickerShowsThunderClap()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("Sound");
            GivenAbilityEditorOpenForAbility("TestAbility");

            // When
            WhenGmSelectsAddResource("Sound");

            // Then
            ThenResourcePickerShowsEntry("Thunder Clap", "SND_ThunderClap_01");
        }

        [TestMethod]
        public void SoundResourcePickerShowsWindGust()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("Sound");
            GivenAbilityEditorOpenForAbility("TestAbility");

            // When
            WhenGmSelectsAddResource("Sound");

            // Then
            ThenResourcePickerShowsEntry("Wind Gust", "SND_WindGust_01");
        }

        [TestMethod]
        public void GmSelectsSoundResourceAndConfirms()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("Sound");
            GivenAbilityEditorOpenForAbility("TestAbility");
            GivenResourcePickerShowing("Sound");

            // When
            WhenGmSelectsResourceAndConfirms("Sound", "Thunder Clap");

            // Then
            ThenElementAdded("Sound", "Thunder Clap");
            ThenElementAtBottom("Thunder Clap");
        }

        [TestMethod]
        public void GmDismissesPickerWithoutSelectingSound()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("Sound");
            GivenAbilityEditorOpenForAbility("TestAbility");
            GivenResourcePickerShowing("Sound");

            // When
            WhenGmDismissesPicker();

            // Then
            ThenNoElementAdded();
        }

        [TestMethod]
        public void EmptySoundResourceCatalog()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("Sound");
            GivenCatalogEmpty("Sound");
            GivenAbilityEditorOpenForAbility("TestAbility");

            // When
            WhenGmSelectsAddResource("Sound");

            // Then
            ThenPickerShowsEmptyState();
        }
    }
}
