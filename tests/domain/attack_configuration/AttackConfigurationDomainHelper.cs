using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    public class AttackConfigurationDomainHelper
    {
        // Combat roles
        protected const string RoleAttacker = "attacker";
        protected const string RoleDefender = "defender";
        protected const string RoleNeutral = "neutral";

        protected TestCombatant _guardCaptain;
        protected TestCombatant _villainBoss;
        protected TestCombatant _healer;

        protected Dictionary<string, string> _combatRoles;
        protected string _attackerName;
        protected List<string> _defenderNames;
        protected bool _panelOpen;
        protected bool _targetsLocked;

        [TestInitialize]
        public void Init()
        {
            _guardCaptain = new TestCombatant("Guard_Captain_01");
            _guardCaptain.HasBeenSpawned = true;
            _villainBoss = new TestCombatant("Villain_Boss_03");
            _villainBoss.HasBeenSpawned = true;
            _healer = new TestCombatant("Healer_01");
            _healer.HasBeenSpawned = true;
            _combatRoles = new Dictionary<string, string>();
            _attackerName = string.Empty;
            _defenderNames = new List<string>();
            _panelOpen = false;
            _targetsLocked = false;
        }

        // Given helpers

        protected void given_panel_open()
        {
            _panelOpen = true;
        }

        protected void given_targets_confirmed()
        {
            _panelOpen = true;
            _targetsLocked = true;
        }

        // When helpers

        protected bool when_attacker_assigned(TestCombatant candidate)
        {
            if (!candidate.HasBeenSpawned) return false;
            if (_defenderNames.Contains(candidate.Name)) return false;
            _attackerName = candidate.Name;
            _combatRoles[candidate.Name] = RoleAttacker;
            return true;
        }

        protected bool when_defender_added(TestCombatant candidate)
        {
            if (!candidate.HasBeenSpawned) return false;
            if (candidate.Name == _attackerName) return false;
            if (_defenderNames.Contains(candidate.Name)) return false;
            _defenderNames.Add(candidate.Name);
            _combatRoles[candidate.Name] = RoleDefender;
            return true;
        }

        protected void when_defender_removed(TestCombatant defender)
        {
            _defenderNames.Remove(defender.Name);
            _combatRoles[defender.Name] = RoleNeutral;
        }

        protected bool when_targets_confirmed()
        {
            if (string.IsNullOrEmpty(_attackerName)) return false;
            if (_defenderNames.Count == 0) return false;
            _targetsLocked = true;
            return true;
        }

        // Then helpers

        protected void then_role(TestCombatant character, string expected)
        {
            string actual;
            if (!_combatRoles.TryGetValue(character.Name, out actual))
                actual = RoleNeutral;
            actual.Should().Be(expected,
                string.Format("'{0}' must have combat role '{1}'", character.Name, expected));
        }

        protected void then_panel_open()
        {
            _panelOpen.Should().BeTrue("Attack Configuration panel must be open");
        }

        protected void then_panel_closed()
        {
            _panelOpen.Should().BeFalse("Attack Configuration panel must be closed");
        }

        protected void then_targets_locked()
        {
            _targetsLocked.Should().BeTrue("targets must be locked after Confirm");
        }

        protected void then_defender_in_list(string name)
        {
            _defenderNames.Should().Contain(name,
                string.Format("'{0}' must be in the Defender list", name));
        }

        protected void then_defender_not_in_list(string name)
        {
            _defenderNames.Should().NotContain(name,
                string.Format("'{0}' must not be in the Defender list", name));
        }

        protected void then_confirmation_blocked(bool result)
        {
            result.Should().BeFalse("Confirm must be blocked");
        }
    }
}
