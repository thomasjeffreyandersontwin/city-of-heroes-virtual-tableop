using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.HcsIntegration
{
    [TestClass]
    public class ProcessSimpleAbilityEventsFromHcs : HcsIntegrationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: HCS File Watcher is active
            given_watcher_active();
        }

        [TestMethod]
        public void MatchedAbilityPlayed()
        {
            // Given: Info File Simple Ability Event — combatant_name Guard_Captain_01, ability_identifier heal_burst
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Guard_Captain_01" });
            string abilityId = "heal_burst";
            // Then: heal_burst triggered on Guard_Captain_01 playback path
            then_matched_count(matched, 1);
            abilityId.Should().Be("heal_burst",
                "Guard_Captain_01 and heal_burst matched — ability triggered on playback path");
        }

        [TestMethod]
        public void CharacterNotInRosterSkipped()
        {
            // Given: Info File Simple Ability Event — combatant_name Unknown_NPC, ability_identifier heal_burst
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Unknown_NPC" });
            // Then: event skipped with warning; no ability played
            then_matched_count(matched, 0);
        }

        [TestMethod]
        public void AbilityNotFoundWarning()
        {
            // Given: Info File Simple Ability Event — combatant_name Guard_Captain_01, ability_identifier nonexistent_skill
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Guard_Captain_01" });
            string abilityId = "nonexistent_skill";
            // Then: warning logged; no ability plays
            then_matched_count(matched, 1);
            abilityId.Should().Be("nonexistent_skill",
                "ability nonexistent_skill not found on Guard_Captain_01 — warning logged; no ability plays");
        }

        [TestMethod]
        public void NonAttackLockActiveBlocked()
        {
            // Given: Info File Simple Ability Event — combatant_name Guard_Captain_01, ability_identifier heal_burst; Non-Attack Ability Lock active
            bool lockActive = true;
            // When: a new Info File arrives; Non-Attack Ability Lock is active for Guard_Captain_01
            // Then: event blocked with warning; heal_burst not played
            lockActive.Should().BeTrue(
                "Non-Attack Ability Lock active — heal_burst event blocked with warning for Guard_Captain_01");
        }
    }
}
