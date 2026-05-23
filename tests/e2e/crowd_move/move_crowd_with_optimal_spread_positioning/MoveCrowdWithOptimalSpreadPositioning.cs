using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CrowdMove
{
    [TestClass]
    public class MoveCrowdWithOptimalSpreadPositioning : CrowdMoveHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenRosterWithSpawnedCrowdMembers(new[] { "Guard_A", "Guard_B", "Guard_C" });
        }

        [TestMethod]
        public void MultipleMembersSpreadSlots()
        {
            GivenCrowdMoveStrategy("optimal spread", new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenGmDesignatesDestination("100.0", "0.0", "-200.0");
            ThenSpreadSlots("slot_1, slot_2, slot_3 (evenly spaced)");
        }

        [TestMethod]
        public void SingleMemberCenterSlot()
        {
            GivenCrowdMoveStrategy("optimal spread", new[] { "Guard_A" });
            WhenGmDesignatesDestination("100.0", "0.0", "-200.0");
            ThenSpreadSlots("destination_center");
        }

        [TestMethod]
        public void PartialObstructionNearestAlternatives()
        {
            GivenCrowdMoveStrategy("optimal spread", new[] { "Guard_A", "Guard_B", "Guard_C" });
            GivenPartialObstruction();
            WhenGmDesignatesDestination("100.0", "0.0", "-200.0");
            ThenSpreadSlots("nearest unobstructed alternatives");
        }

        [TestMethod]
        public void GangModeLeaderFacingApplied()
        {
            GivenGangModeActive("Guard_A", new[] { "Guard_A", "Guard_B" });
            GivenCrowdMoveStrategy("optimal spread", new[] { "Guard_A", "Guard_B" });
            WhenGmDesignatesDestination("100.0", "0.0", "-200.0");
            ThenSpreadSlots("slot_1, slot_2 (evenly spaced)");
        }
    }
}
