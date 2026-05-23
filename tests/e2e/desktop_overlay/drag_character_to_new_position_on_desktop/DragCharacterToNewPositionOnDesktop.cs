using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.DesktopOverlay
{
    [TestClass]
    public class DragCharacterToNewPositionOnDesktop : DesktopOverlayHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void SpawnedDragRepositions()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            WhenGmDragsOverlay("Guard_Captain_01", "200.0", "0.0", "-100.0");
            ThenOverlayPosition("Guard_Captain_01", "(200.0, 0.0, -100.0)");
        }

        [TestMethod]
        public void OutOfBoundsDragCancelled()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            WhenGmDragsOverlay("Guard_Captain_01", "99999.0", "0.0", "-99999.0");
            ThenOverlayPosition("Guard_Captain_01", "original_position");
        }

        [TestMethod]
        public void NotSpawnedDragUnavailable()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            ThenDragUnavailable("Guard_Captain_01");
        }

        [TestMethod]
        public void CollisionHaltsAtBoundary()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            WhenGmDragsOverlay("Guard_Captain_01", "500.0", "0.0", "-500.0");
            ThenOverlayPosition("Guard_Captain_01", "collision_point");
        }

        [TestMethod]
        public void MultiSelectAllMoveTogether()
        {
            GivenSpawnedState("Guard_A", "true");
            GivenSpawnedState("Guard_B", "true");
            GivenSpawnedState("Guard_C", "true");
            GivenMultiSelect(new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenGmDragsOverlay("Guard_A", "200.0", "0.0", "-100.0");
            ThenOverlayPosition("Guard_A", "relative_offset_positions");
        }
    }
}
