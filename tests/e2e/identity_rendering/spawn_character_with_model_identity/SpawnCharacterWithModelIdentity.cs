using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityRendering
{
    [TestClass]
    public class SpawnCharacterWithModelIdentity : IdentityRenderingHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidModelBridgeReadyNpcSpawned()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01", "Clockwork_Gear_01");
            GivenCharacterWithName("Guard_Captain");
            GivenModelIdentity("Dragon_Model", "Skull_Lt_01");

            // When
            WhenGmActivatesModelIdentity("Guard_Captain", "Dragon_Model");

            // Then
            ThenSpawnedNpcPresent("Guard_Captain");
        }

        [TestMethod]
        public void ModelNotInLoadedListActivationBlocked()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01", "Clockwork_Gear_01");
            GivenCharacterWithName("Shadow_Knight");
            GivenModelIdentity("Unknown_Model", "Invalid_Model_99");

            // When
            WhenGmActivatesModelIdentity("Shadow_Knight", "Unknown_Model");

            // Then
            ThenSpawnedNpcAbsent("Shadow_Knight");
            ThenActivationBlocked("model not found");
        }

        [TestMethod]
        public void NpcNameAlreadyExistsDeleteThenRespawn()
        {
            // Given
            GivenGameBridgeReady();
            GivenModelListLoaded("Skull_Lt_01", "Clockwork_Gear_01");
            GivenCharacterWithName("Guard_Captain");
            GivenModelIdentity("Dragon_Model", "Skull_Lt_01");
            GivenSpawnedNpc("Guard_Captain", "present");

            // When
            WhenGmActivatesModelIdentity("Guard_Captain", "Dragon_Model");

            // Then
            ThenSpawnedNpcPresent("Guard_Captain");
        }

        [TestMethod]
        public void BridgeNotReadyActivationBlocked()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameBridgeNotReady();
            GivenModelListLoaded("Clockwork_Gear_01");
            GivenModelIdentity("Dragon_Model", "Clockwork_Gear_01");

            // When
            WhenGmActivatesModelIdentity("Guard_Captain", "Dragon_Model");

            // Then
            ThenActivationBlocked("game not connected");
        }
    }
}
