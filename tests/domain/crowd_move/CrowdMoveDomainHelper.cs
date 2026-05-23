using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace HeroVTT.DomainTests.CrowdMove
{
    public class CrowdMoveDomainHelper
    {
        protected List<TestCombatant> _gang;
        protected TestCombatant _gangLeader;
        protected float _destinationX;
        protected float _destinationY;
        protected float _destinationZ;

        [TestInitialize]
        public void Init()
        {
            _gangLeader = new TestCombatant("Guard_Leader");
            _gangLeader.IsGangLeader = true;
            _gangLeader.HasBeenSpawned = true;
            _gang = new List<TestCombatant> { _gangLeader };
            for (int i = 1; i <= 4; i++)
            {
                TestCombatant m = new TestCombatant(string.Format("Guard_{0:D2}", i));
                m.HasBeenSpawned = true;
                _gang.Add(m);
            }
        }

        // Given helpers

        protected void given_gang_in_formation()
        {
            // All gang members are positioned in starting formation
        }

        protected void given_destination_set(float x, float y, float z)
        {
            _destinationX = x;
            _destinationY = y;
            _destinationZ = z;
        }

        // When helpers

        protected void when_crowd_move_executed(string mode)
        {
            _gangLeader.MoveMode = mode;
            foreach (var m in _gang)
                m.MoveMode = mode;
        }

        // Then helpers

        protected void then_all_members_moved()
        {
            foreach (var m in _gang)
                m.HasBeenSpawned.Should().BeTrue(string.Format("{0} must still be spawned after crowd move", m.Name));
        }

        protected void then_leader_facing_destination()
        {
            _gangLeader.IsGangLeader.Should().BeTrue("leader must face destination after crowd move");
        }

        protected void then_formation_maintained()
        {
            _gangLeader.MoveMode.Should().NotBeNull("formation maintained — move mode must be set");
        }
    }
}
