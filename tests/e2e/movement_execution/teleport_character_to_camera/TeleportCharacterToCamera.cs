using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MovementExecution
{
    [TestClass]
    public class TeleportCharacterToCamera : MovementExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void RegisteredInstantTeleport()
        {
            // Given
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenMemoryPointerState("cameraPosition", "valid");

            // When
            WhenGmTriggersTeleportToCamera();

            // Then
            ThenTeleportCompleted();
            ThenNoAnimationPlayed();
        }

        [TestMethod]
        public void UnregisteredTeleportBlocked()
        {
            // Given
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("pending");

            // When
            WhenGmTriggersTeleportToCamera();

            // Then
            ThenTeleportBlocked();
        }

        [TestMethod]
        public void StalePointerRefreshThenTeleport()
        {
            // Given
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenMemoryPointerState("cameraPosition", "stale");

            // When
            WhenGmTriggersTeleportToCamera();

            // Then
            ThenTeleportCompleted();
        }
    }
}
