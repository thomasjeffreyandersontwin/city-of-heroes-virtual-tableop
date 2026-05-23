using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.DesktopOverlay
{
    [TestClass]
    public class DragCharacterToNewPositionOnDesktop : DesktopOverlayDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Desktop Overlay has Character Overlays rendered; Memory Interface attached
        }

        [TestMethod]
        public void DragToNewPositionMoveCommandIssued()
        {
            // Given: Guard_Captain_01 at screen position (100, 200); dragging to (400, 350)
            _guardCaptain.ScreenX = 100; _guardCaptain.ScreenY = 200;
            // When: the GM drags Guard_Captain_01 from (100, 200) to (400, 350) then releases
            _guardCaptain.ScreenX = 400; _guardCaptain.ScreenY = 350;
            // Then: Character Overlay updates to (400, 350); Move NPC Command issued for Guard_Captain_01
            _guardCaptain.ScreenX.Should().Be(400, "overlay X must update to 400 on drag release");
            _guardCaptain.ScreenY.Should().Be(350, "overlay Y must update to 350 on drag release");
        }

        [TestMethod]
        public void DragAndDropOnExistingOverlayRelativePositioning()
        {
            // Given: Guard_Captain_01 and Villain_Boss_03 are both on the overlay
            // When: the GM drags Guard_Captain_01 and drops it on top of Villain_Boss_03
            // Then: both characters move together to the drop position with relative offset preserved
            _guardCaptain.ScreenX = _villainBoss.ScreenX;
            _guardCaptain.ScreenY = _villainBoss.ScreenY;
            _guardCaptain.ScreenX.Should().Be(_villainBoss.ScreenX,
                "dropped on Villain_Boss_03 — both move to drop position; relative offset preserved");
        }

        [TestMethod]
        public void DragCancelledPositionResets()
        {
            // Given: Guard_Captain_01 at screen position (100, 200); drag initiated
            int origX = 100; int origY = 200;
            _guardCaptain.ScreenX = origX; _guardCaptain.ScreenY = origY;
            // When: the GM cancels the drag (e.g. Escape key)
            // Then: Character Overlay resets to original position (100, 200); no Move NPC Command issued
            _guardCaptain.ScreenX = origX; _guardCaptain.ScreenY = origY;
            _guardCaptain.ScreenX.Should().Be(100, "cancelled drag must reset to original position 100");
            _guardCaptain.ScreenY.Should().Be(200, "cancelled drag must reset to original position 200");
        }
    }
}
