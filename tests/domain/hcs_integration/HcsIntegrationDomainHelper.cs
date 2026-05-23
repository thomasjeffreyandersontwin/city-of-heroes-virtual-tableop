using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace HeroVTT.DomainTests.HcsIntegration
{
    public class HcsIntegrationDomainHelper
    {
        protected bool _gameBridgeReady;
        protected bool _watcherActive;
        protected bool _outputDirExists;
        protected List<TestCombatant> _roster;
        protected TestCombatant _guardCaptain;
        protected TestCombatant _guardA;
        protected TestCombatant _villainB;

        [TestInitialize]
        public void Init()
        {
            _gameBridgeReady = true;
            _outputDirExists = true;
            _watcherActive = false;
            _guardCaptain = new TestCombatant("Guard_Captain_01");
            _guardA = new TestCombatant("Guard_A");
            _villainB = new TestCombatant("Villain_B");
            _roster = new List<TestCombatant> { _guardCaptain, _guardA, _villainB };
        }

        // Given helpers

        protected void given_watcher_active()
        {
            _watcherActive = true;
        }

        // When helpers

        protected bool when_start_watcher()
        {
            if (!_gameBridgeReady || !_outputDirExists) return false;
            if (_watcherActive) return true;
            _watcherActive = true;
            return true;
        }

        protected bool when_stop_watcher()
        {
            if (!_watcherActive) return false;
            _watcherActive = false;
            return true;
        }

        protected List<TestCombatant> when_process_info_file_on_deck(string[] characterNames)
        {
            List<TestCombatant> matched = new List<TestCombatant>();
            foreach (string name in characterNames)
            {
                TestCombatant m = _roster.Find(r => r.Name == name);
                if (m != null) matched.Add(m);
            }
            return matched;
        }

        // Then helpers

        protected void then_watcher_monitoring()
        {
            _watcherActive.Should().BeTrue("HCS File Watcher must be in monitoring state");
        }

        protected void then_watcher_not_monitoring()
        {
            _watcherActive.Should().BeFalse("HCS File Watcher must not be monitoring");
        }

        protected void then_integration_active()
        {
            _watcherActive.Should().BeTrue("HCS Integration must be active");
        }

        protected void then_integration_inactive()
        {
            _watcherActive.Should().BeFalse("HCS Integration must be inactive");
        }

        protected void then_matched_count(List<TestCombatant> matched, int expected)
        {
            matched.Count.Should().Be(expected,
                string.Format("Matched character count must be {0}", expected));
        }
    }
}
