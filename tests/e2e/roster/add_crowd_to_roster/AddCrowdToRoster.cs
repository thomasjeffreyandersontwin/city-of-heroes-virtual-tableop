using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.Roster
{
    [TestClass]
    public class AddCrowdToRoster : RosterHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenSessionActive();
        }

        [TestMethod]
        public void CrowdWithThreeCharactersAllAdded()
        {
            GivenCrowdOnRoster("PatrolCrowd", new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenGmAddsCrowdToRoster("PatrolCrowd");
            ThenRosterEntryExists("Guard_A", "false");
            ThenRosterEntryExists("Guard_B", "false");
            ThenRosterEntryExists("Guard_C", "false");
        }

        [TestMethod]
        public void OneMemberAlreadyOnRosterSkipped()
        {
            GivenRosterEntry("Guard_B", "false", "hidden");
            GivenCrowdOnRoster("PatrolCrowd", new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenGmAddsCrowdToRoster("PatrolCrowd");
            ThenRosterEntryExists("Guard_A", "false");
            ThenRosterEntryExists("Guard_C", "false");
        }

        [TestMethod]
        public void EmptyCrowdNoEntriesAdded()
        {
            GivenCrowdOnRoster("EmptyCrowd", new string[0]);
            WhenGmAddsCrowdToRoster("EmptyCrowd");
        }

        [TestMethod]
        public void AllMembersAlreadyPresentNoChange()
        {
            GivenRosterEntry("Guard_A", "false", "hidden");
            GivenRosterEntry("Guard_B", "false", "hidden");
            GivenCrowdOnRoster("PatrolCrowd", new[] { "Guard_A", "Guard_B" });
            WhenGmAddsCrowdToRoster("PatrolCrowd");
        }

        [TestMethod]
        public void NestedCrowdLeafExpansion()
        {
            GivenCrowdOnRoster("NestedCrowd", new[] { "Nested_Guard_01" });
            WhenGmAddsCrowdToRoster("NestedCrowd");
            ThenRosterEntryExists("Nested_Guard_01", "false");
        }
    }
}
