using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.DesktopOverlay
{
    [TestClass]
    public class TrackSpawnedStatePerCharacter : DesktopOverlayHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void SpawnedFromRosterOrContextMenuPresenceTrue()
        {
            WhenLifecycleEvent("spawn", "Guard_Captain_01");
            ThenSpawnedState("Guard_Captain_01", "true");
        }

        [TestMethod]
        public void ClearedOrRemovedFromDesktopPresenceFalse()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            WhenLifecycleEvent("clear", "Guard_Captain_01");
            ThenSpawnedState("Guard_Captain_01", "false");
        }

        [TestMethod]
        public void GameDoneStateBecomesTrueAllFalse()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenSpawnedState("Guard_A", "true");
            WhenLifecycleEvent("game_done", "all");
            ThenSpawnedState("Guard_Captain_01", "false");
            ThenSpawnedState("Guard_A", "false");
        }

        [TestMethod]
        public void NotSpawnedOverlayNotRendered()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            ThenSpawnedState("Guard_Captain_01", "false");
        }

        [TestMethod]
        public void MultipleSpawnedSimultaneouslyIndependent()
        {
            WhenLifecycleEvent("spawn", "Guard_Captain_01");
            WhenLifecycleEvent("spawn", "Guard_A");
            ThenSpawnedState("Guard_Captain_01", "true");
            ThenSpawnedState("Guard_A", "true");
        }
    }
}
