using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class DisableNonAttackAbilitiesDuringCombat : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration panel is open
        }

        [TestMethod]
        public void AssignedAsAttackerLocked()
        {
            // Given: Combatant Guard_Captain_01 has combat role attacker
            // When: Non-Attack Ability Lock evaluated
            bool lockActive = (_combatRoles[_attacker.Name] == RoleAttacker);
            // Then: Non-Attack Ability Lock suppression state active
            lockActive.Should().BeTrue(
                "combat role attacker — all non-attack abilities locked (suppression state active)");
        }

        [TestMethod]
        public void AssignedAsDefenderLocked()
        {
            // Given: Combatant Villain_Boss_03 has combat role defender
            // When: Non-Attack Ability Lock evaluated
            bool lockActive = (_combatRoles[_defender.Name] == RoleDefender);
            // Then: Non-Attack Ability Lock suppression state active
            lockActive.Should().BeTrue(
                "combat role defender — all non-attack abilities locked (suppression state active)");
        }

        [TestMethod]
        public void ConfigCancelledReleased()
        {
            // Given: Combatant had combat role attacker; config cancelled
            when_execution_cancelled();
            // When: Non-Attack Ability Lock evaluated after cancel
            bool lockActive = (_combatRoles[_attacker.Name] == RoleAttacker);
            // Then: Non-Attack Ability Lock suppression state released
            lockActive.Should().BeFalse(
                "config cancelled — Non-Attack Ability Lock must be released (suppression state released)");
        }

        [TestMethod]
        public void RemovedBeforeConfirmReleased()
        {
            // Given: Villain_Boss_03 was a defender (locked); removed before Confirm
            when_execution_cancelled();
            // When: Non-Attack Ability Lock evaluated after removal
            bool lockActive = (_combatRoles[_defender.Name] == RoleDefender);
            // Then: lock released immediately for that character
            lockActive.Should().BeFalse(
                "removed before Confirm — Non-Attack Ability Lock released immediately for Villain_Boss_03");
        }
    }
}
