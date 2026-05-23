using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CameraRig
{
    [TestClass]
    public class DeployCameraEnableAndDisableScripts : CameraRigHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void EnableScriptDeployedRigActive()
        {
            // Given
            GivenGameBridgeInitialized();

            // When
            WhenGmActivatesCameraRig();

            // Then
            ThenScriptDeployed("enable");
            ThenCameraRigState("active");
        }

        [TestMethod]
        public void DisableScriptDeployedRigRemoved()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");

            // When
            WhenGmDeactivatesCameraRig();

            // Then
            ThenScriptDeployed("disable");
            ThenCameraRigState("inactive");
        }

        [TestMethod]
        public void EnableOnAlreadyActiveRigNoOp()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");

            // When
            WhenGmActivatesCameraRig();

            // Then
            ThenCameraRigState("active");
        }
    }
}
