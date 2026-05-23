using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.KeyboardHook
{
    [TestClass]
    public class DispatchAbilityActivationKeysToCharacters : KeyboardHookHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void DispatchFiresOnKeyMatchWithEligibleAbility()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenAbilityEligible("Fire Strike");

            // When
            WhenKeyPressReceived("F1");

            // Then
            ThenAbilityDispatchFires("Fire Strike");
        }

        [TestMethod]
        public void DispatchSuppressedWhenEligibilityIneligible()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenAbilityIneligible("Fire Strike");

            // When
            WhenKeyPressReceived("F1");

            // Then
            ThenAbilityDispatchDoesNotFire("Fire Strike");
            ThenKeyConsumed();
        }

        [TestMethod]
        public void NoActiveCharacterPassThrough()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenNoActiveCharacter();

            // When
            WhenKeyPressReceived("F1");

            // Then
            ThenKeyPassedThrough();
            ThenNoErrorRaised();
        }

        [TestMethod]
        public void DuplicateActivationKeyFirstEligibleMatchDispatched()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenAbilityWithKey("Ice Shield", "F1");
            GivenAbilityEligible("Fire Strike");
            GivenAbilityEligible("Ice Shield");
            GivenDuplicateKeysOnAbilities("F1", "Fire Strike", "Ice Shield");

            // When
            WhenKeyPressReceived("F1");

            // Then
            ThenAbilityDispatchFires("Fire Strike");
            ThenAmbiguityWarningShown();
        }

        [TestMethod]
        public void DispatchCompletesEligibilityRefreshed()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenAbilityEligible("Fire Strike");
            GivenAbilityExecuting("Fire Strike");

            // When
            WhenAbilityCompletes("Fire Strike");

            // Then
            ThenAbilityHasExecutionState("Fire Strike", "stopped");
            ThenEligibilityRefreshed("Fire Strike", "eligible");
        }
    }
}
