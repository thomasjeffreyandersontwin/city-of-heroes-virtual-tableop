using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ContextMenu
{
    [TestClass]
    public class SaveCharacterPosition : ContextMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void SpawnedSaveSucceeds()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenCharacterMemoryPosition("Guard_Captain_01", "(125.5, 0.0, -340.2)");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsSavePosition("Guard_Captain_01");
            ThenSavedPosition("Guard_Captain_01", "(125.5, 0.0, -340.2)");
        }

        [TestMethod]
        public void PositionAlreadySavedOverwrite()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenCharacterMemoryPosition("Guard_Captain_01", "(200.0, 5.0, -100.0)");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsSavePosition("Guard_Captain_01");
            ThenSavedPosition("Guard_Captain_01", "(200.0, 5.0, -100.0)");
        }

        [TestMethod]
        public void MemoryReadFailsSaveFails()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenTargetCharacter("Guard_Captain_01");
            WhenGmSelectsSavePosition("Guard_Captain_01");
            ThenSavedPosition("Guard_Captain_01", "unchanged");
        }

        [TestMethod]
        public void NotSpawnedActionUnavailable()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            GivenTargetCharacter("Guard_Captain_01");
            ThenActionNotAvailable("SavePosition");
        }
    }
}
