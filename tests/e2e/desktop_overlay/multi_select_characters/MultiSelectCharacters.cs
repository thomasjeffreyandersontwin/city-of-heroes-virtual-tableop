using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.DesktopOverlay
{
    [TestClass]
    public class MultiSelectCharacters : DesktopOverlayHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void AddSecondOverlayToSelection()
        {
            GivenCharacterOverlay("Guard_Captain_01", "selected");
            WhenGmShiftClicks("Guard_B");
            ThenSelectionHighlight("Guard_Captain_01", "multi-select");
            ThenSelectionHighlight("Guard_B", "multi-select");
        }

        [TestMethod]
        public void RemoveFromMultiSelection()
        {
            GivenMultiSelect(new[] { "Guard_A", "Guard_B" });
            WhenGmShiftClicks("Guard_B");
            ThenSelectionHighlight("Guard_A", "multi-select");
        }

        [TestMethod]
        public void ReduceToOneMultiSelectEnds()
        {
            GivenMultiSelect(new[] { "Guard_A", "Guard_B" });
            WhenGmShiftClicks("Guard_A");
            ThenSelectionHighlight("Guard_B", "selected");
        }

        [TestMethod]
        public void PlainClickDuringMultiClearsAll()
        {
            GivenMultiSelect(new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenGmSingleClicks("Guard_C");
            ThenSelectionHighlight("Guard_C", "selected");
        }

        [TestMethod]
        public void ContextMenuOnMultiAppliesToAll()
        {
            GivenMultiSelect(new[] { "Guard_A", "Guard_B" });
            ThenMultiSelectContains(new[] { "Guard_A", "Guard_B" });
            ThenSelectionHighlight("Guard_A", "multi-select");
            ThenSelectionHighlight("Guard_B", "multi-select");
        }
    }
}
