using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ContextMenu
{
    [TestClass]
    public class CloneAndLinkCharacterFromDesktop : ContextMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void CloneSucceeds()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsCloneLink("Guard_Captain_01");
            ThenRosterEntryCreated("Guard_Captain_01_copy");
            ThenSpawnedState("Guard_Captain_01_copy", "false");
        }

        [TestMethod]
        public void NameDuplicatesInCrowdCopySuffixAppended()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenRosterEntryExists("Guard_Captain_01_copy");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsCloneLink("Guard_Captain_01");
            ThenRosterEntryCreated("Guard_Captain_01 (Copy)");
        }

        [TestMethod]
        public void LibrarySaveFailsNotCreated()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenLibrarySaveWillFail();
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsCloneLink("Guard_Captain_01");
            ThenRosterEntryNotCreated("Guard_Captain_01_copy");
            ThenFeedbackShown();
        }
    }
}
