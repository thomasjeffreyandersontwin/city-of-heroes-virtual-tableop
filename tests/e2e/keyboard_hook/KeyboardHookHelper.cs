using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.KeyboardHook
{
    public class KeyboardHookHelper
    {
        protected AppDriver Driver;

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenApplicationRunning()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
        }

        protected void GivenKeyboardHookInstalled()
        {
            Driver.SetKeyboardHookState("installed");
        }

        protected void GivenKeyboardHookNotInstalled()
        {
            Driver.SetKeyboardHookState("not installed");
        }

        protected void GivenActiveCharacter(string characterName)
        {
            Driver.EnsureCharacterExists(characterName);
            Driver.SetActiveCharacter(characterName);
        }

        protected void GivenNoActiveCharacter()
        {
            Driver.ClearActiveCharacter();
        }

        protected void GivenAbilityWithKey(string abilityName, string activationKey)
        {
            Driver.AddAnimatedAbilityToCharacter("Guard_Captain", abilityName);
            Driver.SetAbilityActivationKey(abilityName, activationKey);
        }

        protected void GivenAbilityEligible(string abilityName)
        {
            Driver.SetAbilityEligibility(abilityName, "eligible");
        }

        protected void GivenAbilityIneligible(string abilityName)
        {
            Driver.SetAbilityEligibility(abilityName, "ineligible");
        }

        protected void GivenGameWindowFocus(string focusState)
        {
            Driver.SetGameWindowFocusState(focusState);
        }

        protected void GivenApplicationWindowFocus(string focusState)
        {
            Driver.SetApplicationWindowFocusState(focusState);
        }

        protected void GivenAbilityExecuting(string abilityName)
        {
            Driver.SetAbilityExecutionState(abilityName, "executing");
        }

        protected void GivenNoMatchingActivationKey()
        {
            // No abilities with matching key exist
        }

        protected void GivenDuplicateKeysOnAbilities(string key, string ability1, string ability2)
        {
            Driver.SetAbilityActivationKey(ability1, key);
            Driver.SetAbilityActivationKey(ability2, key);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenApplicationStartsAndHookInstallationRequested()
        {
            Driver.InvokeKeyboardHookInstallation();
        }

        protected void WhenApplicationStartsAndHookInstallationFails()
        {
            Driver.SimulateKeyboardHookInstallationFailure();
        }

        protected void WhenApplicationShutsDown()
        {
            Driver.SimulateApplicationShutdown();
        }

        protected void WhenGmPressesKey(string key)
        {
            Driver.SimulateKeyPress(key, "Guard_Captain");
        }

        protected void WhenKeyPressReceived(string key)
        {
            Driver.SimulateKeyPressViaHook(key);
        }

        protected void WhenAbilityCompletes(string abilityName)
        {
            Driver.SimulateAllElementsComplete(abilityName);
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenKeyboardHookState(string expected)
        {
            string actual = Driver.GetKeyboardHookState();
            Assert.AreEqual(expected, actual,
                string.Format("Hook state: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenAbilityDispatchFires(string abilityName)
        {
            Assert.IsTrue(Driver.WasAbilityDispatched(abilityName),
                string.Format("Ability '{0}' should be dispatched", abilityName));
        }

        protected void ThenAbilityDispatchDoesNotFire(string abilityName)
        {
            Assert.IsFalse(Driver.WasAbilityDispatched(abilityName),
                string.Format("Ability '{0}' should NOT be dispatched", abilityName));
        }

        protected void ThenKeyPassedThrough()
        {
            Assert.IsTrue(Driver.WasKeyPassedThrough(),
                "Key should be passed through to game");
        }

        protected void ThenKeyConsumed()
        {
            Assert.IsFalse(Driver.WasKeyPassedThrough(),
                "Key should be consumed (not passed through)");
        }

        protected void ThenAbilityHasExecutionState(string abilityName, string expected)
        {
            string actual = Driver.GetAbilityExecutionState(abilityName);
            Assert.AreEqual(expected, actual,
                string.Format("Ability '{0}' state: expected '{1}' got '{2}'",
                    abilityName, expected, actual));
        }

        protected void ThenDispatchDisabledForSession()
        {
            Assert.IsTrue(Driver.IsKeyboardDispatchDisabled(),
                "Keyboard dispatch should be disabled");
        }

        protected void ThenDirectPlayActionsStillFunctional()
        {
            Assert.IsTrue(Driver.IsDirectPlayEnabled(),
                "Direct play actions should be functional");
        }

        protected void ThenEligibilityRefreshed(string abilityName, string expected)
        {
            string actual = Driver.GetAbilityEligibilityState(abilityName);
            Assert.AreEqual(expected, actual,
                string.Format("Eligibility for '{0}': expected '{1}' got '{2}'",
                    abilityName, expected, actual));
        }

        protected void ThenNoErrorRaised()
        {
            Assert.IsNull(Driver.GetLastGameBridgeError(), "No error should be raised");
        }

        protected void ThenAmbiguityWarningShown()
        {
            string msg = Driver.GetLastValidationMessage();
            Assert.IsNotNull(msg, "Expected ambiguity warning");
            Assert.IsTrue(msg.Contains("ambiguity") || msg.Contains("duplicate"),
                "Warning should mention ambiguity/duplicate");
        }
    }
}
