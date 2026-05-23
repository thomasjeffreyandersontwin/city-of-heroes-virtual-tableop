using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MovementExecution
{
    [TestClass]
    public class MoveCharacterToCameraPosition : MovementExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void CameraRigActiveNormalMove()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCameraRigState("active");
            GivenCharacterMovementActive("Walk", "Walk", "100");

            // When
            WhenGmTriggersMoveToCameraPosition();

            // Then
            ThenMovementProceeds();
        }

        [TestMethod]
        public void CameraRigInactiveRawCoordsUsed()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCameraRigState("inactive");
            GivenCharacterMovementActive("Walk", "Walk", "100");

            // When
            WhenGmTriggersMoveToCameraPosition();

            // Then
            ThenMovementProceeds();
        }
    }
}
