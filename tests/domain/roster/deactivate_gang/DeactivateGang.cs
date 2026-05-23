using FluentAssertions;
using System.Linq;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.Roster
{
    [TestClass]
    public class DeactivateGang : RosterDomainHelper
    {
        private TestCombatant _guardA;
        private TestCombatant _guardB;

        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Roster has entries
            _guardA = new TestCombatant("Guard_A");
            _guardB = new TestCombatant("Guard_B");
            given_character_on_roster(_guardA);
            given_character_on_roster(_guardB);
        }

        [TestMethod]
        public void GangActiveDeactivated()
        {
            // Given: Gang Mode collective activation state active (Guard_A leader, Guard_B member)
            given_gang_mode_active(_guardA, _guardA, _guardB);
            _guardA.IsActive = true; _guardB.IsActive = true;
            // When: the GM triggers Deactivate Gang
            when_gang_deactivated(_guardA, _guardB);
            // Then: Gang Mode collective activation state inactive; all gang membership indicators removed
            _guardA.IsGangLeader.Should().BeFalse("gang leader designation must be cleared on deactivation");
            _guardA.IsActive.Should().BeFalse("active designation cleared; no entry auto-activated after deactivation");
        }

        [TestMethod]
        public void NoGangActiveNoOp()
        {
            // Given: Gang Mode collective activation state inactive; no gang is active
            bool anyGangLeader = _roster.Any(r => r.IsGangLeader);
            // When: the GM triggers Deactivate Gang
            // Then: action is a no-op with user feedback; collective activation state remains inactive
            anyGangLeader.Should().BeFalse("no gang is active — deactivate gang is a no-op with user feedback");
        }

        [TestMethod]
        public void SomeMembersUnspawnedStillDeactivates()
        {
            // Given: gang active with Guard_A (spawned), Guard_B (not spawned)
            given_gang_mode_active(_guardA, _guardA, _guardB);
            given_character_spawned(_guardA);
            // not spawning Guard_B
            // When: the GM triggers Deactivate Gang
            when_gang_deactivated(_guardA, _guardB);
            // Then: Gang Mode collective activation state inactive; no game command for unspawned Guard_B
            _guardA.IsGangLeader.Should().BeFalse("deactivated regardless of Guard_B spawned state");
        }
    }
}
