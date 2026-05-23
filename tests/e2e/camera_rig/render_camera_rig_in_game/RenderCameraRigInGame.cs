using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CameraRig
{
    [TestClass]
    public class RenderCameraRigInGame : CameraRigHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void RigRenderedCommandProceeds()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");

            // When
            WhenCameraRelativeCommandAttempted();

            // Then
            ThenCommandProceeds();
        }

        [TestMethod]
        public void RigNotRenderedCommandBlocked()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("inactive");

            // When
            WhenCameraRelativeCommandAttempted();

            // Then
            ThenCommandBlocked("camera");
        }
    }
}
