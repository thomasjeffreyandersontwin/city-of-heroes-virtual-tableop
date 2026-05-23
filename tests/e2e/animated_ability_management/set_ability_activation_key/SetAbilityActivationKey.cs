using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimatedAbilityManagement
{
    [TestClass]
    public class SetAbilityActivationKey : AnimatedAbilityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidKeyAssigned()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");

            // When
            WhenGmSetsActivationKey("Fire Strike", "F1");

            // Then
            ThenAbilityHasKey("Fire Strike", "F1");
        }

        [TestMethod]
        public void KeyCleared()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");
            GivenAbilityWithKey("Fire Strike", "F1");

            // When
            WhenGmSetsActivationKey("Fire Strike", "(unset)");

            // Then
            ThenAbilityHasKey("Fire Strike", "(unset)");
        }

        [TestMethod]
        public void DuplicateKeyOnSameCharacterRejected()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");
            GivenAnimatedAbility("Guard_Captain", "Ice Shield");
            GivenAbilityWithKey("Ice Shield", "F1");

            // When
            WhenGmSetsActivationKey("Fire Strike", "F1");

            // Then
            ThenValidationErrorShown("duplicate");
        }

        [TestMethod]
        public void KeySetAndKeyboardHookActiveDispatchesAbility()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenKeyboardHookInstalled();
            GivenCharacterSpawned("Guard_Captain");

            // When
            WhenGmPressesKey("F1", "Guard_Captain");

            // Then
            ThenAbilityDispatched("Fire Strike");
        }
    }
}
