using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimationElementAuthoring
{
    [TestClass]
    public class ReorderAnimationElementsViaDragDrop : AnimationElementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ElementMovedToNewPosition()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenThreeElementsAtPositions();

            // When
            WhenGmDragDropsElement(3, 1);

            // Then
            ThenElementAtPosition("Elem3", 1);
            ThenElementAtPosition("Elem1", 2);
            ThenElementAtPosition("Elem2", 3);
        }

        [TestMethod]
        public void ElementDroppedInSamePosition()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenThreeElementsAtPositions();

            // When
            WhenGmDragDropsElement(2, 2);

            // Then
            ThenElementListUnchanged();
        }

        [TestMethod]
        public void SavePersistsNewOrder()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenThreeElementsAtPositions();
            WhenGmDragDropsElement(3, 1);

            // When
            WhenGmSaves();

            // Then
            ThenElementAtPosition("Elem3", 1);
        }

        [TestMethod]
        public void CancelRevertsReorder()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenThreeElementsAtPositions();
            WhenGmDragDropsElement(3, 1);

            // When
            WhenGmCancels();

            // Then
            ThenElementAtPosition("Elem1", 1);
            ThenElementAtPosition("Elem2", 2);
            ThenElementAtPosition("Elem3", 3);
        }

        [TestMethod]
        public void MultipleReordersBeforeSave()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenThreeElementsAtPositions();
            WhenGmDragDropsElement(3, 1);
            WhenGmDragDropsElement(2, 3);

            // When
            WhenGmSaves();

            // Then — final order is persisted
            ThenElementAtPosition("Elem3", 1);
        }
    }
}
