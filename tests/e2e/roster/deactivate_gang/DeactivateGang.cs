using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.Roster
{
    [TestClass]
    public class DeactivateGang : RosterHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenSessionActive();
            GivenRosterHasEntries();
        }

        [TestMethod]
        public void GangActiveDeactivated()
        {
            GivenRosterEntry("Guard_A", "true", "visible");
            GivenRosterEntry("Guard_B", "true", "visible");
            GivenGangMode("active", new[] { "Guard_A", "Guard_B" });
            WhenGmDeactivatesGang();
            ThenGangModeState("inactive");
            ThenGangIndicator("Guard_A", "hidden");
            ThenGangIndicator("Guard_B", "hidden");
        }

        [TestMethod]
        public void NoGangActiveNoOp()
        {
            GivenGangMode("inactive", new string[0]);
            WhenGmDeactivatesGang();
            ThenGangModeState("inactive");
        }

        [TestMethod]
        public void SomeMembersUnspawnedStillDeactivates()
        {
            GivenRosterEntry("Guard_A", "true", "visible");
            GivenRosterEntry("Guard_B", "false", "visible");
            GivenGangMode("active", new[] { "Guard_A", "Guard_B" });
            WhenGmDeactivatesGang();
            ThenGangModeState("inactive");
            ThenGangIndicator("Guard_A", "hidden");
            ThenGangIndicator("Guard_B", "hidden");
        }
    }
}
