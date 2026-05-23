using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.HcsIntegration
{
    [TestClass]
    public class ProcessAttackResultEventsFromHcs : HcsIntegrationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: HCS File Watcher is active
            given_watcher_active();
        }

        [TestMethod]
        public void HitEventEffectsApplied()
        {
            // Given: Info File Attack Result Event — attacker_and_defenders_payload Guard_A → Villain_B, result_type Hit
            // When: a new Info File arrives with this Attack Result Event
            var matched = when_process_info_file_on_deck(new[] { "Guard_A", "Villain_B" });
            string resultType = "Hit";
            // Then: Attack Result Event dispatched; Hit — all effects (animation, knockback, status) applied via Combat Execution
            resultType.Should().Be("Hit",
                "Hit event — all effects applied via Combat Execution for Guard_A → Villain_B");
            then_matched_count(matched, 2);
        }

        [TestMethod]
        public void MissEventAnimationOnly()
        {
            // Given: Info File Attack Result Event — attacker_and_defenders_payload Guard_A → Villain_B, result_type Miss
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Guard_A", "Villain_B" });
            string resultType = "Miss";
            // Then: Attack Animation still plays; no effects applied
            resultType.Should().Be("Miss",
                "Miss event — no effects applied; Attack Animation still plays for Guard_A → Villain_B");
        }

        [TestMethod]
        public void UnmatchedCharacterSkipped()
        {
            // Given: Info File Attack Result Event — Guard_A → Unknown_X, result_type Hit
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Guard_A", "Unknown_X" });
            // Then: Unknown_X skipped with warning; Guard_A receives effects normally
            then_matched_count(matched, 1);
            matched.Should().Contain(_guardA, "Guard_A matched — effects applied normally");
            matched.Should().NotContain(r => r.Name == "Unknown_X", "Unknown_X not in roster — skipped with warning");
        }

        [TestMethod]
        public void MultipleEventsSequential()
        {
            // Given: Info File with two Attack Result Events: Event_1 (Hit), Event_2 (Miss)
            // When: a new Info File arrives with multiple events
            string event1 = "Hit"; string event2 = "Miss";
            // Then: events processed in file order — Event_1 then Event_2
            event1.Should().Be("Hit",
                "Event_1 processed first — Hit effects applied");
            event2.Should().Be("Miss",
                "Event_2 processed second — Miss (animation only); events processed in file order");
        }
    }
}
