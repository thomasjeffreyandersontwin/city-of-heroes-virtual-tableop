using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ContextMenu
{
    [TestClass]
    public class ResetCharacterOrientationViaContextMenu : ContextMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void SpawnedResetSucceeds()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsResetOrientation("Guard_Captain_01");
            ThenFeedbackShown();
        }

        [TestMethod]
        public void NotSpawnedActionUnavailable()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            GivenTargetCharacter("Guard_Captain_01");
            ThenActionNotAvailable("ResetOrientation");
        }

        [TestMethod]
        public void WriteFailsFacingUnchanged()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsResetOrientation("Guard_Captain_01");
            ThenFeedbackShown();
        }
    }
}
