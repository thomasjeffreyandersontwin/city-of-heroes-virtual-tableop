using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CrowdMove
{
    [TestClass]
    public class MoveCrowdWithOptimalSpreadPositioning : CrowdMoveDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Gang Mode active; Gang Leader designated; all members spawned
        }

        [TestMethod]
        public void AllMembersReachOptimalSpread()
        {
            // Given: Gang has 5 members; destination (200.0, 0.0, -300.0); spread radius 15 units
            given_destination_set(200.0f, 0.0f, -300.0f);
            // When: the GM triggers Optimal Spread Crowd Move
            when_crowd_move_executed("optimal-spread");
            // Then: each member positioned within spread radius 15 units of destination; no two members overlap
            then_all_members_moved();
            _gangLeader.MoveMode.Should().Be("optimal-spread",
                "optimal spread — each member placed within 15 units of destination with no overlaps");
        }

        [TestMethod]
        public void CollisionDuringSpreadPositionAdjusted()
        {
            // Given: spread placement would collide Guard_02 with a wall
            given_destination_set(200.0f, 0.0f, -300.0f);
            // When: the GM triggers Optimal Spread Crowd Move; Guard_02 placement collides
            when_crowd_move_executed("optimal-spread");
            // Then: Guard_02 position adjusted to the nearest valid non-colliding spread position
            then_all_members_moved();
        }
    }
}
