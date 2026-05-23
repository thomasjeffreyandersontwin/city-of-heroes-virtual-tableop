using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.DesktopOverlay
{
    [TestClass]
    public class SelectCharacterOnDesktopViaMouseClick : DesktopOverlayHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void ClickOnUnselectedOverlaySelects()
        {
            GivenCharacterOverlay("Guard_Captain_01", "none");
            WhenGmSingleClicks("Guard_Captain_01");
            ThenSelectionHighlight("Guard_Captain_01", "selected");
        }

        [TestMethod]
        public void ClickEmptySpaceClearsAll()
        {
            GivenCharacterOverlay("Guard_Captain_01", "selected");
            WhenGmSingleClicks("empty_space");
            ThenSelectionHighlight("Guard_Captain_01", "none");
        }

        [TestMethod]
        public void ClickAlreadySelectedOverlayRemains()
        {
            GivenCharacterOverlay("Guard_Captain_01", "selected");
            WhenGmSingleClicks("Guard_Captain_01");
            ThenSelectionHighlight("Guard_Captain_01", "selected");
        }

        [TestMethod]
        public void ClickDuringMultiSelectClearsToSingle()
        {
            GivenMultiSelect(new[] { "Guard_A", "Guard_B" });
            WhenGmSingleClicks("Guard_A");
            ThenSelectionHighlight("Guard_A", "selected");
        }
    }
}
