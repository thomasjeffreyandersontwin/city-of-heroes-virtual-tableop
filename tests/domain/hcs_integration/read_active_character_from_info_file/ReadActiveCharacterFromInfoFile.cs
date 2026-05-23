using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.HcsIntegration
{
    [TestClass]
    public class ReadActiveCharacterFromInfoFile : HcsIntegrationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: HCS File Watcher is active
            given_watcher_active();
        }

        [TestMethod]
        public void CharacterMatched()
        {
            // Given: Info File active character data Guard_Captain_01
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Guard_Captain_01" });
            // Then: Active Character HCS HCS active turn designation Guard_Captain_01; HVT Active Character synced
            then_matched_count(matched, 1);
            matched.Should().Contain(_guardCaptain,
                "Guard_Captain_01 matched — HVT Active Character synchronized to Guard_Captain_01 Roster Entry");
        }

        [TestMethod]
        public void CharacterNotInRoster()
        {
            // Given: Info File active character data Unknown_NPC
            // When: a new Info File arrives
            var matched = when_process_info_file_on_deck(new[] { "Unknown_NPC" });
            // Then: Active Character HCS HCS active turn designation no_change; warning logged
            then_matched_count(matched, 0);
        }

        [TestMethod]
        public void DesignationAbsent()
        {
            // Given: Info File active character data (absent — field not present)
            // When: a new Info File arrives with no active character designation
            var matched = when_process_info_file_on_deck(new string[0]);
            // Then: Active Character HCS HCS active turn designation no_change; current selection unchanged
            then_matched_count(matched, 0);
        }
    }
}
