using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.KeybindExecution
{
    [TestClass]
    public class ExecuteDeleteNpcCommand : KeybindExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void NpcExistsDeleteRemovesFromGameWorld()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");

            // When
            WhenGameBridgeExecutesDeleteNpcCommand("Guard_Captain");

            // Then
            ThenSpawnedNpcHasPresence("Guard_Captain", "absent");
        }

        [TestMethod]
        public void NpcDoesNotExistDeleteIsNoOp()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("NonExistent_NPC", "absent");

            // When
            WhenGameBridgeExecutesDeleteNpcCommand("NonExistent_NPC");

            // Then
            ThenSpawnedNpcHasPresence("NonExistent_NPC", "absent");
        }

        [TestMethod]
        public void GhostNpcRemovalSetsGhostInactive()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain_Ghost", "present");

            // When
            WhenGameBridgeExecutesDeleteNpcCommand("Guard_Captain_Ghost");

            // Then
            ThenSpawnedNpcHasPresence("Guard_Captain_Ghost", "absent");
        }

        [TestMethod]
        public void DeleteCommandBeforeGameLoadedEventRejected()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameBridgeNotReady("polling");
            GivenSpawnedNpc("Guard_Captain", "present");

            // When
            WhenGameBridgeExecutesDeleteNpcCommand("Guard_Captain");

            // Then
            ThenCommandRejected("not-ready");
        }
    }
}
