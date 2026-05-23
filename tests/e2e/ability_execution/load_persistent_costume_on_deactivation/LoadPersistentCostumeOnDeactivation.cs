using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AbilityExecution
{
    [TestClass]
    public class LoadPersistentCostumeOnDeactivation : AbilityExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void DeactivationTriggersCostumeReload()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Aura", "executing");
            GivenAbilityPersistent("Fire Aura");
            GivenPersistentFxCostumeVariantExists();

            // When
            WhenGmClearsPersistence("Fire Aura");

            // Then
            ThenCostumeVariantLoaded();
        }

        [TestMethod]
        public void DeactivationWhenCharacterNotSpawned()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenNoSpawnedNpc("Guard_Captain");
            GivenAnimatedAbility("Fire Aura", "stopped");
            GivenAbilityPersistent("Fire Aura");

            // When
            WhenGmClearsPersistence("Fire Aura");

            // Then
            ThenNoCostumeLoadCommand();
            ThenAbilityPersistence("Fire Aura", "non-persistent");
        }

        [TestMethod]
        public void OneOfMultiplePersistentAbilitiesDeactivated()
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
            WhenGmClearsPersistence("Fire Aura");

            // Then
            ThenAbilityPersistence("Fire Aura", "non-persistent");
            ThenAbilityPersistence("Ice Shield", "persistent");
        }

        [TestMethod]
        public void PersistentFxCostumeVariantFileMissing()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Aura", "executing");
            GivenAbilityPersistent("Fire Aura");
            GivenPersistentFxCostumeVariantMissing();

            // When
            WhenGmClearsPersistence("Fire Aura");

            // Then
            ThenNoCostumeLoadCommand();
        }
    }
}
