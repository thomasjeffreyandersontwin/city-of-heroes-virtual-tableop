using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatExecution
{
    public class CombatExecutionHelper
    {
        protected AppDriver Driver;

        protected void GivenCombatExecutionBegun()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
            Driver.BeginCombatExecution();
        }

        protected void GivenDesktopOverlayWithCharacters()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
            Driver.EnsureDesktopOverlayRendered();
        }

        protected void GivenAttackConfigPanelOpen()
        {
            Driver.OpenAttackConfigurationPanel();
        }

        protected void GivenCombatExecutionInProgress(string[] pairSequence)
        {
            Driver.SetCombatExecutionPairSequence(pairSequence);
        }

        protected void GivenNonNeutralCombatState(string characterName, string role, string effects)
        {
            Driver.SetCombatState(characterName, role, effects);
        }

        protected void GivenCombatRole(string characterName, string role)
        {
            Driver.SetCombatState(characterName, role, "none");
        }

        protected void GivenAttackAnimation(string ability)
        {
            Driver.SetAttackAnimation(ability);
        }

        protected void GivenOnHitAnimation(string ability)
        {
            Driver.SetOnHitAnimation(ability);
        }

        protected void GivenPairResult(string pairId, string result)
        {
            Driver.SetAttackResultForPair(pairId, result);
        }

        protected void GivenPairKnockback(string pairId, string distance)
        {
            Driver.SetPairKnockbackDistance(pairId, distance);
        }

        protected void GivenObstructionPresent()
        {
            Driver.SetCollisionObstructionPresent(true);
        }

        protected void GivenPairEffect(string pairId, string effect)
        {
            Driver.SetPairAttackEffect(pairId, effect);
        }

        protected void GivenSpawnedState(string characterName, string state)
        {
            Driver.SetRosterEntrySpawnedState(characterName, state);
        }

        protected void GivenConfigurationLinkage(string characterName, string linkage)
        {
            Driver.SetConfigurationLinkage(characterName, linkage);
        }

        protected void WhenPairResolutionBegins(string pairId)
        {
            Driver.InvokeResolvePair(pairId);
        }

        protected void WhenAttackAnimationCompletes(string pairId)
        {
            Driver.InvokePostAttackAnimation(pairId);
        }

        protected void WhenKnockbackExecutes(string pairId)
        {
            Driver.InvokeKnockbackStep(pairId);
        }

        protected void WhenStatusEffectExecutes(string pairId)
        {
            Driver.InvokeStatusEffectStep(pairId);
        }

        protected void WhenCombatStateChanges(string characterName)
        {
            Driver.InvokeCombatStateChange(characterName);
        }

        protected void WhenGmClicksCancel()
        {
            Driver.InvokeCancelAttack();
        }

        protected void WhenGmClicksAbort()
        {
            Driver.InvokeAbortAttack();
        }

        protected void WhenGmTriggersResetCombatState(string characterName)
        {
            Driver.InvokeResetCombatState(characterName);
        }

        protected void WhenNonAttackLockEvaluated(string characterName)
        {
            Driver.InvokeEvaluateNonAttackLock(characterName);
        }

        protected void WhenRoleAssigned(string characterName, string role)
        {
            Driver.InvokeAssignCombatRole(characterName, role);
        }

        protected void WhenRoleRemoved(string characterName)
        {
            Driver.InvokeRemoveCombatRole(characterName);
        }

        protected void ThenAttackAnimationPlayed()
        {
            Assert.IsTrue(Driver.WasAttackAnimationPlayed(), "Attack animation should play");
        }

        protected void ThenAttackAnimationSkipped()
        {
            Assert.IsFalse(Driver.WasAttackAnimationPlayed(), "Attack animation should be skipped");
        }

        protected void ThenOnHitAnimationPlayed()
        {
            Assert.IsTrue(Driver.WasOnHitAnimationPlayed(), "On-hit animation should play");
        }

        protected void ThenOnHitAnimationSkipped()
        {
            Assert.IsFalse(Driver.WasOnHitAnimationPlayed(), "On-hit should be skipped");
        }

        protected void ThenKnockbackDestination(string pairId, string expected)
        {
            string actual = Driver.GetKnockbackDestination(pairId);
            Assert.AreEqual(expected, actual,
                string.Format("Knockback for '{0}': expected '{1}' got '{2}'", pairId, expected, actual));
        }

        protected void ThenStatusEffectApplied(string characterName, string expected)
        {
            string actual = Driver.GetCharacterStatusEffect(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Status for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenAttackStateIndicator(string characterName, string effectLabel, string roleIndicator)
        {
            string actualLabel = Driver.GetAttackStateEffectLabel(characterName);
            string actualRole = Driver.GetAttackStateRoleIndicator(characterName);
            Assert.AreEqual(effectLabel, actualLabel);
            Assert.AreEqual(roleIndicator, actualRole);
        }

        protected void ThenCombatStateNeutral(string characterName)
        {
            string actual = Driver.GetCombatStateRole(characterName);
            Assert.AreEqual("neutral", actual,
                string.Format("Combat state for '{0}' should be neutral", characterName));
        }

        protected void ThenCombatStateRole(string characterName, string expected)
        {
            string actual = Driver.GetCombatStateRole(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Role for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenNonAttackAbilitiesLocked(string characterName)
        {
            Assert.IsTrue(Driver.AreNonAttackAbilitiesLocked(characterName));
        }

        protected void ThenNonAttackAbilitiesReleased(string characterName)
        {
            Assert.IsFalse(Driver.AreNonAttackAbilitiesLocked(characterName));
        }

        protected void ThenResetBlocked()
        {
            Assert.IsNotNull(Driver.GetLastValidationMessage(), "Reset should be blocked");
        }

        protected void ThenPanelClosed()
        {
            Assert.IsFalse(Driver.IsAttackConfigPanelOpen(), "Panel should be closed");
        }

        protected void ThenAbortButtonDisabled()
        {
            Assert.IsTrue(Driver.IsAbortButtonDisabled(), "Abort should be disabled");
        }

        protected void ThenIndicatorCleared(string characterName)
        {
            Assert.AreEqual("cleared", Driver.GetAttackStateEffectLabel(characterName));
            Assert.AreEqual("cleared", Driver.GetAttackStateRoleIndicator(characterName));
        }
    }
}
