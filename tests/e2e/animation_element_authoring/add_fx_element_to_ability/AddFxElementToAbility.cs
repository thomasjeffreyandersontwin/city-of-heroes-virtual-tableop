using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimationElementAuthoring
{
    [TestClass]
    public class AddFxElementToAbility : AnimationElementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidResourceSelectedFxElementAdded()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("FX");

            // When
            WhenGmAddsFxElement("Fire Blast");

            // Then
            ThenElementExists("FX", "Fire Blast");
            ThenElementAtBottom();
        }

        [TestMethod]
        public void FxElementExecutedDuringAbilityPlay()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("FX");
            GivenFxElement("Fire Blast", 1);
            GivenSpawnedNpcPresent("Guard_Captain");

            // When
            WhenAbilityExecutesElement("FX", "Fire Blast");

            // Then
            ThenGameCommandIssued("FX", "Fire Blast");
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void FxResourceNotFoundAtExecutionTime()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("FX");
            GivenFxElement("Deleted_FX", 1);

            // When
            WhenAbilityExecutesElement("FX", "Deleted_FX");

            // Then
            ThenElementProducesNoOp();
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void SpawnedNpcNotPresentWhenFxElementExecutes()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("FX");
            GivenFxElement("Fire Blast", 1);
            GivenNoSpawnedNpc("Guard_Captain");

            // When
            WhenAbilityExecutesElement("FX", "Fire Blast");

            // Then
            ThenElementProducesNoOp();
            ThenSubsequentElementsContinue();
        }
    }
}
