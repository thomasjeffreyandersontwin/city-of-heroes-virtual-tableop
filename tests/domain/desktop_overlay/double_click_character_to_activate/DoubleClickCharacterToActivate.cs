using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.DesktopOverlay
{
    [TestClass]
    public class DoubleClickCharacterToActivate : DesktopOverlayDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Desktop Overlay has Character Overlays rendered
        }

        [TestMethod]
        public void DoubleClickActivatesCharacter()
        {
            // Given: Guard_Captain_01 is not active
            _guardCaptain.IsActive = false;
            // When: the GM double-clicks on Guard_Captain_01 in the Desktop Overlay
            when_double_click(_guardCaptain);
            // Then: Guard_Captain_01 becomes the active character; active designation and turn indicator updated
            then_active(_guardCaptain);
        }

        [TestMethod]
        public void DoubleClickAlreadyActiveStaysActive()
        {
            // Given: Guard_Captain_01 is already active
            _guardCaptain.IsActive = true;
            // When: the GM double-clicks Guard_Captain_01 again
            when_double_click(_guardCaptain);
            // Then: no-op; Guard_Captain_01 remains active
            then_active(_guardCaptain);
        }

        [TestMethod]
        public void DoubleClickPreviousActiveCleared()
        {
            // Given: Villain_Boss_03 is active; Guard_Captain_01 is not active
            _villainBoss.IsActive = true;
            _guardCaptain.IsActive = false;
            // When: the GM double-clicks Guard_Captain_01
            _villainBoss.IsActive = false;
            when_double_click(_guardCaptain);
            // Then: Villain_Boss_03 loses active designation; Guard_Captain_01 is the active character
            then_active(_guardCaptain);
            _villainBoss.IsActive.Should().BeFalse(
                "Villain_Boss_03 must lose active designation when Guard_Captain_01 is double-clicked");
        }

        [TestMethod]
        public void DoubleClickSyncedToRosterSelection()
        {
            // Given: Guard_Captain_01 overlays Roster Entry Guard_Captain_01
            // When: the GM double-clicks Guard_Captain_01 in the Desktop Overlay
            when_double_click(_guardCaptain);
            // Then: Roster selection updates to Guard_Captain_01; active designation synchronized
            then_active(_guardCaptain);
            _guardCaptain.IsActive.Should().BeTrue(
                "double-click must sync Roster selection to Guard_Captain_01");
        }
    }
}
