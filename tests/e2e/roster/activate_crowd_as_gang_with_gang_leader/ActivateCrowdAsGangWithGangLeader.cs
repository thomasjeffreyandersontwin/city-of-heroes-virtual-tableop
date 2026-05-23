using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.Roster
{
    [TestClass]
    public class ActivateCrowdAsGangWithGangLeader : RosterHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenSessionActive();
            GivenRosterHasEntries();
        }

        [TestMethod]
        public void GangActivatedSuccessfully()
        {
            GivenRosterEntry("Guard_A", "true", "hidden");
            GivenRosterEntry("Guard_B", "true", "hidden");
            GivenRosterEntry("Guard_C", "true", "hidden");
            GivenCrowdOnRoster("PatrolCrowd", new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenGmActivatesGang("PatrolCrowd", "Guard_A");
            ThenGangModeState("active");
            ThenGangLeader("Guard_A");
            ThenGangIndicator("Guard_A", "visible");
            ThenGangIndicator("Guard_B", "visible");
            ThenGangIndicator("Guard_C", "visible");
        }

        [TestMethod]
        public void MemberMissingFromRosterRejected()
        {
            GivenRosterEntry("Guard_A", "true", "hidden");
            GivenCrowdOnRoster("PatrolCrowd", new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenGmActivatesGang("PatrolCrowd", "Guard_A");
            ThenGangModeState("inactive");
        }

        [TestMethod]
        public void NoLeaderDesignatedBlocked()
        {
            GivenRosterEntry("Guard_A", "true", "hidden");
            GivenRosterEntry("Guard_B", "true", "hidden");
            GivenCrowdOnRoster("PatrolCrowd", new[] { "Guard_A", "Guard_B" });
            WhenGmActivatesGang("PatrolCrowd", "");
            ThenGangModeState("inactive");
        }

        [TestMethod]
        public void ExistingGangReplacedNewLeader()
        {
            GivenRosterEntry("Villain_A", "true", "hidden");
            GivenRosterEntry("Villain_B", "true", "hidden");
            GivenGangMode("active", new[] { "Guard_A", "Guard_B" });
            GivenCrowdOnRoster("VillainCrowd", new[] { "Villain_A", "Villain_B" });
            WhenGmActivatesGang("VillainCrowd", "Villain_A");
            ThenGangModeState("active");
            ThenGangLeader("Villain_A");
        }

        [TestMethod]
        public void SingleMemberGangValid()
        {
            GivenRosterEntry("Guard_A", "true", "hidden");
            GivenCrowdOnRoster("SingleCrowd", new[] { "Guard_A" });
            WhenGmActivatesGang("SingleCrowd", "Guard_A");
            ThenGangModeState("active");
            ThenGangLeader("Guard_A");
        }
    }
}
