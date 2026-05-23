using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AbilityExecution
{
    [TestClass]
    public class MaintainPersistentAbilityAcrossIdentityChanges : AbilityExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void PersistentAbilityStoppedBeforeIdentitySwitch()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Aura", "executing");
            GivenAbilityPersistent("Fire Aura");

            // When
            WhenIdentityChanges();

            // Then
            ThenAbilityHasExecutionState("Fire Aura", "stopped");
        }

        [TestMethod]
        public void PersistentAbilityReplaysAfterNewIdentityLoads()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Aura", "stopped");
            GivenAbilityPersistent("Fire Aura");

            // When
            WhenNewIdentityLoads();

            // Then
            ThenAbilityHasExecutionState("Fire Aura", "executing");
        }

        [TestMethod]
        public void MultiplePersistentAbilitiesAllRestart()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Aura", "executing");
            GivenAbilityPersistent("Fire Aura");
            GivenAnimatedAbility("Ice Shield", "executing");
            GivenAbilityPersistent("Ice Shield");

            // When
            WhenIdentityChanges();
            WhenNewIdentityLoads();

            // Then
            ThenAbilityHasExecutionState("Fire Aura", "executing");
            ThenAbilityHasExecutionState("Ice Shield", "executing");
        }

        [TestMethod]
        public void CharacterDespawnedWhilePersistentAbilityActive()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Aura", "executing");
            GivenAbilityPersistent("Fire Aura");

            // When
            WhenCharacterDespawned("Guard_Captain");

            // Then
            ThenAbilityHasExecutionState("Fire Aura", "stopped");
            ThenAbilityPersistence("Fire Aura", "persistent");
        }
    }
}
