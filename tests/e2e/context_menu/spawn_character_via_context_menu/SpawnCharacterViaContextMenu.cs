using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ContextMenu
{
    [TestClass]
    public class SpawnCharacterViaContextMenu : ContextMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void NotSpawnedSpawnActionAvailable()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            GivenTargetCharacter("Guard_Captain_01");
            ThenActionAvailable("Spawn");
        }

        [TestMethod]
        public void AlreadySpawnedSpawnActionHidden()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenTargetCharacter("Guard_Captain_01");
            ThenActionNotAvailable("Spawn");
        }

        [TestMethod]
        public void SpawnSucceedsPresenceBecomesTrue()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsSpawn("Guard_Captain_01");
            ThenSpawnedState("Guard_Captain_01", "true");
        }

        [TestMethod]
        public void SpawnCommandFailsPresenceRemainsFalse()
        {
            GivenSpawnedState("Villain_Boss_03", "false");
            GivenSpawnWillFail();
            GivenTargetCharacter("Villain_Boss_03");
            WhenGmSelectsSpawn("Villain_Boss_03");
            ThenSpawnedState("Villain_Boss_03", "false");
        }
    }
}
