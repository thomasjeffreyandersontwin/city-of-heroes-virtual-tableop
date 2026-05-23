using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimationElementAuthoring
{
    [TestClass]
    public class AddMovementElementToAbility : AnimationElementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidResourceSelectedMovementElementAdded()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("Movement");

            // When
            WhenGmAddsMovementElement("Fly");

            // Then
            ThenElementExists("Movement", "Fly");
            ThenElementAtBottom();
        }

        [TestMethod]
        public void MovementElementExecutedDuringAbilityPlay()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("Movement");
            GivenMovementElement("Fly", 1);
            GivenSpawnedNpcPresent("Guard_Captain");

            // When
            WhenAbilityExecutesElement("Movement", "Fly");

            // Then
            ThenGameCommandIssued("Movement", "Fly");
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void MovementResourceNotFoundAtExecutionTime()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("Movement");
            GivenMovementElement("Deleted_Move", 1);

            // When
            WhenAbilityExecutesElement("Movement", "Deleted_Move");

            // Then
            ThenElementProducesNoOp();
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void ReorderMovementElementViaDragDrop()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("Movement");
            GivenThreeElementsAtPositions();

            // When
            WhenGmDragDropsElement(3, 1);

            // Then
            ThenElementAtPosition("Elem3", 1);
        }
    }
}
