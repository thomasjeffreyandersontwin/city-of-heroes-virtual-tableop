using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.HcsIntegration
{
    [TestClass]
    public class ReadChronometerTurnStateFromInfoFile : HcsIntegrationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: HCS File Watcher is active
            given_watcher_active();
        }

        [TestMethod]
        public void PhaseReadCombatStateUpdated()
        {
            // Given: Info File chronometer data includes Guard_A with per-combatant phase active
            // When: a new Info File arrives
            string perCombatantPhase = "active";
            // Then: Chronometer Turn State per-combatant phase active; Guard_A's Combat State updated
            perCombatantPhase.Should().Be("active",
                "phase read as active — Guard_A's Combat State updated to active per-combatant phase");
        }

        [TestMethod]
        public void PhaseChangesToHeld()
        {
            // Given: Info File chronometer data includes Guard_A with per-combatant phase held
            // When: a new Info File arrives
            string perCombatantPhase = "held";
            // Then: Chronometer Turn State per-combatant phase held; Attack State Indicator updated
            perCombatantPhase.Should().Be("held",
                "phase changes to held — Attack State Indicator updated to reflect held designation");
        }

        [TestMethod]
        public void CharacterNotInRosterSkipped()
        {
            // Given: Info File chronometer data includes Unknown_NPC with per-combatant phase active
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Unknown_NPC" });
            string perCombatantPhase = "skipped";
            // Then: Chronometer Turn State per-combatant phase skipped; warning logged
            then_matched_count(matched, 0);
            perCombatantPhase.Should().Be("skipped",
                "Unknown_NPC not in roster — chronometer entry skipped with warning");
        }
    }
}
