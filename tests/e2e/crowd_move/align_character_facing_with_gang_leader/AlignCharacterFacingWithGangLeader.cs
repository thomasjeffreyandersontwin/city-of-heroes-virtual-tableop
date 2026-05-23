using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CrowdMove
{
    [TestClass]
    public class AlignCharacterFacingWithGangLeader : CrowdMoveHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenRosterWithSpawnedCrowdMembers(new[] { "Guard_A", "Guard_B", "Guard_C" });
        }

        [TestMethod]
        public void LeaderSpawnedAlignmentApplied()
        {
            GivenGangModeActive("Guard_A", new[] { "Guard_A", "Guard_B", "Guard_C" });
            GivenGangLeaderFacing("(1.0, 0.0, 0.0)");
            WhenGmTriggersAlignWithGangLeader();
            ThenFacingVector("Guard_B", "(1.0, 0.0, 0.0)");
            ThenFacingVector("Guard_C", "(1.0, 0.0, 0.0)");
        }

        [TestMethod]
        public void LeaderNotSpawnedBlocked()
        {
            GivenGangModeActive("Guard_A", new[] { "Guard_A", "Guard_B" });
            GivenGangLeaderFacing("unreadable");
            WhenGmTriggersAlignWithGangLeader();
            ThenFacingUnavailable();
        }

        [TestMethod]
        public void OneMemberNotSpawnedSkipped()
        {
            GivenGangModeActive("Guard_A", new[] { "Guard_A", "Guard_B", "Guard_C" });
            GivenGangLeaderFacing("(1.0, 0.0, 0.0)");
            WhenGmTriggersAlignWithGangLeader();
            ThenFacingVector("Guard_B", "(1.0, 0.0, 0.0)");
        }

        [TestMethod]
        public void GangModeNotActiveUnavailable()
        {
            GivenGangModeInactive();
            WhenGmTriggersAlignWithGangLeader();
            ThenFacingUnavailable();
        }
    }
}
