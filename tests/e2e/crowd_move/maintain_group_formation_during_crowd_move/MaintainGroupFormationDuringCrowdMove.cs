using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CrowdMove
{
    [TestClass]
    public class MaintainGroupFormationDuringCrowdMove : CrowdMoveHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenRosterWithSpawnedCrowdMembers(new[] { "Guard_A", "Guard_B", "Guard_C" });
        }

        [TestMethod]
        public void FormationPreservedAfterMove()
        {
            GivenGroupFormationOffsets("A:(0,0,0), B:(5,0,0), C:(0,0,5)");
            GivenCrowdMoveStrategy("relative", new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenCrowdMoveCompletes();
            ThenFormationPreserved("A:(0,0,0), B:(5,0,0), C:(0,0,5)");
        }

        [TestMethod]
        public void DifferentStartingPositionsPreserved()
        {
            GivenGroupFormationOffsets("A:(0,0,0), B:(10,0,0), C:(5,0,10)");
            GivenCrowdMoveStrategy("relative", new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenCrowdMoveCompletes();
            ThenFormationPreserved("A:(0,0,0), B:(10,0,0), C:(5,0,10)");
        }

        [TestMethod]
        public void MemberPositionUnreadableMoveBlocked()
        {
            GivenGroupFormationOffsets("blocked until resolved");
            GivenCrowdMoveStrategy("relative", new[] { "Guard_A", "Guard_B", "Guard_C" });
            ThenMoveBlocked();
        }
    }
}
