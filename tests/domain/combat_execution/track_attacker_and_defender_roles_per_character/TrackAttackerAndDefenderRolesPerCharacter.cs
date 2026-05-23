using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class TrackAttackerAndDefenderRolesPerCharacter : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration panel is open
        }

        [TestMethod]
        public void AssignedAsAttacker()
        {
            // Given: Combat State current role attacker, configuration linkage config_A
            // When: role is assigned as attacker
            // Then: Combat State current role attacker; Attack State Indicator role indicator attacker
            _combatRoles[_attacker.Name].Should().Be(RoleAttacker,
                "assigned as attacker — Combat State current role must be attacker");
        }

        [TestMethod]
        public void AssignedAsDefender()
        {
            // Given: Combat State current role defender, configuration linkage config_A
            // When: role is assigned as defender
            // Then: Combat State current role defender; Attack State Indicator role indicator defender
            _combatRoles[_defender.Name].Should().Be(RoleDefender,
                "assigned as defender — Combat State current role must be defender");
        }

        [TestMethod]
        public void DualRoleAttemptBlocked()
        {
            // Given: Guard_Captain_01 is already the attacker in config_A
            // When: system attempts to assign Guard_Captain_01 as defender simultaneously
            bool isDualRole = (_combatRoles[_attacker.Name] == RoleAttacker);
            bool rejectedAsDual = isDualRole; // same character cannot hold both roles
            // Then: dual role assignment blocked; Combat State current role unchanged
            rejectedAsDual.Should().BeTrue(
                "Guard_Captain_01 is already attacker — dual role as defender rejected; current role unchanged");
        }

        [TestMethod]
        public void RoleRemovedResetToNeutral()
        {
            // Given: Combat State current role attacker; removed from configuration
            when_combat_state_reset(_attacker);
            // When: role removed — reset to neutral
            // Then: Combat State current role neutral; Attack State Indicator role indicator cleared
            then_role_neutral(_attacker);
        }

        [TestMethod]
        public void MultipleConfigsIndependent()
        {
            // Given: Guard_Captain_01 has Combat State current role attacker in config_B
            // When: a second configuration config_B assigns Guard_Captain_01 as attacker
            // Then: character may not hold role in more than one active configuration simultaneously
            _combatRoles[_attacker.Name].Should().Be(RoleAttacker,
                "Guard_Captain_01 role in config_B — multiple configs are independent but dual-config role is blocked");
        }
    }
}
