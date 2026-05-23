using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.Roster
{
    [TestClass]
    public class AddCharacterToRoster : RosterHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenSessionActive();
        }

        [TestMethod]
        public void NewCharacterAddedSuccessfully()
        {
            WhenGmAddsCharacterToRoster("Guard_Captain_01");
            ThenRosterEntryExists("Guard_Captain_01", "false");
            ThenActiveTurnIndicator("Guard_Captain_01", "hidden");
            ThenGangIndicator("Guard_Captain_01", "hidden");
        }

        [TestMethod]
        public void DuplicateCharacterRejected()
        {
            GivenRosterEntry("Guard_Captain_01", "false", "hidden");
            WhenGmAddsCharacterToRoster("Guard_Captain_01");
            ThenRosterEntryRejected();
        }

        [TestMethod]
        public void EmptyRosterBeforeAddPlaceholderReplaced()
        {
            WhenGmAddsCharacterToRoster("Villain_Boss_03");
            ThenRosterEntryExists("Villain_Boss_03", "false");
            ThenActiveTurnIndicator("Villain_Boss_03", "hidden");
        }

        [TestMethod]
        public void MultipleAddedInSequence()
        {
            WhenGmAddsCharacterToRoster("Guard_Captain_01");
            WhenGmAddsCharacterToRoster("Healer_01");
            ThenRosterEntryExists("Healer_01", "false");
            ThenActiveTurnIndicator("Healer_01", "hidden");
        }

        [TestMethod]
        public void NoIdentityConfiguredStillAdded()
        {
            WhenGmAddsCharacterToRoster("Blank_Character");
            ThenRosterEntryExists("Blank_Character", "false");
            ThenActiveTurnIndicator("Blank_Character", "hidden");
        }
    }
}
