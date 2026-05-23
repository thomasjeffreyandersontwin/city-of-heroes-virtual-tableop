using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MovementExecution
{
    [TestClass]
    public class DetectFloorAndWallCollisions : MovementExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void NoCollisionStepProceeds()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "100");
            GivenMovementExecutionInProgress();

            // When
            WhenMovementStepComputed();

            // Then
            ThenNoCollisionDetected();
            ThenMovementProceeds();
        }

        [TestMethod]
        public void FloorCollisionDetectedAnchor()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "100");
            GivenMovementExecutionInProgress();
            GivenFloorCollisionWillOccur();

            // When
            WhenMovementStepComputed();

            // Then
            ThenFloorCollisionDetected();
        }

        [TestMethod]
        public void BothFloorAndWallOnSameStep()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "100");
            GivenMovementExecutionInProgress();
            GivenBothCollisionsWillOccur();

            // When
            WhenMovementStepComputed();

            // Then
            ThenFloorCollisionDetected();
            ThenWallCollisionDetected();
            ThenMovementHalted();
        }

        [TestMethod]
        public void FlyMovementSkipsFloorCollision()
        {
            // Given — levitate = true; floor collision simulated but should be ignored
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Fly", "Fly", "100");
            GivenMovementExecutionInProgress();
            GivenFloorCollisionWillOccur();

            // When
            WhenMovementStepComputed();

            // Then — floor collision not detected; step proceeds
            ThenNoFloorCollisionDetected();
            ThenMovementProceeds();
        }

        [TestMethod]
        public void JumpMovementSkipsFloorCollision()
        {
            // Given — levitate = true
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Jump", "Jump", "100");
            GivenMovementExecutionInProgress();
            GivenFloorCollisionWillOccur();

            // When
            WhenMovementStepComputed();

            // Then
            ThenNoFloorCollisionDetected();
            ThenMovementProceeds();
        }

        [TestMethod]
        public void SwimMovementSkipsFloorCollision()
        {
            // Given — levitate = true
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Swim", "Swim", "100");
            GivenMovementExecutionInProgress();
            GivenFloorCollisionWillOccur();

            // When
            WhenMovementStepComputed();

            // Then
            ThenNoFloorCollisionDetected();
            ThenMovementProceeds();
        }

        [TestMethod]
        public void LevitatingMovementStillDetectsWallCollision()
        {
            // Wall collision applies regardless of levitate; only floor is skipped
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Fly", "Fly", "100");
            GivenMovementExecutionInProgress();
            GivenWallCollisionWillOccur();

            // When
            WhenMovementStepComputed();

            // Then
            ThenWallCollisionDetected();
        }
    }
}
