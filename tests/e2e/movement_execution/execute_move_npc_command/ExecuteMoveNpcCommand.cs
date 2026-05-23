using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MovementExecution
{
    [TestClass]
    public class ExecuteMoveNpcCommand : MovementExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void RegisteredTargetCommandIssued()
        {
            // Given
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenSpawnedNpcPresent("Guard_Captain_01");

            // When
            WhenMovementExecutionComputesDestination();

            // Then
            ThenMoveNpcCommandIssued("Guard_Captain_01", "200.0", "0.0", "-150.0");
        }

        [TestMethod]
        public void UnregisteredCommandHeld()
        {
            // Given
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("pending");

            // When
            WhenMovementExecutionComputesDestination();

            // Then
            ThenCommandHeld();
        }

        [TestMethod]
        public void NameHasNoMatchingNpcNoOp()
        {
            // Given
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");

            // When
            WhenMovementExecutionComputesDestination();

            // Then
            ThenCommandNoOp();
        }
    }
}
