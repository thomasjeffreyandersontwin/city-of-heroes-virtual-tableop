using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.KeyboardHook
{
    [TestClass]
    public class RouteKeyEventsWhenApplicationWindowIsFocused : KeyboardHookHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ApplicationWindowFocusedDispatchFires()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenAbilityEligible("Fire Strike");
            GivenApplicationWindowFocus("focused");

            // When
            WhenGmPressesKey("F1");

            // Then
            ThenAbilityDispatchFires("Fire Strike");
        }

        [TestMethod]
        public void NeitherGameNorAppWindowFocusedNoDispatch()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenAbilityEligible("Fire Strike");
            GivenApplicationWindowFocus("unfocused");
            GivenGameWindowFocus("unfocused");

            // When
            WhenGmPressesKey("F1");

            // Then
            ThenAbilityDispatchDoesNotFire("Fire Strike");
        }

        [TestMethod]
        public void ApplicationWindowDispatchExecutesAbility()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenAbilityEligible("Fire Strike");
            GivenApplicationWindowFocus("focused");

            // When
            WhenGmPressesKey("F1");

            // Then
            ThenAbilityHasExecutionState("Fire Strike", "executing");
        }

        [TestMethod]
        public void ApplicationWindowLosesFocusRoutingSuspended()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenAbilityEligible("Fire Strike");
            GivenApplicationWindowFocus("unfocused");

            // When
            WhenGmPressesKey("F1");

            // Then
            ThenAbilityDispatchDoesNotFire("Fire Strike");
        }
    }
}
