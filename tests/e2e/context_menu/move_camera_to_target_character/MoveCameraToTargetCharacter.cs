using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ContextMenu
{
    [TestClass]
    public class MoveCameraToTargetCharacter : ContextMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void SpawnedCameraMoves()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenCameraRigState("active");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsMoveCameraToTarget("Guard_Captain_01");
            ThenCameraMovedToTarget();
        }

        [TestMethod]
        public void CameraRigNotActiveUnavailable()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenCameraRigState("inactive");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsMoveCameraToTarget("Guard_Captain_01");
            ThenFeedbackShown();
        }

        [TestMethod]
        public void NotSpawnedActionUnavailable()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            GivenTargetCharacter("Guard_Captain_01");
            ThenActionNotAvailable("MoveCameraToTarget");
        }
    }
}
