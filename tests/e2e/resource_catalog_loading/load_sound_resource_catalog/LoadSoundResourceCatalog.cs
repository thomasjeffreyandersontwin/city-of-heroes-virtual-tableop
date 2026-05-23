using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ResourceCatalogLoading
{
    [TestClass]
    public class LoadSoundResourceCatalog : ResourceCatalogLoadingHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void FilePresentAndValidCatalogLoaded()
        {
            // Given
            GivenApplicationStarting();
            GivenCatalogDataFileExists("SoundRepo.data");

            // When
            WhenApplicationReadsDataFile("SoundRepo.data");

            // Then
            ThenResourceCatalogHasState("loaded");
            ThenResourcePickerEnabled("Sound");
        }

        [TestMethod]
        public void FileMissingAtStartupCatalogNotLoaded()
        {
            // Given
            GivenApplicationStarting();
            GivenCatalogDataFileMissing("SoundRepo.data");

            // When
            WhenApplicationReadsDataFile("SoundRepo.data");

            // Then
            ThenResourceCatalogHasState("not loaded");
        }

        [TestMethod]
        public void AllThreeCatalogsLoadedEnablesAbilityEditor()
        {
            // Given
            GivenApplicationStarting();
            GivenAllCatalogsLoaded();

            // Then
            ThenAllResourcePickersEnabled();
        }
    }
}
