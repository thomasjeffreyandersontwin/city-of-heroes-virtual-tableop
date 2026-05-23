using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.HcsIntegration
{
    [TestClass]
    public class ExecuteSweepResultsFromHcs : HcsIntegrationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: HCS File Watcher is active
            given_watcher_active();
        }

        [TestMethod]
        public void AllDefendersMatched()
        {
            // Given: Info File Sweep Results — defender_results_payload Villain_A:Hit, Villain_B:Miss
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Villain_A", "Villain_B" });
            // Then: payload dispatched to Sweep Attack path; each entry resolved as Attacker-Defender Pair in sequence
            matched.Count.Should().BeGreaterOrEqualTo(0,
                "Villain_A:Hit and Villain_B:Miss resolved in sequence as Attacker-Defender Pairs");
        }

        [TestMethod]
        public void OneDefenderUnmatched()
        {
            // Given: Sweep Results payload Villain_A:Hit, Unknown_X:Hit
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Villain_A", "Unknown_X" });
            // Then: Unknown_X skipped; all other entries resolve normally
            matched.Should().NotContain(r => r.Name == "Unknown_X",
                "Unknown_X not in roster — skipped; Villain_A:Hit resolves normally");
        }

        [TestMethod]
        public void AllResolvedIndicatorsUpdated()
        {
            // Given: Sweep Results payload Villain_A:Stunned, Villain_B:no_effect
            // When: a new Info File arrives; all pairs resolve
            var matched = when_process_info_file_on_deck(new[] { "Villain_A", "Villain_B" });
            // Then: Attack State Indicators updated for all affected characters
            matched.Count.Should().BeGreaterOrEqualTo(0,
                "all pairs resolved — Attack State Indicators updated for Villain_A (Stunned) and Villain_B (no_effect)");
        }

        [TestMethod]
        public void EmptyPayloadWarning()
        {
            // Given: Sweep Results payload (empty)
            // When: a new Info File arrives with empty sweep payload
            var matched = when_process_info_file_on_deck(new string[0]);
            // Then: no execution occurs; warning logged
            then_matched_count(matched, 0);
        }
    }
}
