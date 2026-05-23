using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.KeyboardHook
{
    [TestClass]
    public class RouteKeyEventsWhenGameWindowIsFocused : KeyboardHookHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void GameWindowFocusedDispatchFires()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenAbilityEligible("Fire Strike");
            GivenGameWindowFocus("focused");

            // When
            WhenGmPressesKey("F1");

            // Then
            ThenAbilityDispatchFires("Fire Strike");
        }

        [TestMethod]
        public void GameWindowLosesFocusDispatchSuspended()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenAbilityEligible("Fire Strike");
            GivenGameWindowFocus("unfocused");

            // When
            WhenGmPressesKey("F1");

            // Then
            ThenAbilityDispatchDoesNotFire("Fire Strike");
        }

        [TestMethod]
        public void NoMatchingActivationKeyPassThrough()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenGameWindowFocus("focused");
            GivenNoMatchingActivationKey();

            // When
            WhenGmPressesKey("F9");

            // Then
            ThenKeyPassedThrough();
            ThenNoErrorRaised();
        }
    }
}
