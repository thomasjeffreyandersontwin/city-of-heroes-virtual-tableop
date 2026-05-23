using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ModelBrowser
{
    [TestClass]
    public class GenerateCharactersWithModelIdentities : ModelBrowserHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SingleModelCharacterNameMatchesModel()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01");

            // When
            WhenGmConfirmsCrowdCreation(new[] { "Skull_Lt_01" });

            // Then
            ThenCharacterNameIs("Skull_Lt_01");
            ThenCharacterHasModelIdentity("Skull_Lt_01", "Skull_Lt_01");
        }

        [TestMethod]
        public void DuplicateModelNamesFirstGetsBaseName()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01");

            // When
            WhenGmConfirmsCrowdCreation(new[] { "Skull_Lt_01", "Skull_Lt_01" });

            // Then
            ThenCharacterNameIs("Skull_Lt_01");
            ThenCharacterNameIs("Skull_Lt_01_2");
        }

        [TestMethod]
        public void DuplicateModelNamesSecondGetsSuffix()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01");

            // When
            WhenGmConfirmsCrowdCreation(new[] { "Skull_Lt_01", "Skull_Lt_01" });

            // Then
            ThenCharacterHasModelIdentity("Skull_Lt_01_2", "Skull_Lt_01");
        }

        [TestMethod]
        public void NameConflictsWithExistingCharacterGetsSuffix()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Guard_Captain");
            GivenExistingCrowdWithName("Existing");

            // When
            WhenGmConfirmsCrowdCreation(new[] { "Guard_Captain" });

            // Then
            ThenCharacterNameIs("Guard_Captain_2");
            ThenCharacterHasModelIdentity("Guard_Captain_2", "Guard_Captain");
        }

        [TestMethod]
        public void GeneratedCrowdContainsExactCountOfSelectedModels()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01", "Clockwork_Gear_01", "Hellion_Thug_01",
                "Tsoo_Ink_01", "Outcast_Brick_01");

            // When
            WhenGmConfirmsCrowdCreation(new[] {
                "Skull_Lt_01", "Clockwork_Gear_01", "Hellion_Thug_01",
                "Tsoo_Ink_01", "Outcast_Brick_01" });

            // Then
            ThenCrowdCreatedWithCharacterCount(5);
        }
    }
}
