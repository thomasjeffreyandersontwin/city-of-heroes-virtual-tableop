using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    public class AttackConfigurationHelper
    {
        protected AppDriver Driver;

        protected void GivenGameBridgeInitialized()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenAttackConfigPanelOpen()
        {
            Driver.OpenAttackConfigurationPanel();
        }

        protected void GivenAttackerAssigned(string characterName)
        {
            Driver.SetAttackerAssignment(characterName);
        }

        protected void GivenDefenderAdded(string characterName)
        {
            Driver.AddDefenderToConfiguration(characterName);
        }

        protected void GivenTargetsConfirmed()
        {
            Driver.ConfirmAttackTargets();
        }

        protected void GivenSpawnedState(string characterName, string state)
        {
            Driver.SetRosterEntrySpawnedState(characterName, state);
        }

        protected void GivenAttackResult(string pairId, string result)
        {
            Driver.SetAttackResultForPair(pairId, result);
        }

        protected void GivenAreaCenterDesignated(string centerNpc)
        {
            Driver.SetAreaCenterDesignation(centerNpc);
        }

        protected void GivenSweepAttackOrder(string[] pairs)
        {
            Driver.SetSweepAttackOrder(pairs);
        }

        protected void GivenAutoFireShotCount(int count)
        {
            Driver.SetAutoFireShotCount(count);
        }

        protected void WhenGmSelectsAttacker(string characterName)
        {
            Driver.InvokeSelectAttacker(characterName);
        }

        protected void WhenGmActivatesAttackAbility(string characterName)
        {
            Driver.InvokeActivateAttackAbility(characterName);
        }

        protected void WhenGmAddsDefender(string characterName)
        {
            Driver.InvokeAddDefender(characterName);
        }

        protected void WhenGmRemovesDefender(string characterName)
        {
            Driver.InvokeRemoveDefender(characterName);
        }

        protected void WhenGmClicksConfirmTargets()
        {
            Driver.InvokeConfirmAttackTargets();
        }

        protected void WhenGmEditsAttackParameters(string pairId, string effect, int knockback, string result)
        {
            Driver.InvokeEditAttackParameters(pairId, effect, knockback, result);
        }

        protected void WhenGmSelectsAttackEffect(string pairId, string effectType)
        {
            Driver.InvokeSetAttackEffect(pairId, effectType);
        }

        protected void WhenGmEntersKnockbackDistance(string pairId, string distance)
        {
            Driver.InvokeSetKnockbackDistance(pairId, distance);
        }

        protected void WhenGmSelectsAttackResult(string pairId, string resultType)
        {
            Driver.InvokeSetAttackResult(pairId, resultType);
        }

        protected void WhenGmSelectsAttackMode(string modeType)
        {
            Driver.InvokeSetAttackMode(modeType);
        }

        protected void WhenGmDesignatesAreaCenter(string targetNpc)
        {
            Driver.InvokeDesignateAreaCenter(targetNpc);
        }

        protected void WhenGmUnchecksAreaCenter()
        {
            Driver.InvokeUncheckAreaCenter();
        }

        protected void WhenGmConfirmsAreaAttack()
        {
            Driver.InvokeConfirmAreaAttack();
        }

        protected void WhenGmConfirmsSweepAttack()
        {
            Driver.InvokeConfirmSweepAttack();
        }

        protected void WhenGmEntersAutoFireShots(string count)
        {
            Driver.InvokeSetAutoFireShots(count);
        }

        protected void WhenGmTriggersSpreadAttack(string centerNpc)
        {
            Driver.InvokeSpreadAttack(centerNpc);
        }

        protected void ThenAttackerRole(string characterName, string expected)
        {
            string actual = Driver.GetCombatStateRole(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Role for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenSelectionRejected()
        {
            Assert.IsNotNull(Driver.GetLastValidationMessage(), "Expected rejection");
        }

        protected void ThenPanelOpened(string attacker)
        {
            Assert.IsTrue(Driver.IsAttackConfigPanelOpen(), "Panel should be open");
            Assert.AreEqual(attacker, Driver.GetAttackerAssignment());
        }

        protected void ThenPanelNotOpened()
        {
            Assert.IsFalse(Driver.IsAttackConfigPanelOpen(), "Panel should not be open");
        }

        protected void ThenNonAttackAbilitiesLocked(string characterName)
        {
            Assert.IsTrue(Driver.AreNonAttackAbilitiesLocked(characterName),
                string.Format("Non-attack abilities for '{0}' should be locked", characterName));
        }

        protected void ThenNonAttackAbilitiesReleased(string characterName)
        {
            Assert.IsFalse(Driver.AreNonAttackAbilitiesLocked(characterName),
                string.Format("Non-attack abilities for '{0}' should be released", characterName));
        }

        protected void ThenTargetsLocked()
        {
            Assert.IsTrue(Driver.AreTargetsLocked(), "Targets should be locked");
        }

        protected void ThenConfirmBlocked()
        {
            Assert.IsTrue(Driver.IsConfirmBlocked(), "Confirm should be blocked");
        }

        protected void ThenPairParameters(string pairId, string effect, string knockback, string result)
        {
            Assert.AreEqual(effect, Driver.GetPairAttackEffect(pairId));
            Assert.AreEqual(knockback, Driver.GetPairKnockbackDistance(pairId));
            Assert.AreEqual(result, Driver.GetPairAttackResult(pairId));
        }

        protected void ThenStatusEffect(string pairId, string expected)
        {
            string actual = Driver.GetPairStatusEffectApplied(pairId);
            Assert.AreEqual(expected, actual,
                string.Format("Status effect for pair '{0}': expected '{1}' got '{2}'", pairId, expected, actual));
        }

        protected void ThenKnockbackStored(string pairId, string expected)
        {
            string actual = Driver.GetPairKnockbackDistance(pairId);
            Assert.AreEqual(expected, actual,
                string.Format("Knockback for pair '{0}': expected '{1}' got '{2}'", pairId, expected, actual));
        }

        protected void ThenAttackMode(string expected)
        {
            string actual = Driver.GetAttackMode();
            Assert.AreEqual(expected, actual,
                string.Format("Attack mode: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenAreaCenterDesignated(string expected)
        {
            string actual = Driver.GetAreaCenterDesignation();
            Assert.AreEqual(expected, actual,
                string.Format("Area center: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenDefendersPopulated(string[] expected)
        {
            foreach (string d in expected)
                Assert.IsTrue(Driver.IsDefenderInConfiguration(d),
                    string.Format("'{0}' should be a defender", d));
        }

        protected void ThenDefendersEmpty()
        {
            Assert.IsTrue(Driver.IsDefenderListEmpty(), "Defender list should be empty");
        }

        protected void GivenBlockedLos(string defender)
        {
            Driver.SetLosBlocked(defender, true);
        }

        protected void GivenCharactersInRange(string[] characters)
        {
            Driver.SetCharactersInRange(characters);
        }

        protected void GivenPopUpMenuNotDeployed()
        {
            Driver.SetPopUpMenuDeployed(false);
        }

        protected void GivenNonAttackAbilitiesLocked(string characterName)
        {
            Driver.SetNonAttackAbilityLock(characterName, true);
        }

        protected void ThenLineOfSight(string defender, string expected)
        {
            string actual = Driver.GetLineOfSightState(defender);
            Assert.AreEqual(expected, actual,
                string.Format("LOS for '{0}': expected '{1}' got '{2}'", defender, expected, actual));
        }

        protected void ThenSweepResolved(string[] resolved)
        {
            foreach (string pair in resolved)
                Assert.IsTrue(Driver.WasSweepPairResolved(pair),
                    string.Format("Pair '{0}' should be resolved", pair));
        }

        protected void ThenSweepNotResolved(string[] notResolved)
        {
            foreach (string pair in notResolved)
                Assert.IsFalse(Driver.WasSweepPairResolved(pair),
                    string.Format("Pair '{0}' should not be resolved", pair));
        }

        protected void ThenAutoFireDistribution(string expected)
        {
            string actual = Driver.GetAutoFireDistribution();
            Assert.AreEqual(expected, actual,
                string.Format("Auto-fire distribution: expected '{0}' got '{1}'", expected, actual));
        }
    }
}
