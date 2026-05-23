using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CrowdMove
{
    [TestClass]
    public class MoveCrowdWithRelativePositioning : CrowdMoveHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenRosterWithSpawnedCrowdMembers(new[] { "Guard_A", "Guard_B", "Guard_C" });
        }

        [TestMethod]
        public void AllMembersSpawnedMoveSimultaneously()
        {
            GivenCrowdMoveStrategy("relative", new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenGmDesignatesDestination("50.0", "0.0", "-30.0");
            ThenDisplacementVector("(50.0, 0.0, -30.0)");
        }

        [TestMethod]
        public void OneMemberUnspawnedSilentlyExcluded()
        {
            GivenCrowdMoveStrategy("relative", new[] { "Guard_A", "Guard_C" });
            WhenGmDesignatesDestination("50.0", "0.0", "-30.0");
            ThenDisplacementVector("(50.0, 0.0, -30.0)");
        }

        [TestMethod]
        public void ZeroOffsetDestinationNoMovement()
        {
            GivenCrowdMoveStrategy("relative", new[] { "Guard_A", "Guard_B" });
            WhenGmDesignatesDestination("0.0", "0.0", "0.0");
            ThenDisplacementVector("(0.0, 0.0, 0.0)");
        }

        [TestMethod]
        public void OneMemberFailsMidMoveOthersNotRolledBack()
        {
            GivenCrowdMoveStrategy("relative", new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenGmDesignatesDestination("50.0", "0.0", "-30.0");
            ThenDisplacementVector("(50.0, 0.0, -30.0)");
        }
    }
}
