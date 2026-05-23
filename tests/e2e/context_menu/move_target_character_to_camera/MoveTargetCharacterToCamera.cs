using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ContextMenu
{
    [TestClass]
    public class MoveTargetCharacterToCamera : ContextMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void SpawnedMoveToCamera()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenCameraRigState("active");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsMoveTargetToCamera("Guard_Captain_01");
            ThenOverlayPosition("Guard_Captain_01", "camera_position");
        }

        [TestMethod]
        public void CameraRigNotActiveUnchanged()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenCameraRigState("inactive");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsMoveTargetToCamera("Guard_Captain_01");
            ThenOverlayPosition("Guard_Captain_01", "unchanged");
            ThenFeedbackShown();
        }

        [TestMethod]
        public void CollisionOnPathToCameraStopsAtCollision()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenCameraRigState("active");
            GivenTargetCharacter("Guard_Captain_01");
            GivenCollisionOnPath();
            WhenGmSelectsMoveTargetToCamera("Guard_Captain_01");
            ThenOverlayPosition("Guard_Captain_01", "collision_point");
        }

        [TestMethod]
        public void NotSpawnedActionUnavailable()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            GivenTargetCharacter("Guard_Captain_01");
            ThenActionNotAvailable("MoveTargetToCamera");
        }
    }
}
