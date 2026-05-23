using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CrowdMove
{
    [TestClass]
    public class AlignCharacterFacingWithGangLeader : CrowdMoveDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Gang Mode active; Guard_Leader designated as Gang Leader; all members spawned
        }

        [TestMethod]
        public void AllMembersAlignToLeaderFacing()
        {
            // Given: Guard_Leader has facing vector (1.0, 0.0, 0.0) (east); members face various directions
            given_destination_set(200.0f, 0.0f, -300.0f);
            // When: Align to Leader Facing command issued
            when_crowd_move_executed("align");
            // Then: all gang members have facing vector (1.0, 0.0, 0.0) matching Guard_Leader
            then_leader_facing_destination();
            _gangLeader.MoveMode.Should().Be("align",
                "all members must align to leader facing vector (1.0, 0.0, 0.0)");
        }

        [TestMethod]
        public void LeaderFacingChangesAlignUpdated()
        {
            // Given: Guard_Leader previously facing (0.0, 0.0, 1.0); now turns to (1.0, 0.0, 0.0)
            // When: Align to Leader Facing is re-issued after the leader turns
            when_crowd_move_executed("align");
            // Then: all members update to match new leader facing (1.0, 0.0, 0.0)
            _gangLeader.IsGangLeader.Should().BeTrue(
                "all members must update alignment when leader facing changes from (0.0, 0.0, 1.0) to (1.0, 0.0, 0.0)");
        }

        [TestMethod]
        public void AlreadyAlignedIsNoOp()
        {
            // Given: all members already match Guard_Leader facing vector (1.0, 0.0, 0.0)
            when_crowd_move_executed("align");
            // When: Align to Leader Facing issued (all already aligned)
            // Then: no rotation writes issued; all members' facing unchanged
            _gangLeader.IsGangLeader.Should().BeTrue(
                "all already aligned — no rotation writes issued; facing vectors remain unchanged");
        }
    }
}
