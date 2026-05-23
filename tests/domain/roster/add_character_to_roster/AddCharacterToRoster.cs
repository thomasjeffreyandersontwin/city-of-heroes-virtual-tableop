using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.Roster
{
    [TestClass]
    public class AddCharacterToRoster : RosterDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: a session is active
        }

        [TestMethod]
        public void NewCharacterAddedSuccessfully()
        {
            // Given: Guard_Captain_01 not yet on roster; session active
            // When: the GM adds Character Guard_Captain_01 to the Roster
            bool added = when_character_added_to_roster(_guardCaptain);
            // Then: Roster Entry Guard_Captain_01 has spawned state false, active turn indicator hidden, gang indicator hidden
            added.Should().BeTrue("new character must be added successfully");
            then_on_roster("Guard_Captain_01");
            then_not_spawned(_guardCaptain);
            then_not_active(_guardCaptain);
        }

        [TestMethod]
        public void DuplicateCharacterRejected()
        {
            // Given: Guard_Captain_01 already on roster
            when_character_added_to_roster(_guardCaptain);
            int countBefore = _roster.Count;
            // When: the GM adds Character Guard_Captain_01 again
            bool added = when_character_added_to_roster(_guardCaptain);
            // Then: addition rejected with user feedback; roster unchanged
            then_add_rejected(added);
            then_roster_count(countBefore);
        }

        [TestMethod]
        public void EmptyRosterBeforeAddPlaceholderReplaced()
        {
            // Given: roster is empty (placeholder visible); Villain_Boss_03 being added
            then_roster_count(0);
            // When: the GM adds Character Villain_Boss_03 to the Roster
            bool added = when_character_added_to_roster(_villainBoss);
            // Then: Roster Entry Villain_Boss_03 has spawned state false; empty-roster placeholder is replaced
            added.Should().BeTrue();
            then_on_roster("Villain_Boss_03");
        }

        [TestMethod]
        public void MultipleAddedInSequence()
        {
            // Given: session active; roster empty
            // When: the GM adds Healer_01, Guard_Captain_01, Villain_Boss_03 in sequence
            when_character_added_to_roster(_healer);
            when_character_added_to_roster(_guardCaptain);
            when_character_added_to_roster(_villainBoss);
            // Then: each entry is independent with spawned state false, active indicator hidden
            then_on_roster("Healer_01");
            then_not_spawned(_healer);
            then_not_spawned(_guardCaptain);
            then_not_spawned(_villainBoss);
        }

        [TestMethod]
        public void NoIdentityConfiguredStillAdded()
        {
            // Given: Blank_Character has no identity configured
            TestCombatant blank = new TestCombatant("Blank_Character");
            // When: the GM adds Blank_Character to the Roster
            bool added = when_character_added_to_roster(blank);
            // Then: Roster Entry Blank_Character has spawned state false; identity not required for roster membership
            added.Should().BeTrue("identity or ability configuration is not required for roster membership");
            then_on_roster("Blank_Character");
            then_not_spawned(blank);
        }
    }
}
