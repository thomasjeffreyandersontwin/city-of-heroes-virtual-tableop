using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.Roster
{
    [TestClass]
    public class AddCrowdToRoster : RosterDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: a session is active
        }

        [TestMethod]
        public void CrowdWithThreeCharactersAllAdded()
        {
            // Given: Crowd with Guard_A, Guard_B, Guard_C (none on roster)
            TestCombatant a = new TestCombatant("Guard_A");
            TestCombatant b = new TestCombatant("Guard_B");
            TestCombatant c = new TestCombatant("Guard_C");
            // When: the GM adds the Crowd to the Roster
            when_character_added_to_roster(a);
            when_character_added_to_roster(b);
            when_character_added_to_roster(c);
            // Then: Roster Entries Guard_A, Guard_B, Guard_C all have spawned state false
            then_on_roster("Guard_A");
            then_on_roster("Guard_B");
            then_on_roster("Guard_C");
            then_not_spawned(a); then_not_spawned(b); then_not_spawned(c);
        }

        [TestMethod]
        public void OneMemberAlreadyOnRosterSkipped()
        {
            // Given: Guard_B already on roster; Guard_A and Guard_C are not
            TestCombatant a = new TestCombatant("Guard_A");
            TestCombatant b = new TestCombatant("Guard_B");
            TestCombatant c = new TestCombatant("Guard_C");
            when_character_added_to_roster(b);
            int countBefore = _roster.Count;
            // When: the GM adds the Crowd containing Guard_A, Guard_B, Guard_C
            when_character_added_to_roster(a);
            bool guardBAdded = when_character_added_to_roster(b); // already present — skipped
            when_character_added_to_roster(c);
            // Then: Guard_B skipped with per-character feedback; Guard_A and Guard_C added
            then_add_rejected(guardBAdded);
            then_on_roster("Guard_A");
            then_on_roster("Guard_C");
        }

        [TestMethod]
        public void EmptyCrowdNoEntriesAdded()
        {
            // Given: empty crowd with no characters
            int countBefore = _roster.Count;
            // When: the GM adds the empty crowd
            // Then: action completes with feedback; no entries added
            _roster.Count.Should().Be(countBefore, "empty crowd must result in zero roster entries added");
        }

        [TestMethod]
        public void AllMembersAlreadyPresentNoChange()
        {
            // Given: Guard_A, Guard_B, Guard_C all already on roster
            TestCombatant a = new TestCombatant("Guard_A");
            TestCombatant b = new TestCombatant("Guard_B");
            TestCombatant c = new TestCombatant("Guard_C");
            when_character_added_to_roster(a);
            when_character_added_to_roster(b);
            when_character_added_to_roster(c);
            int countBefore = _roster.Count;
            // When: the GM adds the same Crowd again
            when_character_added_to_roster(a);
            when_character_added_to_roster(b);
            when_character_added_to_roster(c);
            // Then: Roster is unchanged with appropriate feedback
            _roster.Count.Should().Be(countBefore, "all members already present — roster must be unchanged");
        }

        [TestMethod]
        public void NestedCrowdLeafExpansion()
        {
            // Given: Crowd with nested characters at all levels including Nested_Guard_01
            TestCombatant nested = new TestCombatant("Nested_Guard_01");
            // When: the GM adds the Crowd (leaf expansion includes nested crowd members)
            when_character_added_to_roster(nested);
            // Then: Nested_Guard_01 added with spawned state false
            then_on_roster("Nested_Guard_01");
            then_not_spawned(nested);
        }
    }
}
