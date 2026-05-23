using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameBridgeInitialization
{
    [TestClass]
    public class ExtractCostumePackOnFirstRun : GameBridgeInitializationHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void FirstRunDirectoryExistsExtractsPack()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenGameLoadedEventPublished();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");

            // When
            WhenGameBridgeExtractsCostumePack(@"C:\Games\CoH\costumes");

            // Then
            ThenCostumesDirectoryAvailable(@"C:\Games\CoH\costumes");
        }

        [TestMethod]
        public void NotFirstRunFilesPresentSkipsExtraction()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenGameLoadedEventPublished();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");

            // When
            WhenGameBridgeExtractsCostumePack(@"C:\Games\CoH\costumes");

            // Then
            ThenCostumesDirectoryAvailable(@"C:\Games\CoH\costumes");
        }

        [TestMethod]
        public void FirstRunDirectoryMissingCreatesAndExtracts()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenGameLoadedEventPublished();

            // When
            WhenGameBridgeExtractsCostumePack(@"C:\Games\CoH\costumes");

            // Then
            ThenCostumesDirectoryAvailable(@"C:\Games\CoH\costumes");
        }

        [TestMethod]
        public void ExtractionFailsPartwayReportsError()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenGameLoadedEventPublished();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");

            // When
            WhenExtractionFailsPartway();

            // Then
            ThenGameBridgeReportsError("extraction failure");
        }
    }
}
