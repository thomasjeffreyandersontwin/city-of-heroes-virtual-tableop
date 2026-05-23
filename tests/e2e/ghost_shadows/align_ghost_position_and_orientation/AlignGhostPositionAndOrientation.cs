using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GhostShadows
{
    [TestClass]
    public class AlignGhostPositionAndOrientation : GhostShadowsHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void BothNpcsPresentGhostAlignedToCharacter()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenGhostNpcPresent("Guard_Captain_Ghost");

            // When
            WhenGameBridgePerformsGhostAlignment("Guard_Captain");

            // Then
            ThenGhostAligned("Guard_Captain_Ghost", "matches character position and facing");
        }

        [TestMethod]
        public void PrimaryNpcNotFoundGhostUnchanged()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "absent");
            GivenGhostNpcPresent("Guard_Captain_Ghost");

            // When
            WhenGameBridgePerformsGhostAlignment("Guard_Captain");

            // Then
            ThenGhostAligned("Guard_Captain_Ghost", "unchanged — default spawn position");
            ThenErrorReported("character not found");
        }

        [TestMethod]
        public void GhostNpcNotFoundAtAlignmentTimeReportsError()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenGhostNpcAbsent("Guard_Captain_Ghost");

            // When
            WhenGameBridgePerformsGhostAlignment("Guard_Captain");

            // Then
            ThenErrorReported("ghost NPC not found");
        }

        [TestMethod]
        public void CharacterMovesWithoutReAlignmentDriftOccurs()
        {
            // Given
            GivenGameBridgeReady();
            GivenGhostShadowActive("Guard_Captain");
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenGhostNpcPresent("Guard_Captain_Ghost");

            // Then — no re-alignment performed; drift is expected
            ThenGhostAligned("Guard_Captain_Ghost", "unchanged — default spawn position");
        }
    }
}
