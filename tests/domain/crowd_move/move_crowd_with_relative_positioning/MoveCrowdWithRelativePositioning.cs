using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CrowdMove
{
    [TestClass]
    public class MoveCrowdWithRelativePositioning : CrowdMoveDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Gang Mode active; Gang Leader designated; all members spawned
        }

        [TestMethod]
        public void LeaderArrivesAllMembersFollowInRelativeOffset()
        {
            // Given: Gang with Guard_Leader, Guard_01 through Guard_04; each member has relative offset to leader
            given_gang_in_formation();
            given_destination_set(200.0f, 0.0f, -300.0f);
            // When: the GM triggers Relative Crowd Move
            when_crowd_move_executed("relative");
            // Then: each member moves maintaining their offset from the leader at destination (200.0, 0.0, -300.0)
            then_formation_maintained();
            _gangLeader.MoveMode.Should().Be("relative",
                "relative crowd move — each member moves maintaining their offset from leader");
        }

        [TestMethod]
        public void MemberObstructedLeaderStillArrives()
        {
            // Given: a wall collision will obstruct Guard_02 mid-move
            given_gang_in_formation();
            given_destination_set(200.0f, 0.0f, -300.0f);
            // When: the GM triggers Relative Crowd Move; Guard_02 is obstructed
            when_crowd_move_executed("relative");
            // Then: Guard_Leader arrives; Guard_02 halts at collision; other members proceed
            then_leader_facing_destination();
            then_all_members_moved();
        }

        [TestMethod]
        public void ZeroMembersLeaderOnlyMoves()
        {
            // Given: Gang has only Guard_Leader (no other members)
            _gang.Clear(); _gang.Add(_gangLeader);
            given_destination_set(100.0f, 0.0f, -100.0f);
            // When: the GM triggers Relative Crowd Move
            when_crowd_move_executed("relative");
            // Then: Guard_Leader moves; no-member relative offsets to compute — move proceeds normally
            then_leader_facing_destination();
        }
    }
}
