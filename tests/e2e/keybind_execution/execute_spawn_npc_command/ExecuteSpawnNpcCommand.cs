using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.KeybindExecution
{
    [TestClass]
    public class ExecuteSpawnNpcCommand : KeybindExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidModelBridgeReadyNpcPresent()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded();
            GivenCharacterWithName("Guard_Captain");
            GivenModelIdentityWithModelName("Skull_Lt_01");

            // When
            WhenGameBridgeExecutesSpawnNpcCommand("Guard_Captain", "Skull_Lt_01");

            // Then
            ThenSpawnedNpcHasPresence("Guard_Captain", "present");
        }

        [TestMethod]
        public void ModelNotInListSpawnFails()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded();
            GivenCharacterWithName("Shadow_Knight");
            GivenModelIdentityWithModelName("Invalid_Model_99");

            // When
            WhenGameBridgeExecutesSpawnNpcCommand("Shadow_Knight", "Invalid_Model_99");

            // Then
            ThenSpawnedNpcHasPresence("Shadow_Knight", "absent");
        }

        [TestMethod]
        public void DuplicateNpcNameExistsSpawnProceeds()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded();
            GivenCharacterWithName("Guard_Captain");
            GivenModelIdentityWithModelName("Skull_Lt_01");
            GivenSpawnedNpc("Guard_Captain", "present");

            // When
            WhenGameBridgeExecutesSpawnNpcCommand("Guard_Captain", "Skull_Lt_01");

            // Then
            ThenSpawnedNpcHasPresence("Guard_Captain", "present");
        }

        [TestMethod]
        public void BridgeNotReadyCommandRejected()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameBridgeNotReady("polling");
            GivenCharacterWithName("Frost_Archer");
            GivenModelIdentityWithModelName("Clockwork_Gear_01");

            // When
            WhenGameBridgeExecutesSpawnNpcCommand("Frost_Archer", "Clockwork_Gear_01");

            // Then
            ThenSpawnedNpcHasPresence("Frost_Archer", "absent");
        }
    }
}
