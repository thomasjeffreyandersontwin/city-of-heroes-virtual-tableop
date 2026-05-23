using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.DesktopOverlay
{
    [TestClass]
    public class SelectCharacterOnDesktopViaMouseClick : DesktopOverlayDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Desktop Overlay has Character Overlays rendered
        }

        [TestMethod]
        public void ClickOnUnselectedOverlaySelects()
        {
            // Given: Character Overlay Guard_Captain_01 selection highlight none
            then_not_selected(_guardCaptain);
            // When: the GM single-clicks in the Desktop Overlay on Guard_Captain_01
            when_single_click(_guardCaptain);
            // Then: Character Overlay Guard_Captain_01 has selection highlight selected
            then_selected(_guardCaptain);
        }

        [TestMethod]
        public void ClickEmptySpaceClearsAll()
        {
            // Given: Guard_Captain_01 has selection highlight selected
            given_selected(_guardCaptain);
            // When: the GM single-clicks empty space in the Desktop Overlay
            when_click_empty_space();
            // Then: all selections cleared; no Roster Entry remains highlighted
            then_selection_empty();
        }

        [TestMethod]
        public void ClickAlreadySelectedOverlayRemains()
        {
            // Given: Guard_Captain_01 already selected
            given_selected(_guardCaptain);
            // When: the GM single-clicks Guard_Captain_01 again
            when_single_click(_guardCaptain);
            // Then: selection remains; Guard_Captain_01 still selected
            then_selected(_guardCaptain);
        }

        [TestMethod]
        public void ClickDuringMultiSelectClearsToSingle()
        {
            // Given: multi-select active with Guard_Captain_01 and Villain_Boss_03 selected
            given_selected(_guardCaptain);
            given_selected(_villainBoss);
            // When: the GM single-clicks (without modifier) Guard_Captain_01 during multi-select
            when_single_click(_guardCaptain);
            // Then: all multi-selections cleared; only Guard_Captain_01 is selected (single-select)
            then_selected(_guardCaptain);
            then_selection_count(1);
        }
    }
}
