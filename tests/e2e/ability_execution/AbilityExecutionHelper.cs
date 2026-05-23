using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AbilityExecution
{
    public class AbilityExecutionHelper
    {
        protected AppDriver Driver;

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenGameBridgeReady()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenCharacterWithName(string characterName)
        {
            Driver.EnsureCharacterExists(characterName);
        }

        protected void GivenSpawnedNpcPresent(string characterName)
        {
            Driver.SetSpawnedNpcState(characterName, "present");
        }

        protected void GivenNoSpawnedNpc(string characterName)
        {
            Driver.SetSpawnedNpcState(characterName, "absent");
        }

        protected void GivenAnimatedAbility(string abilityName, string executionState)
        {
            Driver.AddAnimatedAbilityToCharacter("Guard_Captain", abilityName);
            Driver.SetAbilityExecutionState(abilityName, executionState);
        }

        protected void GivenAbilityPersistent(string abilityName)
        {
            Driver.SetAbilityPersistence(abilityName, "persistent");
        }

        protected void GivenAbilityNonPersistent(string abilityName)
        {
            Driver.SetAbilityPersistence(abilityName, "non-persistent");
        }

        protected void GivenAbilityDefault(string abilityName)
        {
            Driver.SetAbilityDefaultDesignation(abilityName, "default");
        }

        protected void GivenSequenceElement(string executionType, int childCount)
        {
            Driver.AddSequenceElement(executionType, childCount);
        }

        protected void GivenPauseElementActive(string duration)
        {
            Driver.SimulatePauseActive(duration);
        }

        protected void GivenEmptyAbilitiesOptionGroup(string characterName)
        {
            Driver.ClearAbilitiesOnCharacter(characterName);
        }

        protected void GivenAbilityAlreadyExists(string characterName, string abilityName)
        {
            Driver.AddAnimatedAbilityToCharacter(characterName, abilityName);
        }

        protected void GivenAbilityWithKey(string abilityName, string key)
        {
            Driver.SetAbilityActivationKey(abilityName, key);
        }

        protected void GivenCharacterNotSpawned(string characterName)
        {
            Driver.SetSpawnedNpcState(characterName, "absent");
        }

        protected void GivenPersistentFxCostumeVariantMissing()
        {
            Driver.SetPersistentFxCostumeVariantExists(false);
        }

        protected void GivenPersistentFxCostumeVariantExists()
        {
            Driver.SetPersistentFxCostumeVariantExists(true);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenGmPlaysAbility(string abilityName)
        {
            Driver.InvokePlayAbility(abilityName);
        }

        protected void WhenGmStopsAbility(string abilityName)
        {
            Driver.InvokeStopAbility(abilityName);
        }

        protected void WhenAllElementsComplete(string abilityName)
        {
            Driver.SimulateAllElementsComplete(abilityName);
        }

        protected void WhenSequenceExecutes(string executionType)
        {
            Driver.SimulateSequenceExecution(executionType);
        }

        protected void WhenIdentityChanges()
        {
            Driver.SimulateIdentityChange();
        }

        protected void WhenNewIdentityLoads()
        {
            Driver.SimulateNewIdentityLoaded();
        }

        protected void WhenCharacterDespawned(string characterName)
        {
            Driver.SetSpawnedNpcState(characterName, "absent");
            Driver.SimulateCharacterDespawn(characterName);
        }

        protected void WhenGmClearsPersistence(string abilityName)
        {
            Driver.InvokeClearPersistence(abilityName);
        }

        protected void WhenAddDefaultAbilitiesApplied(string characterName)
        {
            Driver.InvokeAddDefaultAbilities(characterName);
        }

        protected void WhenCharacterSpawned(string characterName)
        {
            Driver.SimulateCharacterSpawn(characterName);
        }

        protected void WhenConditionsChange()
        {
            Driver.SimulateEligibilityRefresh();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenAbilityHasExecutionState(string abilityName, string expected)
        {
            string actual = Driver.GetAbilityExecutionState(abilityName);
            Assert.AreEqual(expected, actual,
                string.Format("Ability '{0}': expected '{1}' got '{2}'", abilityName, expected, actual));
        }

        protected void ThenPlayBlocked()
        {
            Assert.IsTrue(Driver.WasPlayBlocked(), "Play should be blocked");
        }

        protected void ThenNoGameCommandIssued()
        {
            Assert.AreEqual(0, Driver.GetGameCommandCount(), "No game command should be issued");
        }

        protected void ThenAllChildrenExecutedInOrder()
        {
            Assert.IsTrue(Driver.WereAllChildrenExecutedInOrder(),
                "All children should execute in order");
        }

        protected void ThenExactlyOneChildExecuted()
        {
            Assert.AreEqual(1, Driver.GetExecutedChildCount(), "One child should execute");
        }

        protected void ThenCostumeVariantLoaded()
        {
            Assert.IsTrue(Driver.WasCostumeVariantLoaded(),
                "Persistent-FX costume variant should be loaded");
        }

        protected void ThenNoCostumeLoadCommand()
        {
            Assert.IsFalse(Driver.WasCostumeVariantLoaded(),
                "No costume load command should be issued");
        }

        protected void ThenAbilityCount(int expected)
        {
            int actual = Driver.GetAbilityCount();
            Assert.AreEqual(expected, actual,
                string.Format("Expected {0} abilities, got {1}", expected, actual));
        }

        protected void ThenDefaultAbilitiesPresent()
        {
            string[] defaults = new[] { "Recovery", "Stun Recovery", "Pass Turn",
                "Half Phase Action", "Hold Action", "Draw A Weapon", "Dodge",
                "Strike", "Haymaker", "Prone", "Move By", "Move Through",
                "Grab", "Disarm", "Block", "Set", "Sweep", "Rapid Fire",
                "Off Ground", "Generic Damage/Power" };
            foreach (string name in defaults)
            {
                Assert.IsTrue(Driver.AbilityExistsOnCharacter(name),
                    string.Format("Default ability '{0}' missing", name));
            }
        }

        protected void ThenEligibilityState(string abilityName, string expected)
        {
            string actual = Driver.GetAbilityEligibilityState(abilityName);
            Assert.AreEqual(expected, actual,
                string.Format("Eligibility for '{0}': expected '{1}' got '{2}'",
                    abilityName, expected, actual));
        }

        protected void ThenAbilityPersistence(string abilityName, string expected)
        {
            string actual = Driver.GetAbilityPersistence(abilityName);
            Assert.AreEqual(expected, actual,
                string.Format("Persistence for '{0}': expected '{1}' got '{2}'",
                    abilityName, expected, actual));
        }

        protected void ThenStopCompletesImmediately()
        {
            Assert.IsTrue(Driver.DidStopCompleteImmediately(), "Stop should be immediate");
        }
    }
}
