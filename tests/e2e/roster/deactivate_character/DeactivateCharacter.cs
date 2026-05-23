using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.Roster
{
    [TestClass]
    public class DeactivateCharacter : RosterHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenSessionActive();
            GivenRosterHasEntries();
        }

        [TestMethod]
        public void ActiveEntryDeactivated()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            GivenActiveCharacter("Guard_Captain_01");
            WhenGmDeactivatesEntry("Guard_Captain_01");
            ThenNoActiveCharacter();
        }

        [TestMethod]
        public void NotActiveNoOp()
        {
            GivenRosterEntry("Villain_Boss_03", "true", "hidden");
            GivenActiveCharacter("Guard_Captain_01");
            WhenGmDeactivatesEntry("Villain_Boss_03");
            ThenActiveCharacter("Guard_Captain_01");
        }

        [TestMethod]
        public void GangMemberDeactivatedIndividually()
        {
            GivenRosterEntry("Guard_A", "true", "visible");
            GivenActiveCharacter("Guard_A");
            GivenGangMode("active", new[] { "Guard_A", "Guard_B" });
            WhenGmDeactivatesEntry("Guard_A");
            ThenNoActiveCharacter();
        }
    }
}
