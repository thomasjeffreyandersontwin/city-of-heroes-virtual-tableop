using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.HcsIntegration
{
    [TestClass]
    public class ReadOnDeckCombatantsFromInfoFile : HcsIntegrationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: HCS File Watcher is active
            given_watcher_active();
        }

        [TestMethod]
        public void CharactersMatched()
        {
            // Given: Info File on-deck combatants data Guard_A, Villain_B
            // When: a new Info File arrives with on-deck combatants data Guard_A, Villain_B
            var matched = when_process_info_file_on_deck(new[] { "Guard_A", "Villain_B" });
            // Then: On-Deck Combatants imminent turn characters Guard_A, Villain_B; overlays highlighted
            then_matched_count(matched, 2);
            matched.Should().Contain(_guardA, "Guard_A must be matched from roster");
            matched.Should().Contain(_villainB, "Villain_B must be matched from roster");
        }

        [TestMethod]
        public void OneCharacterUnmatched()
        {
            // Given: Info File on-deck combatants data Guard_A, Unknown_X
            // When: a new Info File arrives with Guard_A, Unknown_X
            var matched = when_process_info_file_on_deck(new[] { "Guard_A", "Unknown_X" });
            // Then: On-Deck Combatants imminent turn characters Guard_A (only); Unknown_X skipped with warning
            then_matched_count(matched, 1);
            matched.Should().Contain(_guardA, "Guard_A must be matched");
            matched.Should().NotContain(r => r.Name == "Unknown_X", "Unknown_X not in roster — skipped with warning");
        }

        [TestMethod]
        public void EmptyList()
        {
            // Given: Info File on-deck combatants data (empty)
            // When: a new Info File arrives with empty on-deck combatants data
            var matched = when_process_info_file_on_deck(new string[0]);
            // Then: On-Deck Combatants imminent turn characters none; no overlays highlighted
            then_matched_count(matched, 0);
        }
    }
}
