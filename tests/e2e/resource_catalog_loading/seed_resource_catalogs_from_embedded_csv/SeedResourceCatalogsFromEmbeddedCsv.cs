using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ResourceCatalogLoading
{
    [TestClass]
    public class SeedResourceCatalogsFromEmbeddedCsv : ResourceCatalogLoadingHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void FxCatalogSeededFromEmbeddedCsvOnFirstRun()
        {
            // Given
            GivenApplicationStarting();
            GivenCatalogDataFileMissing("FxRepo.data");
            GivenEmbeddedCsvHasData("FX", "FX resource entries");

            // When
            WhenApplicationSeedsCatalogFromEmbeddedData("FX");

            // Then
            ThenResourceCatalogHasState("loaded");
        }

        [TestMethod]
        public void MovementCatalogSeededFromEmbeddedCsvOnFirstRun()
        {
            // Given
            GivenApplicationStarting();
            GivenCatalogDataFileMissing("MoveRepo.data");
            GivenEmbeddedCsvHasData("Movement", "movement entries");

            // When
            WhenApplicationSeedsCatalogFromEmbeddedData("Movement");

            // Then
            ThenResourceCatalogHasState("loaded");
        }

        [TestMethod]
        public void SoundCatalogSeededFromEmbeddedCsvOnFirstRun()
        {
            // Given
            GivenApplicationStarting();
            GivenCatalogDataFileMissing("SoundRepo.data");
            GivenEmbeddedCsvHasData("Sound", "sound entries");

            // When
            WhenApplicationSeedsCatalogFromEmbeddedData("Sound");

            // Then
            ThenResourceCatalogHasState("loaded");
        }

        [TestMethod]
        public void DataFileAlreadyExistsEmbeddedCsvNotRead()
        {
            // Given
            GivenApplicationStarting();
            GivenCatalogDataFileExists("FxRepo.data");

            // When
            WhenApplicationStarts();

            // Then
            ThenEmbeddedCsvNotRead("FX");
        }

        [TestMethod]
        public void EmbeddedCsvAbsentOrUnreadable()
        {
            // Given
            GivenApplicationStarting();
            GivenCatalogDataFileMissing("FxRepo.data");
            GivenEmbeddedCsvAbsent("FX");

            // When
            WhenApplicationSeedsCatalogFromEmbeddedData("FX");

            // Then
            ThenCatalogUnavailableReported("FX");
        }
    }
}
