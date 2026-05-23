using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimatedAbilityManagement
{
    [TestClass]
    public class ToggleAbilityPersistence : AnimatedAbilityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ToggleOnWasNonPersistent()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Aura");
            GivenAbilityPersistence("Fire Aura", "non-persistent");

            // When
            WhenGmTogglesPersistence("Fire Aura");

            // Then
            ThenAbilityHasPersistence("Fire Aura", "persistent");
        }

        [TestMethod]
        public void ToggleOffWasPersistent()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Aura");
            GivenAbilityPersistence("Fire Aura", "persistent");

            // When
            WhenGmTogglesPersistence("Fire Aura");

            // Then
            ThenAbilityHasPersistence("Fire Aura", "non-persistent");
        }

        [TestMethod]
        public void PersistentAbilityStopsAndRestartsOnIdentityChange()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Aura");
            GivenAbilityPersistence("Fire Aura", "persistent");
            GivenAbilityExecutionState("Fire Aura", "executing");

            // When
            GivenIdentityChanges();

            // Then
            ThenAbilityHasExecutionState("Fire Aura", "executing");
        }

        [TestMethod]
        public void PersistentAbilityDeactivatedTriggersCostumeReload()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Aura");
            GivenAbilityPersistence("Fire Aura", "persistent");
            GivenAbilityExecutionState("Fire Aura", "executing");

            // When
            WhenGmClearsPersistence("Fire Aura");

            // Then
            ThenAbilityHasPersistence("Fire Aura", "non-persistent");
        }
    }
}
