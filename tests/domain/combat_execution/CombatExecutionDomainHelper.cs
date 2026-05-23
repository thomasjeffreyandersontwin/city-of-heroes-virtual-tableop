using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace HeroVTT.DomainTests.CombatExecution
{
    public class CombatExecutionDomainHelper
    {
        protected const string RoleNeutral = "neutral";
        protected const string RoleAttacker = "attacker";
        protected const string RoleDefender = "defender";

        protected TestCombatant _attacker;
        protected TestCombatant _defender;

        protected Dictionary<string, string> _combatRoles;
        protected Dictionary<string, string> _statusEffects;
        protected bool _executionInProgress;
        protected bool _panelOpen;

        [TestInitialize]
        public void Init()
        {
            _attacker = new TestCombatant("Guard_Captain_01");
            _attacker.HasBeenSpawned = true;
            _defender = new TestCombatant("Villain_Boss_03");
            _defender.HasBeenSpawned = true;
            _combatRoles = new Dictionary<string, string>
            {
                { _attacker.Name, RoleAttacker },
                { _defender.Name, RoleDefender }
            };
            _statusEffects = new Dictionary<string, string>();
            _executionInProgress = false;
            _panelOpen = true;
        }

        // Given helpers

        protected void given_execution_in_progress()
        {
            _executionInProgress = true;
        }

        // When helpers

        protected void when_execution_begins()
        {
            _executionInProgress = true;
        }

        protected void when_execution_cancelled()
        {
            _panelOpen = false;
            _executionInProgress = false;
            _combatRoles[_attacker.Name] = RoleNeutral;
            _combatRoles[_defender.Name] = RoleNeutral;
        }

        protected void when_execution_aborted()
        {
            _executionInProgress = false;
            _combatRoles[_attacker.Name] = RoleNeutral;
            _combatRoles[_defender.Name] = RoleNeutral;
        }

        protected void when_combat_state_reset(TestCombatant character)
        {
            _combatRoles[character.Name] = RoleNeutral;
            _statusEffects[character.Name] = string.Empty;
        }

        // Then helpers

        protected void then_status_effect(TestCombatant character, string expected)
        {
            string actual;
            if (!_statusEffects.TryGetValue(character.Name, out actual))
                actual = string.Empty;
            actual.Should().Be(expected,
                string.Format("'{0}' must have status effect '{1}'", character.Name, expected));
        }

        protected void then_panel_closed()
        {
            _panelOpen.Should().BeFalse("Attack Configuration panel must be closed");
        }

        protected void then_role_neutral(TestCombatant character)
        {
            string actual;
            if (!_combatRoles.TryGetValue(character.Name, out actual))
                actual = RoleNeutral;
            actual.Should().Be(RoleNeutral,
                string.Format("'{0}' combat role must be neutral", character.Name));
        }

        protected void then_execution_stopped()
        {
            _executionInProgress.Should().BeFalse("Combat Execution must be stopped");
        }
    }
}
