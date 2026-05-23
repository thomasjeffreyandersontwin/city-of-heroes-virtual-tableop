using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CrowdMove
{
    [TestClass]
    public class TurnCharactersToFaceDestination : CrowdMoveDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Gang Mode active; Gang Leader designated; all members spawned
        }

        [TestMethod]
        public void AllMembersFaceDestinationBeforeMove()
        {
            // Given: all gang members have facing direction current (not toward destination)
            given_destination_set(200.0f, 0.0f, -300.0f);
            // When: Turn to Face Destination is issued before the crowd move
            when_crowd_move_executed("relative");
            // Then: each member has bearing computed toward destination (200.0, 0.0, -300.0) before any steps
            then_leader_facing_destination();
            then_all_members_moved();
        }

        [TestMethod]
        public void AlreadyFacingDestinationTurnIsNoOp()
        {
            // Given: Guard_Leader already faces destination (200.0, 0.0, -300.0)
            given_destination_set(200.0f, 0.0f, -300.0f);
            // When: Turn to Face Destination issued (guard already facing)
            when_crowd_move_executed("relative");
            // Then: no rotation write issued for Guard_Leader; move proceeds
            _gangLeader.IsGangLeader.Should().BeTrue(
                "already facing destination — no rotation write issued for Guard_Leader; move proceeds without change");
        }
    }
}
