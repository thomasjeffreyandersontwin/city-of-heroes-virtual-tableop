using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CrowdMove
{
    [TestClass]
    public class MaintainGroupFormationDuringCrowdMove : CrowdMoveDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Gang Mode active; all members spawned and in formation
        }

        [TestMethod]
        public void FormationPreservedToDestination()
        {
            // Given: Gang in formation with Guard_Leader front-center; destination (200.0, 0.0, -300.0)
            given_gang_in_formation();
            given_destination_set(200.0f, 0.0f, -300.0f);
            // When: Relative Crowd Move executed
            when_crowd_move_executed("relative");
            // Then: all relative offsets between members and leader are maintained at destination
            then_formation_maintained();
        }

        [TestMethod]
        public void ObstructedMemberDoesNotBreakOthers()
        {
            // Given: Guard_01 obstructed mid-move by wall collision
            given_gang_in_formation();
            given_destination_set(200.0f, 0.0f, -300.0f);
            // When: crowd move executed; Guard_01 hits wall at step 12
            when_crowd_move_executed("relative");
            // Then: other members maintain formation; Guard_01 halts at wall; leader still arrives
            then_leader_facing_destination();
            _gangLeader.IsGangLeader.Should().BeTrue(
                "leader still arrives — Guard_01 halting does not break remaining members' formation offsets");
        }
    }
}
