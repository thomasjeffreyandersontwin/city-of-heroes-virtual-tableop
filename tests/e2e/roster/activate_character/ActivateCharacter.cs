using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.Roster
{
    [TestClass]
    public class ActivateCharacter : RosterHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenSessionActive();
            GivenRosterHasEntries();
        }

        [TestMethod]
        public void ActivateNewEntry()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            WhenGmActivatesEntry("Guard_Captain_01");
            ThenActiveCharacter("Guard_Captain_01");
            ThenActiveTurnIndicator("Guard_Captain_01", "visible");
        }

        [TestMethod]
        public void ReplaceExistingActive()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            GivenRosterEntry("Villain_Boss_03", "true", "hidden");
            GivenActiveCharacter("Guard_Captain_01");
            WhenGmActivatesEntry("Villain_Boss_03");
            ThenActiveCharacter("Villain_Boss_03");
            ThenActiveTurnIndicator("Villain_Boss_03", "visible");
            ThenActiveTurnIndicator("Guard_Captain_01", "hidden");
        }

        [TestMethod]
        public void PreviousActiveCleared()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            GivenRosterEntry("Villain_Boss_03", "true", "hidden");
            GivenActiveCharacter("Guard_Captain_01");
            WhenGmActivatesEntry("Villain_Boss_03");
            ThenActiveTurnIndicator("Guard_Captain_01", "hidden");
        }

        [TestMethod]
        public void ActivateUnspawnedEntry()
        {
            GivenRosterEntry("Healer_01", "false", "hidden");
            WhenGmActivatesEntry("Healer_01");
            ThenActiveCharacter("Healer_01");
            ThenActiveTurnIndicator("Healer_01", "visible");
        }

        [TestMethod]
        public void AlreadyActiveNoOp()
        {
            GivenRosterEntry("Guard_Captain_01", "true", "hidden");
            GivenActiveCharacter("Guard_Captain_01");
            WhenGmActivatesEntry("Guard_Captain_01");
            ThenActiveCharacter("Guard_Captain_01");
            ThenActiveTurnIndicator("Guard_Captain_01", "visible");
        }

        [TestMethod]
        public void GangMemberActivatedGangOverrides()
        {
            GivenRosterEntry("Guard_A", "true", "visible");
            GivenGangMode("active", new[] { "Guard_A", "Guard_B", "Guard_C" });
            WhenGmActivatesEntry("Guard_A");
            ThenActiveCharacter("Guard_A");
            ThenActiveTurnIndicator("Guard_A", "visible");
        }
    }
}
