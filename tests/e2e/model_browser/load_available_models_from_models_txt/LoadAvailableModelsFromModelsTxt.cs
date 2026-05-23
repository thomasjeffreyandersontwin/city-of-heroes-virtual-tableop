using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ModelBrowser
{
    [TestClass]
    public class LoadAvailableModelsFromModelsTxt : ModelBrowserHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void FilePresentAndValidAllModelsLoaded()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameLoadedEventPublished();
            GivenModelsTxtAt(@"C:\Games\CoH\Models.txt",
                new[] { "Skull_Lt_01", "Clockwork_Gear_01", "Hellion_Thug_01" });

            // When
            WhenHvtReadsModelsTxt(@"C:\Games\CoH\Models.txt");

            // Then
            ThenModelListHasState("loaded");
            ThenModelListContains("Skull_Lt_01", "Clockwork_Gear_01", "Hellion_Thug_01");
        }

        [TestMethod]
        public void FileAbsentFatalError()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameLoadedEventPublished();
            GivenModelsTxtAbsent(@"C:\Games\CoH\Models.txt");

            // When
            WhenHvtReadsModelsTxt(@"C:\Games\CoH\Models.txt");

            // Then
            ThenModelListHasState("not loaded");
            ThenErrorReported("Models.txt not found");
        }

        [TestMethod]
        public void FileHasMalformedLinesValidEntriesLoaded()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameLoadedEventPublished();
            GivenModelsTxtAt(@"C:\Games\CoH\Models.txt",
                new[] { "Skull_Lt_01", "[INVALID]", "Clockwork_Gear_01" });

            // When
            WhenHvtReadsModelsTxt(@"C:\Games\CoH\Models.txt");

            // Then
            ThenModelListHasState("loaded");
            ThenModelListContains("Skull_Lt_01", "Clockwork_Gear_01");
        }

        [TestMethod]
        public void FilePresentButEmptyLoadedWithEmptyCollection()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameLoadedEventPublished();
            GivenModelsTxtEmpty(@"C:\Games\CoH\Models.txt");

            // When
            WhenHvtReadsModelsTxt(@"C:\Games\CoH\Models.txt");

            // Then
            ThenModelListHasState("loaded");
            ThenModelListIsEmpty();
            ThenModelBrowserShowsNoModelsMessage();
        }

        [TestMethod]
        public void ModelListHeldForSessionDuration()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameLoadedEventPublished();
            GivenModelListLoaded("Skull_Lt_01", "Clockwork_Gear_01");

            // Then
            ThenModelListHasState("loaded");
            ThenModelListContains("Skull_Lt_01", "Clockwork_Gear_01");
        }
    }
}
