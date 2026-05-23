using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.HcsIntegration
{
    [TestClass]
    public class ReadEligibleCombatantsFromInfoFile : HcsIntegrationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: HCS File Watcher is active
            given_watcher_active();
            TestCombatant guardB = new TestCombatant("Guard_B");
            TestCombatant villainC = new TestCombatant("Villain_C");
            _roster.Add(guardB); _roster.Add(villainC);
        }

        [TestMethod]
        public void CharactersMatched()
        {
            // Given: Info File eligible combatants data Guard_A, Guard_B, Villain_C
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Guard_A", "Guard_B", "Villain_C" });
            // Then: Eligible Combatants available-to-act characters Guard_A, Guard_B, Villain_C; eligible status reflected in UI
            then_matched_count(matched, 3);
        }

        [TestMethod]
        public void OneCharacterUnmatched()
        {
            // Given: Info File eligible combatants data Guard_A, Unknown_Y
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Guard_A", "Unknown_Y" });
            // Then: Eligible Combatants available-to-act characters Guard_A (only); Unknown_Y skipped with warning
            then_matched_count(matched, 1);
            matched.Should().NotContain(r => r.Name == "Unknown_Y",
                "Unknown_Y not in roster — skipped with warning");
        }

        [TestMethod]
        public void EmptyList()
        {
            // Given: Info File eligible combatants data (empty)
            // When: a new Info File arrives with empty eligible combatants data
            var matched = when_process_info_file_on_deck(new string[0]);
            // Then: Eligible Combatants available-to-act characters none; no characters marked eligible
            then_matched_count(matched, 0);
        }
    }
}
