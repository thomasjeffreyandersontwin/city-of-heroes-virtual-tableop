using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ContextMenu
{
    [TestClass]
    public class PlaceCharacterAtLocation : ContextMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void ValidPositionPlacementSucceeds()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenMousePosition("authoritative", "(150.0, 0.0, -200.0)");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsPlaceAtLocation("Guard_Captain_01");
            ThenOverlayPosition("Guard_Captain_01", "(150.0, 0.0, -200.0)");
        }

        [TestMethod]
        public void NoFocusPlacementBlocked()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenMousePosition("potentially stale", "unavailable");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsPlaceAtLocation("Guard_Captain_01");
            ThenOverlayPosition("Guard_Captain_01", "unchanged");
            ThenFeedbackShown();
        }

        [TestMethod]
        public void NotSpawnedActionUnavailable()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            GivenTargetCharacter("Guard_Captain_01");
            ThenActionNotAvailable("PlaceAtLocation");
        }

        [TestMethod]
        public void CollisionAdjustedDestination()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenMousePosition("authoritative", "(150.0, 0.0, -200.0)");
            GivenTargetCharacter("Guard_Captain_01");
            GivenCollisionAtDestination();
            WhenGmSelectsPlaceAtLocation("Guard_Captain_01");
            ThenOverlayPosition("Guard_Captain_01", "collision_adjusted_point");
        }
    }
}
