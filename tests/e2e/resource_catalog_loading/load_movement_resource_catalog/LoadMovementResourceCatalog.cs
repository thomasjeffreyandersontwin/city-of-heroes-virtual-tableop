using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ResourceCatalogLoading
{
    [TestClass]
    public class LoadMovementResourceCatalog : ResourceCatalogLoadingHelper
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
            GivenCatalogDataFileExists("MoveRepo.data");

            // When
            WhenApplicationReadsDataFile("MoveRepo.data");

            // Then
            ThenResourceCatalogHasState("loaded");
            ThenResourcePickerEnabled("Movement");
        }

        [TestMethod]
        public void FileMissingAtStartupCatalogNotLoaded()
        {
            // Given
            GivenApplicationStarting();
            GivenCatalogDataFileMissing("MoveRepo.data");

            // When
            WhenApplicationReadsDataFile("MoveRepo.data");

            // Then
            ThenResourceCatalogHasState("not loaded");
        }

        [TestMethod]
        public void MovementCatalogHeldForSessionDuration()
        {
            // Given
            GivenApplicationStarting();
            GivenResourceCatalogLoaded("Movement");

            // Then
            ThenResourceCatalogHasState("loaded");
        }
    }
}
