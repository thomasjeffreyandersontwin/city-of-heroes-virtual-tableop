using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ContextMenu
{
    [TestClass]
    public class ManeuverCharacterWithCameraViaContextMenu : ContextMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void SpawnedModeActivated()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenCameraRigState("active");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsManeuverWithCamera("Guard_Captain_01");
            ThenManeuverModeState("active");
        }

        [TestMethod]
        public void CameraRigNotActiveBlocked()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenCameraRigState("inactive");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsManeuverWithCamera("Guard_Captain_01");
            ThenFeedbackShown();
            ThenManeuverModeState("inactive");
        }

        [TestMethod]
        public void AlreadyActiveToggleDeactivates()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenCameraRigState("active");
            GivenManeuverModeActive();
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsManeuverWithCamera("Guard_Captain_01");
            ThenManeuverModeState("inactive");
        }

        [TestMethod]
        public void NotSpawnedActionUnavailable()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            GivenTargetCharacter("Guard_Captain_01");
            ThenActionNotAvailable("ManeuverWithCamera");
        }

        [TestMethod]
        public void ModeActiveGmMovesCharacterInCameraDirection()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenCameraRigState("active");
            GivenManeuverModeActive();
            GivenTargetCharacter("Guard_Captain_01");
            ThenManeuverModeState("active");
        }
    }
}
