using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.HcsIntegration
{
    public class HcsIntegrationHelper
    {
        protected AppDriver Driver;

        protected void GivenApplicationRunning()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
        }

        protected void GivenGameBridgeReady()
        {
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenGameBridgeNotInitialized()
        {
            Driver.SetGameBridgeState("not_initialized");
        }

        protected void GivenHcsIntegrationActive()
        {
            Driver.SetHcsIntegrationState("active");
        }

        protected void GivenHcsFileWatcherActive()
        {
            Driver.SetHcsFileWatcherState("monitoring");
        }

        protected void GivenOutputDirectoryExists(bool exists)
        {
            Driver.SetHcsOutputDirectoryExists(exists);
        }

        protected void GivenNonAttackLockActive(string characterName)
        {
            Driver.SetNonAttackAbilityLock(characterName, true);
        }

        protected void WhenGmTriggersStartHcsIntegration()
        {
            Driver.InvokeStartHcsIntegration();
        }

        protected void WhenGmTriggersStopHcsIntegration()
        {
            Driver.InvokeStopHcsIntegration();
        }

        protected void WhenInfoFileArrives(string eventType, string payload)
        {
            Driver.SimulateHcsInfoFileArrival(eventType, payload);
        }

        protected void ThenHcsIntegrationState(string expected)
        {
            string actual = Driver.GetHcsIntegrationState();
            Assert.AreEqual(expected, actual,
                string.Format("HCS integration state: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenFileWatcherState(string expected)
        {
            string actual = Driver.GetHcsFileWatcherState();
            Assert.AreEqual(expected, actual,
                string.Format("File watcher state: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenOnDeckCombatants(string[] expected)
        {
            foreach (string name in expected)
                Assert.IsTrue(Driver.IsCharacterOnDeck(name),
                    string.Format("'{0}' should be on-deck", name));
        }

        protected void ThenNoOnDeckHighlights()
        {
            Assert.IsTrue(Driver.AreOnDeckHighlightsCleared(), "No on-deck highlights expected");
        }

        protected void ThenEligibleCombatants(string[] expected)
        {
            foreach (string name in expected)
                Assert.IsTrue(Driver.IsCharacterEligible(name),
                    string.Format("'{0}' should be eligible", name));
        }

        protected void ThenActiveCharacterHcs(string expected)
        {
            string actual = Driver.GetActiveCharacterDesignation();
            Assert.AreEqual(expected, actual,
                string.Format("Active character HCS: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenActiveCharacterUnchanged()
        {
            Assert.IsTrue(Driver.WasActiveCharacterUnchanged(), "Active character should be unchanged");
        }

        protected void ThenChronometerPhase(string characterName, string expected)
        {
            string actual = Driver.GetChronometerPhase(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Phase for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenAttackResultDispatched(string attacker, string defender, string result)
        {
            Assert.IsTrue(Driver.WasAttackResultDispatched(attacker, defender, result),
                string.Format("Attack result {0}->{1}:{2} should be dispatched", attacker, defender, result));
        }

        protected void ThenSimpleAbilityPlayed(string characterName, string ability)
        {
            Assert.IsTrue(Driver.WasSimpleAbilityPlayed(characterName, ability),
                string.Format("Ability '{0}' on '{1}' should play", ability, characterName));
        }

        protected void ThenSimpleAbilityBlocked(string characterName)
        {
            Assert.IsTrue(Driver.WasSimpleAbilityBlocked(characterName),
                string.Format("Ability on '{0}' should be blocked", characterName));
        }

        protected void ThenHeldState(string characterName, string expected)
        {
            string actual = Driver.GetHeldCharacterState(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Held state for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenSweepResultsDispatched(string expected)
        {
            string actual = Driver.GetSweepResultsDispatched();
            Assert.AreEqual(expected, actual,
                string.Format("Sweep results: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenWarningLogged()
        {
            Assert.IsNotNull(Driver.GetLastWarning(), "Expected warning logged");
        }

        protected void ThenFeedbackShown()
        {
            Assert.IsNotNull(Driver.GetLastValidationMessage(), "Expected feedback");
        }
    }
}
