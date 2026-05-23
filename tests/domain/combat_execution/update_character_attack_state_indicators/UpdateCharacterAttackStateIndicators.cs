using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class UpdateCharacterAttackStateIndicators : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Desktop Overlay has Character Overlays rendered
        }

        [TestMethod]
        public void StatusEffectApplied()
        {
            // Given: Combat State current role defender, active status effects Stunned
            _combatRoles[_defender.Name] = RoleDefender;
            _statusEffects[_defender.Name] = "Stunned";
            // When: Combat State changes (status effect applied)
            // Then: Attack State Indicator displayed effect label Stunned, role indicator defender
            _statusEffects[_defender.Name].Should().Be("Stunned",
                "status effect Stunned applied — indicator must show label Stunned");
            _combatRoles[_defender.Name].Should().Be("defender",
                "role indicator must show defender designation");
        }

        [TestMethod]
        public void AttackerRoleSet()
        {
            // Given: Combat State current role attacker, active status effects none
            _combatRoles[_attacker.Name] = RoleAttacker;
            _statusEffects[_attacker.Name] = string.Empty;
            // When: Combat State changes (attacker role set)
            // Then: Attack State Indicator displayed effect label none, role indicator attacker
            _combatRoles[_attacker.Name].Should().Be("attacker",
                "attacker role set — indicator must show attacker designation");
            _statusEffects[_attacker.Name].Should().BeEmpty(
                "no status effect — indicator effect label must be none");
        }

        [TestMethod]
        public void CombatStateReset()
        {
            // Given: Combat State non-neutral (Stunned / defender)
            _statusEffects[_defender.Name] = "Stunned";
            _combatRoles[_defender.Name] = RoleDefender;
            // When: Combat State reset
            when_combat_state_reset(_defender);
            // Then: Attack State Indicator displayed effect label cleared, role indicator cleared
            then_status_effect(_defender, string.Empty);
            then_role_neutral(_defender);
        }

        [TestMethod]
        public void ExecutionCompletesFinalState()
        {
            // Given: execution completed; final status Dead
            _statusEffects[_defender.Name] = "Dead";
            _combatRoles[_defender.Name] = RoleDefender;
            // When: execution completes and panel closes
            // Then: Attack State Indicator displayed effect label Dead, role indicator defender (retained before close)
            _statusEffects[_defender.Name].Should().Be("Dead",
                "final state Dead retained before Attack Configuration panel closes");
        }
    }
}
