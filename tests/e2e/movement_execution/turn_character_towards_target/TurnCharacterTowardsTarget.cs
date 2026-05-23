using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MovementExecution
{
    [TestClass]
    public class TurnCharacterTowardsTarget : MovementExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void TurnToNpcTarget()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterFacingVector("0.0", "0.0", "1.0");

            // When
            WhenGmTriggersTurnToTarget();

            // Then
            ThenRotationMatrixWritten("computed_bearing_matrix");
        }

        [TestMethod]
        public void AlreadyFacingTargetNoOp()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterFacingVector("1.0", "0.0", "0.0");

            // When
            WhenGmTriggersTurnToTarget();

            // Then
            ThenNoRotationWriteIssued();
        }

        [TestMethod]
        public void TurnToLocationPoint()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterFacingVector("0.0", "0.0", "1.0");

            // When
            WhenGmTriggersTurnToTarget();

            // Then
            ThenRotationMatrixWritten("computed_location_matrix");
        }
    }
}
