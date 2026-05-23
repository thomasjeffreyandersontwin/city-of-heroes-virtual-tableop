using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;

namespace HeroVTT.DomainTests.AnimatedAbilityManagement
{
    [TestClass]
    public class SetDefaultAbilityForCharacter : AnimatedAbilityManagementDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Character Guard_Captain
        }

        [TestMethod]
        public void SetNewDefault()
        {
            // Given: Animated Ability Recovery in the Ability Option Group
            AnimatedAbility recovery = given_ability("Recovery");
            given_ability_on_character(_guardCaptain, recovery);
            // When: the GM uses set-default on Animated Ability Recovery
            when_default_ability_set(_guardCaptain, "Recovery");
            // Then: Ability Option Group designates Animated Ability Recovery as default ability with state default
            then_default_ability(_guardCaptain, "Recovery");
        }

        [TestMethod]
        public void DefaultAbilityAutoPlaysOnSpawn()
        {
            // Given: Animated Ability Recovery has default designation default
            AnimatedAbility recovery = given_ability("Recovery");
            given_ability_on_character(_guardCaptain, recovery);
            when_default_ability_set(_guardCaptain, "Recovery");
            // When: Character Guard_Captain is spawned (Spawned NPC becomes present)
            // Then: Animated Ability Recovery is automatically played on the Spawned NPC; no manual play needed
            _guardCaptain.DefaultAbility.Should().NotBeNull(
                "a default ability must be set so auto-play fires on spawn");
            _guardCaptain.DefaultAbility.Name.Should().Be("Recovery",
                "Recovery must be the default ability that auto-plays on spawn");
        }

        [TestMethod]
        public void DefaultAbilityRemovedFromCharacter()
        {
            // Given: Animated Ability Recovery has default designation default
            AnimatedAbility recovery = given_ability("Recovery");
            given_ability_on_character(_guardCaptain, recovery);
            when_default_ability_set(_guardCaptain, "Recovery");
            // When: Animated Ability Recovery is removed from the Character
            when_ability_deleted(_guardCaptain, "Recovery");
            // Then: no Animated Ability on the Character carries the default designation
            //       subsequent spawns do not auto-play any ability
            then_no_default_ability(_guardCaptain);
        }

        [TestMethod]
        public void ClearDefaultDesignation()
        {
            // Given: Animated Ability Recovery has default designation default
            AnimatedAbility recovery = given_ability("Recovery");
            given_ability_on_character(_guardCaptain, recovery);
            when_default_ability_set(_guardCaptain, "Recovery");
            // When: the GM toggles off the default designation on the current default ability
            _guardCaptain.DefaultAbility = null;
            // Then: no Animated Ability on the Character has default designation default
            then_no_default_ability(_guardCaptain);
        }
    }
}
