using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CameraRig
{
    [TestClass]
    public class ExecuteCameraDetachCommand : CameraRigHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void FollowActiveDetachTerminates()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");
            GivenCameraFollowState("active", "Guard_Captain_01");
            GivenManeuverWithCameraModeState("inactive");

            // When
            WhenGmTriggersCameraDetach();

            // Then
            ThenCameraFollowState("inactive", "none");
            ThenManeuverModeState("inactive");
        }

        [TestMethod]
        public void NoFollowActiveNoOp()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");
            GivenCameraFollowState("inactive", "none");
            GivenManeuverWithCameraModeState("inactive");

            // When
            WhenGmTriggersCameraDetach();

            // Then
            ThenCameraFollowState("inactive", "none");
            ThenNoError();
        }

        [TestMethod]
        public void ManeuverModeAlsoActiveBothEnd()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");
            GivenCameraFollowState("active", "Guard_Captain_01");
            GivenManeuverWithCameraModeState("active");

            // When
            WhenGmTriggersCameraDetach();

            // Then
            ThenCameraFollowState("inactive", "none");
            ThenManeuverModeState("inactive");
        }
    }
}
