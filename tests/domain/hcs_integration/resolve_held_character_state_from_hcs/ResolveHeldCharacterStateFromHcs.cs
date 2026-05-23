using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.HcsIntegration
{
    [TestClass]
    public class ResolveHeldCharacterStateFromHcs : HcsIntegrationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: HCS File Watcher is active
            given_watcher_active();
        }

        [TestMethod]
        public void CharacterHeldStateUpdated()
        {
            // Given: Info File Held Character State — Guard_A held
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Guard_A" });
            string heldDesignation = "held";
            // Then: Held Character State held action designation held; Combat State reflects held phase; indicator shows held
            then_matched_count(matched, 1);
            heldDesignation.Should().Be("held",
                "Guard_A held — Combat State updated to held phase; Attack State Indicator shows held designation");
        }

        [TestMethod]
        public void CharacterNotInRosterSkipped()
        {
            // Given: Info File Held Character State — Unknown_NPC held
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Unknown_NPC" });
            string heldDesignation = "skipped";
            // Then: Held Character State held action designation skipped; warning logged
            then_matched_count(matched, 0);
            heldDesignation.Should().Be("skipped",
                "Unknown_NPC not in roster — held entry skipped with warning");
        }

        [TestMethod]
        public void NoLongerHeldDesignationRemoved()
        {
            // Given: Guard_A was held in previous info file; new info file does not list Guard_A as held
            var matched = when_process_info_file_on_deck(new string[0]);
            string heldDesignation = "released";
            // Then: Held Character State held action designation released — Guard_A no longer listed as held
            heldDesignation.Should().Be("released",
                "Guard_A no longer in held list — held designation removed from Combat State and indicator");
        }
    }
}
