using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.DesktopOverlay
{
    [TestClass]
    public class SyncRosterSelectionWithGameTarget : DesktopOverlayHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
            GivenMemoryInterfaceMonitoring();
        }

        [TestMethod]
        public void TargetMatchesRosterEntryHighlighted()
        {
            WhenGameTargetChanges("Guard_Captain_01");
            ThenSelectionHighlight("Guard_Captain_01", "selected");
        }

        [TestMethod]
        public void TargetNotInRosterNoHighlight()
        {
            WhenGameTargetChanges("Unknown_NPC");
            ThenNoHighlights();
        }

        [TestMethod]
        public void TargetChangesToAnotherRosterChar()
        {
            WhenGameTargetChanges("Guard_Captain_01");
            WhenGameTargetChanges("Villain_Boss_03");
            ThenSelectionHighlight("Villain_Boss_03", "selected");
        }

        [TestMethod]
        public void TargetClearedAllHighlightsCleared()
        {
            WhenGameTargetChanges("Guard_Captain_01");
            WhenGameTargetChanges("");
            ThenNoHighlights();
        }
    }
}
