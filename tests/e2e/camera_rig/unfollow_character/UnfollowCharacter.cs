using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CameraRig
{
    [TestClass]
    public class UnfollowCharacter : CameraRigHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ActiveFollowTerminated()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");
            GivenCameraFollowState("active", "Guard_Captain_01");

            // When
            WhenGmTriggersUnfollow();

            // Then
            ThenCameraFollowState("inactive", "none");
            ThenCameraInFreeRoam();
        }

        [TestMethod]
        public void NoFollowActiveNoOp()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");
            GivenCameraFollowState("inactive", "none");

            // When
            WhenGmTriggersUnfollow();

            // Then
            ThenCameraFollowState("inactive", "none");
            ThenNoError();
        }
    }
}
