using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.DesktopOverlay
{
    [TestClass]
    public class DoubleClickCharacterToActivate : DesktopOverlayHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void DoubleClickActivatesCharacter()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenGangMode("inactive");
            WhenGmDoubleClicks("Guard_Captain_01");
            ThenActiveCharacter("Guard_Captain_01");
        }

        [TestMethod]
        public void AlreadyActiveNoOp()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenGangMode("inactive");
            WhenGmDoubleClicks("Guard_Captain_01");
            ThenActiveCharacter("Guard_Captain_01");
        }

        [TestMethod]
        public void GangActiveReplacesWithSingle()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenGangMode("active");
            WhenGmDoubleClicks("Guard_Captain_01");
            ThenActiveCharacter("Guard_Captain_01");
        }

        [TestMethod]
        public void NotSpawnedNoEffect()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            GivenGangMode("inactive");
            WhenGmDoubleClicks("Guard_Captain_01");
            ThenActiveCharacter("unchanged");
        }
    }
}
