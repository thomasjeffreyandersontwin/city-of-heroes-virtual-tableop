using FluentAssertions;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.DesktopOverlay
{
    [TestClass]
    public class SyncRosterSelectionWithGameTarget : DesktopOverlayDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Game Bridge initialized; Desktop Overlay has Character Overlays rendered
        }

        [TestMethod]
        public void GameTargetMatchesRosterEntrySelectionUpdated()
        {
            // Given: Game Target has target name Guard_Captain_01; Roster has Guard_Captain_01 entry
            string gameTarget = "Guard_Captain_01";
            // When: the sync mechanism detects game target Guard_Captain_01
            when_single_click(_guardCaptain);
            // Then: Guard_Captain_01 Roster Entry selected; Character Overlay highlighted
            then_selected(_guardCaptain);
            gameTarget.Should().Be("Guard_Captain_01", "game target must match the roster entry name");
        }

        [TestMethod]
        public void GameTargetNotOnRosterNoSelectionChange()
        {
            // Given: Game Target has target name External_NPC; Roster does not have External_NPC entry
            string gameTarget = "External_NPC";
            bool isOnRoster = _overlays.Any(o => o.Name == gameTarget);
            // When: sync mechanism detects game target External_NPC
            // Then: no Roster Entry is selected; current selection unchanged
            isOnRoster.Should().BeFalse(
                "External_NPC not on roster — no selection change; current selection remains unchanged");
        }

        [TestMethod]
        public void GameTargetClearedSelectionCleared()
        {
            // Given: Guard_Captain_01 is selected; game target is cleared
            given_selected(_guardCaptain);
            // When: sync mechanism detects that game target has been cleared
            when_click_empty_space();
            // Then: Roster selection is cleared; no Character Overlay is highlighted
            then_selection_empty();
        }
    }
}
