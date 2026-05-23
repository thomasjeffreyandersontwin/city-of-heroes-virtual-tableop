using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ResourceCatalogLoading
{
    [TestClass]
    public class LoadFxResourceCatalog : ResourceCatalogLoadingHelper
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
            GivenCatalogDataFileExists("FxRepo.data");

            // When
            WhenApplicationReadsDataFile("FxRepo.data");

            // Then
            ThenResourceCatalogHasState("loaded");
            ThenResourcePickerEnabled("FX");
        }

        [TestMethod]
        public void FileMissingAtStartupCatalogNotLoaded()
        {
            // Given
            GivenApplicationStarting();
            GivenCatalogDataFileMissing("FxRepo.data");

            // When
            WhenApplicationReadsDataFile("FxRepo.data");

            // Then
            ThenResourceCatalogHasState("not loaded");
        }

        [TestMethod]
        public void ResourcePickerBlockedBeforeCatalogLoadCompletes()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogNotLoaded("FX");

            // When
            WhenResourcePickerInteractionAttempted();

            // Then
            ThenOperationBlockedWithNotReady();
        }
    }
}
