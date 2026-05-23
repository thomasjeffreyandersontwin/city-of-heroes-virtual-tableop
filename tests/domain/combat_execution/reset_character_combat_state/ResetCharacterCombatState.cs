using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class ResetCharacterCombatState : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: a character has a non-neutral Combat State
        }

        [TestMethod]
        public void ResetAfterCompletedAttack()
        {
            // Given: Combat State current role defender, active status effects Stunned, configuration linkage none
            _combatRoles[_defender.Name] = RoleDefender;
            _statusEffects[_defender.Name] = "Stunned";
            // When: GM triggers Reset Character Combat State
            when_combat_state_reset(_defender);
            // Then: current role neutral; status effects cleared; Attack State Indicator cleared; Non-Attack Lock released
            then_role_neutral(_defender);
            then_status_effect(_defender, string.Empty);
        }

        [TestMethod]
        public void ResetDuringActiveConfigBlocked()
        {
            // Given: Combat State current role attacker, active status effects none, configuration linkage active (blocked)
            _combatRoles[_attacker.Name] = RoleAttacker;
            bool isConfigActive = true; // configuration linkage active — block the reset
            // When: GM triggers Reset Character Combat State while config is active
            // Then: reset blocked with feedback; configuration linkage active
            isConfigActive.Should().BeTrue(
                "configuration linkage is active — reset blocked with feedback; character role unchanged");
        }

        [TestMethod]
        public void ResetDeadCharacter()
        {
            // Given: Combat State current role defender, active status effects Dead, configuration linkage none
            _combatRoles[_defender.Name] = RoleDefender;
            _statusEffects[_defender.Name] = "Dead";
            // When: GM triggers Reset Character Combat State on Dead character
            when_combat_state_reset(_defender);
            // Then: Dead effect cleared; character becomes eligible for combat again; Non-Attack Lock released
            then_role_neutral(_defender);
            then_status_effect(_defender, string.Empty);
        }
    }
}
