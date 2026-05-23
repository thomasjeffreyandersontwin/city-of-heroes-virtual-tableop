using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.KeybindExecution
{
    [TestClass]
    public class ExecuteTargetByNameCommand : KeybindExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void NpcExistsInGameTargetResolves()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");

            // When
            WhenGameBridgeExecutesTargetByNameCommand("Guard_Captain");

            // Then
            ThenTargetByNameResolves("Guard_Captain");
        }

        [TestMethod]
        public void NpcDoesNotExistTargetFails()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Ghost_Entity", "absent");

            // When
            WhenGameBridgeExecutesTargetByNameCommand("Ghost_Entity");

            // Then
            ThenTargetByNameResolves("Ghost_Entity");
        }

        [TestMethod]
        public void NpcDespawnedBetweenCommandsTargetFails()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "absent");

            // When
            WhenGameBridgeExecutesTargetByNameCommand("Guard_Captain");

            // Then
            ThenGameBridgeReportsError("target failure");
        }

        [TestMethod]
        public void TargetChainedWithLoadCostumeInSameKeybindFile()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");

            // When
            WhenTargetChainedWithLoadCostume("Guard_Captain", "guard.costume");

            // Then
            ThenTargetByNameResolves("Guard_Captain");
        }
    }
}
