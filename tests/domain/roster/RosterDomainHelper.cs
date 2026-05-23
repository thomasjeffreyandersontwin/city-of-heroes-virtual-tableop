using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace HeroVTT.DomainTests.Roster
{
    public class RosterDomainHelper
    {
        protected List<TestCombatant> _roster;
        protected TestCombatant _guardCaptain;
        protected TestCombatant _villainBoss;
        protected TestCombatant _healer;

        [TestInitialize]
        public void Init()
        {
            _roster = new List<TestCombatant>();
            _guardCaptain = new TestCombatant("Guard_Captain_01");
            _villainBoss = new TestCombatant("Villain_Boss_03");
            _healer = new TestCombatant("Healer_01");
        }

        // Given helpers

        protected void given_character_on_roster(TestCombatant character)
        {
            if (!_roster.Any(r => r.Name == character.Name))
                _roster.Add(character);
        }

        protected void given_character_spawned(TestCombatant character)
        {
            character.HasBeenSpawned = true;
        }

        protected void given_character_active(TestCombatant character)
        {
            character.IsActive = true;
        }

        protected void given_gang_mode_active(TestCombatant leader, params TestCombatant[] members)
        {
            leader.IsGangLeader = true;
            foreach (var m in members)
                given_character_on_roster(m);
        }

        // When helpers

        protected bool when_character_added_to_roster(TestCombatant character)
        {
            if (_roster.Any(r => r.Name == character.Name)) return false;
            _roster.Add(character);
            return true;
        }

        protected void when_character_removed_from_roster(TestCombatant character)
        {
            if (character.HasBeenSpawned)
                character.HasBeenSpawned = false;
            _roster.RemoveAll(r => r.Name == character.Name);
        }

        protected void when_character_spawned(TestCombatant character)
        {
            character.HasBeenSpawned = true;
        }

        protected void when_character_despawned(TestCombatant character)
        {
            character.HasBeenSpawned = false;
        }

        protected void when_character_activated(TestCombatant character)
        {
            foreach (var r in _roster) r.IsActive = false;
            character.IsActive = true;
        }

        protected void when_character_deactivated(TestCombatant character)
        {
            character.IsActive = false;
        }

        protected void when_gang_deactivated(params TestCombatant[] members)
        {
            foreach (var m in members)
            {
                m.IsGangLeader = false;
                m.IsActive = false;
            }
        }

        // Then helpers

        protected void then_on_roster(string characterName)
        {
            _roster.Any(r => r.Name == characterName).Should().BeTrue(
                string.Format("'{0}' must be on the roster", characterName));
        }

        protected void then_not_on_roster(string characterName)
        {
            _roster.Any(r => r.Name == characterName).Should().BeFalse(
                string.Format("'{0}' must not be on the roster", characterName));
        }

        protected void then_roster_count(int expected)
        {
            _roster.Count.Should().Be(expected,
                string.Format("Roster must contain exactly {0} entries", expected));
        }

        protected void then_spawned(TestCombatant character)
        {
            character.HasBeenSpawned.Should().BeTrue(
                string.Format("'{0}' must have spawned state true", character.Name));
        }

        protected void then_not_spawned(TestCombatant character)
        {
            character.HasBeenSpawned.Should().BeFalse(
                string.Format("'{0}' must have spawned state false", character.Name));
        }

        protected void then_active(TestCombatant character)
        {
            character.IsActive.Should().BeTrue(
                string.Format("'{0}' must have active turn indicator visible", character.Name));
        }

        protected void then_not_active(TestCombatant character)
        {
            character.IsActive.Should().BeFalse(
                string.Format("'{0}' must not have active turn indicator", character.Name));
        }

        protected void then_add_rejected(bool added)
        {
            added.Should().BeFalse("duplicate character addition must be rejected");
        }
    }
}
