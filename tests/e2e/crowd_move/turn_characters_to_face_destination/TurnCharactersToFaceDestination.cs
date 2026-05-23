using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CrowdMove
{
    [TestClass]
    public class TurnCharactersToFaceDestination : CrowdMoveHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenRosterWithSpawnedCrowdMembers(new[] { "Guard_A", "Guard_B", "Guard_C" });
        }

        [TestMethod]
        public void NonGangFaceDestination()
        {
            GivenGangModeInactive();
            WhenFacingCommandsIssued();
            ThenFacingVector("Guard_A", "toward_destination");
        }

        [TestMethod]
        public void GangLeaderFacingSubstitutes()
        {
            GivenGangModeActive("Guard_A", new[] { "Guard_A", "Guard_B" });
            WhenFacingCommandsIssued();
            ThenFacingVector("Guard_B", "leader_facing");
        }

        [TestMethod]
        public void MemberAtDestinationPointSkipped()
        {
            GivenGangModeInactive();
            GivenMemberAtDestination("Guard_C");
            WhenFacingCommandsIssued();
            ThenFacingVector("Guard_C", "skip_no_command");
        }

        [TestMethod]
        public void OneMemberFacingFailsOthersStillReceive()
        {
            GivenGangModeInactive();
            WhenFacingCommandsIssued();
            ThenFacingVector("Guard_A", "toward_destination");
        }
    }
}
