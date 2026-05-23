using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CameraRig
{
    [TestClass]
    public class ActivateManeuverWithCameraMode : CameraRigHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void RigActiveModeActivated()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");
            GivenMemoryInterfaceAttachedAndRegistered();

            // When
            WhenGmActivatesManeuverWithCameraMode();

            // Then
            ThenManeuverModeState("active");
        }

        [TestMethod]
        public void RigInactiveActivationBlocked()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("inactive");

            // When
            WhenGmActivatesManeuverWithCameraMode();

            // Then
            ThenManeuverModeState("inactive");
            ThenCommandBlocked("rig");
        }
    }
}
