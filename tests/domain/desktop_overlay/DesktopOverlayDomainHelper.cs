using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace HeroVTT.DomainTests.DesktopOverlay
{
    public class DesktopOverlayDomainHelper
    {
        protected List<TestCombatant> _overlays;
        protected TestCombatant _guardCaptain;
        protected TestCombatant _villainBoss;
        protected List<TestCombatant> _selection;

        [TestInitialize]
        public void Init()
        {
            _guardCaptain = new TestCombatant("Guard_Captain_01");
            _guardCaptain.HasBeenSpawned = true;
            _villainBoss = new TestCombatant("Villain_Boss_03");
            _villainBoss.HasBeenSpawned = true;
            _overlays = new List<TestCombatant> { _guardCaptain, _villainBoss };
            _selection = new List<TestCombatant>();
        }

        // Given helpers

        protected TestCombatant given_overlay(string name, bool spawned = true)
        {
            TestCombatant m = new TestCombatant(name);
            m.HasBeenSpawned = spawned;
            _overlays.Add(m);
            return m;
        }

        protected void given_selected(TestCombatant character)
        {
            if (!_selection.Contains(character))
                _selection.Add(character);
        }

        // When helpers

        protected void when_single_click(TestCombatant target)
        {
            _selection.Clear();
            if (target != null)
                _selection.Add(target);
        }

        protected void when_click_empty_space()
        {
            _selection.Clear();
        }

        protected void when_modifier_click_add(TestCombatant target)
        {
            if (!_selection.Contains(target))
                _selection.Add(target);
        }

        protected void when_modifier_click_remove(TestCombatant target)
        {
            _selection.Remove(target);
        }

        protected void when_double_click(TestCombatant target)
        {
            if (target != null && target.HasBeenSpawned)
                target.IsActive = true;
        }

        // Then helpers

        protected void then_selected(TestCombatant character)
        {
            _selection.Should().Contain(character,
                string.Format("'{0}' must be in the selection", character.Name));
        }

        protected void then_not_selected(TestCombatant character)
        {
            _selection.Should().NotContain(character,
                string.Format("'{0}' must not be in the selection", character.Name));
        }

        protected void then_selection_count(int expected)
        {
            _selection.Count.Should().Be(expected,
                string.Format("Selection must contain exactly {0} overlays", expected));
        }

        protected void then_selection_empty()
        {
            _selection.Should().BeEmpty("all selections must be cleared");
        }

        protected void then_active(TestCombatant character)
        {
            character.IsActive.Should().BeTrue(
                string.Format("'{0}' must be the active character after double-click", character.Name));
        }
    }
}
