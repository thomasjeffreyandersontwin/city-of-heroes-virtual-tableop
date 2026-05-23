using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ContextMenu
{
    [TestClass]
    public class ActivateCharacterOptionViaContextMenu : ContextMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void ActivateViaContextMenu()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenGangMode("inactive");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsActivateOption("Guard_Captain_01");
            ThenActiveCharacter("Guard_Captain_01");
        }

        [TestMethod]
        public void AlreadyActiveNoOp()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenGangMode("inactive");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsActivateOption("Guard_Captain_01");
            ThenActiveCharacter("Guard_Captain_01");
        }

        [TestMethod]
        public void GangActiveReplacesWithSingle()
        {
            GivenSpawnedState("Villain_Boss_03", "true");
            GivenGangMode("active");
            GivenTargetCharacter("Villain_Boss_03");
            WhenGmSelectsActivateOption("Villain_Boss_03");
            ThenActiveCharacter("Villain_Boss_03");
        }
    }
}
