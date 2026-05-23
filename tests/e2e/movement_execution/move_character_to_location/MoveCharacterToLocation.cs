using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MovementExecution
{
    [TestClass]
    public class MoveCharacterToLocation : MovementExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void DestinationReachedBeforeLimit()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "100");

            // When
            WhenGmTriggersMoveToLocation();

            // Then
            ThenCumulativeDistance(35);
        }

        [TestMethod]
        public void DistanceLimitReachedBeforeDestination()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "50");

            // When
            WhenGmTriggersMoveToLocation();

            // Then
            ThenCumulativeDistance(50);
            ThenMovementHalted();
        }

        [TestMethod]
        public void FloorCollisionHaltsVertical()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "100");
            GivenFloorCollisionWillOccur();

            // When
            WhenGmTriggersMoveToLocation();

            // Then
            ThenFloorCollisionDetected();
        }

        [TestMethod]
        public void WallCollisionHaltsHorizontal()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "100");
            GivenWallCollisionWillOccur();

            // When
            WhenGmTriggersMoveToLocation();

            // Then
            ThenWallCollisionDetected();
        }
    }
}
