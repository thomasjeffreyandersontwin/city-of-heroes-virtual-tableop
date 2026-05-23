using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GhostShadows
{
    [TestClass]
    public class RemoveGhostFromDesktop : GhostShadowsHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void NormalRemovalGhostNpcDespawned()
        {
            // Given
            GivenGameBridgeReady();
            GivenGhostShadowActive("Guard_Captain");
            GivenGhostNpcPresent("Guard_Captain_Ghost");

            // When
            WhenGmChoosesRemoveGhost("Guard_Captain");

            // Then
            ThenGhostNpcPresence("Guard_Captain_Ghost", "absent");
            ThenGhostShadowState("Guard_Captain", "inactive");
        }

        [TestMethod]
        public void GhostNpcAlreadyGoneStateStillCleared()
        {
            // Given
            GivenGameBridgeReady();
            GivenGhostShadowActive("Guard_Captain");
            GivenGhostNpcAbsent("Guard_Captain_Ghost");

            // When
            WhenGmChoosesRemoveGhost("Guard_Captain");

            // Then
            ThenGhostNpcPresence("Guard_Captain_Ghost", "absent");
            ThenGhostShadowState("Guard_Captain", "inactive");
        }

        [TestMethod]
        public void ClearCharacterFromDesktopRemovesGhostToo()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenGhostShadowActive("Guard_Captain");
            GivenGhostNpcPresent("Guard_Captain_Ghost");

            // When
            WhenCharacterClearedFromDesktop("Guard_Captain");

            // Then
            ThenGhostNpcPresence("Guard_Captain_Ghost", "absent");
            ThenGhostNpcPresence("Guard_Captain", "absent");
        }

        [TestMethod]
        public void BridgeNotReadyRemovalDeferred()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameBridgeNotReady();
            GivenGhostShadowActive("Guard_Captain");
            GivenGhostNpcPresent("Guard_Captain_Ghost");

            // When
            WhenGmChoosesRemoveGhost("Guard_Captain");

            // Then
            ThenGhostShadowState("Guard_Captain", "active");
            ThenGhostIndicatorShown("Guard_Captain");
        }

        [TestMethod]
        public void AddGhostReEnabledAfterRemoval()
        {
            // Given
            GivenGameBridgeReady();
            GivenGhostShadowInactive("Guard_Captain");

            // Then
            ThenAddGhostEnabled();
        }
    }
}
