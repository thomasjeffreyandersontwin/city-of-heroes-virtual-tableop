using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimatedAbilityManagement
{
    public class AnimatedAbilityManagementHelper
    {
        protected AppDriver Driver;

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenCharacterSelected(string characterName)
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.EnsureCharacterExists(characterName);
            Driver.SelectCharacterInCrowdTree(characterName);
        }

        protected void GivenNoCharacterSelected()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.ClearCharacterSelection();
        }

        protected void GivenAnimatedAbility(string characterName, string abilityName)
        {
            Driver.AddAnimatedAbilityToCharacter(characterName, abilityName);
        }

        protected void GivenAbilityWithKey(string abilityName, string activationKey)
        {
            Driver.SetAbilityActivationKey(abilityName, activationKey);
        }

        protected void GivenAbilityPersistence(string abilityName, string persistence)
        {
            Driver.SetAbilityPersistence(abilityName, persistence);
        }

        protected void GivenAbilityDefault(string abilityName, string defaultDesignation)
        {
            Driver.SetAbilityDefaultDesignation(abilityName, defaultDesignation);
        }

        protected void GivenAbilityExecutionState(string abilityName, string executionState)
        {
            Driver.SetAbilityExecutionState(abilityName, executionState);
        }

        protected void GivenKeyboardHookInstalled()
        {
            Driver.SetKeyboardHookState("installed");
        }

        protected void GivenCharacterSpawned(string characterName)
        {
            Driver.SetSpawnedNpcState(characterName, "present");
        }

        protected void GivenReferenceElementPointsTo(string ownerAbility, string referencedAbility)
        {
            Driver.AddReferenceElementToAbility(ownerAbility, referencedAbility);
        }

        protected void GivenIdentityChanges()
        {
            Driver.SimulateIdentityChange();
            Driver.SimulateNewIdentityLoaded();
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenGmCreatesAbility(string characterName, string abilityName)
        {
            Driver.InvokeCreateAbility(characterName, abilityName);
        }

        protected void WhenGmEditsAbility(string abilityName)
        {
            Driver.InvokeEditAbility(abilityName);
        }

        protected void WhenGmSavesAbilityEditor()
        {
            Driver.InvokeSaveAbilityEditor();
        }

        protected void WhenGmCancelsAbilityEditor()
        {
            Driver.InvokeCancelAbilityEditor();
        }

        protected void WhenGmDeletesAbility(string abilityName)
        {
            Driver.InvokeDeleteAbility(abilityName);
        }

        protected void WhenGmSetsActivationKey(string abilityName, string key)
        {
            Driver.InvokeSetActivationKey(abilityName, key);
        }

        protected void WhenGmTogglesPersistence(string abilityName)
        {
            Driver.InvokeTogglePersistence(abilityName);
        }

        protected void WhenGmSetsDefault(string abilityName)
        {
            Driver.InvokeSetDefaultAbility(abilityName);
        }

        protected void WhenGmClearsDefault(string abilityName)
        {
            Driver.InvokeClearDefaultAbility(abilityName);
        }

        protected void WhenGmPressesKey(string key, string characterName)
        {
            Driver.SimulateKeyPress(key, characterName);
        }

        protected void WhenCharacterSpawned(string characterName)
        {
            Driver.SimulateCharacterSpawn(characterName);
        }

        protected void WhenAbilityRemoved(string abilityName, string characterName)
        {
            Driver.InvokeDeleteAbility(abilityName);
        }

        protected void WhenGmClearsPersistence(string abilityName)
        {
            Driver.InvokeClearPersistence(abilityName);
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenAbilityExistsWithState(string abilityName, string activationKey,
            string persistence, string defaultDesignation)
        {
            Assert.IsTrue(Driver.AbilityExistsOnCharacter(abilityName),
                string.Format("Ability '{0}' not found", abilityName));
            Assert.AreEqual(activationKey, Driver.GetAbilityActivationKey(abilityName),
                string.Format("Ability '{0}' key mismatch", abilityName));
            Assert.AreEqual(persistence, Driver.GetAbilityPersistence(abilityName),
                string.Format("Ability '{0}' persistence mismatch", abilityName));
        }

        protected void ThenAbilityNotInList(string abilityName)
        {
            Assert.IsFalse(Driver.AbilityExistsOnCharacter(abilityName),
                string.Format("Ability '{0}' should not exist", abilityName));
        }

        protected void ThenAbilityHasKey(string abilityName, string expectedKey)
        {
            string actual = Driver.GetAbilityActivationKey(abilityName);
            Assert.AreEqual(expectedKey, actual,
                string.Format("Ability '{0}' key: expected '{1}' got '{2}'", abilityName, expectedKey, actual));
        }

        protected void ThenAbilityHasPersistence(string abilityName, string expected)
        {
            string actual = Driver.GetAbilityPersistence(abilityName);
            Assert.AreEqual(expected, actual,
                string.Format("Ability '{0}' persistence: expected '{1}' got '{2}'", abilityName, expected, actual));
        }

        protected void ThenAbilityHasDefault(string abilityName, string expected)
        {
            string actual = Driver.GetAbilityDefaultDesignation(abilityName);
            Assert.AreEqual(expected, actual,
                string.Format("Ability '{0}' default: expected '{1}' got '{2}'", abilityName, expected, actual));
        }

        protected void ThenAbilityHasExecutionState(string abilityName, string expected)
        {
            string actual = Driver.GetAbilityExecutionState(abilityName);
            Assert.AreEqual(expected, actual,
                string.Format("Ability '{0}' execution: expected '{1}' got '{2}'", abilityName, expected, actual));
        }

        protected void ThenCreateActionDisabled()
        {
            Assert.IsFalse(Driver.IsCreateAbilityEnabled(), "Create action should be disabled");
        }

        protected void ThenValidationErrorShown(string expectedFragment)
        {
            string msg = Driver.GetLastValidationMessage();
            Assert.IsNotNull(msg, "Expected validation error");
            Assert.IsTrue(msg.Contains(expectedFragment),
                string.Format("Validation message missing '{0}'", expectedFragment));
        }

        protected void ThenAbilityEditorOpen()
        {
            Assert.IsTrue(Driver.IsAbilityEditorOpen(), "Ability editor should be open");
        }

        protected void ThenAbilityEditorClosed()
        {
            Assert.IsFalse(Driver.IsAbilityEditorOpen(), "Ability editor should be closed");
        }

        protected void ThenNoDefaultAbilityOnCharacter()
        {
            Assert.IsNull(Driver.GetDefaultAbilityName(),
                "No ability should carry default designation");
        }

        protected void ThenAbilityDispatched(string abilityName)
        {
            Assert.IsTrue(Driver.WasAbilityDispatched(abilityName),
                string.Format("Ability '{0}' should have been dispatched", abilityName));
        }

        protected void ThenAbilityCount(int expectedCount)
        {
            int actual = Driver.GetAbilityCount();
            Assert.AreEqual(expectedCount, actual,
                string.Format("Expected {0} abilities, got {1}", expectedCount, actual));
        }
    }
}
