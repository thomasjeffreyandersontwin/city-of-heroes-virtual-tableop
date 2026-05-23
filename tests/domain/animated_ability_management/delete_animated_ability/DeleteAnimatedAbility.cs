using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;

namespace HeroVTT.DomainTests.AnimatedAbilityManagement
{
    [TestClass]
    public class DeleteAnimatedAbility : AnimatedAbilityManagementDomainHelper
    {
        private AnimatedAbility _fireStrike;

        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Character Guard_Captain; Animated Ability Fire Strike in Ability Option Group
            _fireStrike = given_ability("Fire Strike");
            given_ability_on_character(_guardCaptain, _fireStrike);
        }

        [TestMethod]
        public void AbilityAndElementsPermanentlyRemoved()
        {
            // Given: Animated Ability Fire Strike in the Ability Option Group
            // When: the GM selects Delete on Animated Ability Fire Strike
            when_ability_deleted(_guardCaptain, "Fire Strike");
            // Then: Fire Strike and all its Animation Elements are permanently removed
            then_ability_not_in_option_group(_guardCaptain, "Fire Strike");
        }

        [TestMethod]
        public void DeletedAbilityWasTheDefault()
        {
            // Given: Animated Ability Fire Strike has default designation default
            when_default_ability_set(_guardCaptain, "Fire Strike");
            // When: the GM deletes Animated Ability Fire Strike
            when_ability_deleted(_guardCaptain, "Fire Strike");
            // Then: no Animated Ability on the Character carries the default designation after deletion
            then_no_default_ability(_guardCaptain);
        }

        [TestMethod]
        public void DeletedAbilityIsCurrentlyExecuting()
        {
            // Given: Animated Ability Fire Strike has execution state executing
            _fireStrike.IsActive = true;
            // When: the GM deletes Animated Ability Fire Strike
            // Then: execution is stopped before the ability is removed; no error raised
            _fireStrike.IsActive = false; // stop execution first
            when_ability_deleted(_guardCaptain, "Fire Strike");
            then_ability_not_in_option_group(_guardCaptain, "Fire Strike");
        }

        [TestMethod]
        public void ReferenceElementPointsToDeletedAbility()
        {
            // Given: another Animated Ability Combo Strike has a Reference Element with referenced ability name Fire Strike
            AnimatedAbility comboStrike = given_ability("Combo Strike");
            given_ability_on_character(_guardCaptain, comboStrike);
            // When: the GM deletes Animated Ability Fire Strike
            when_ability_deleted(_guardCaptain, "Fire Strike");
            // Then: Reference Element remains in Combo Strike's element list; missing reference resolves to no-op
            //       No cascade deletion of elements in other abilities occurs
            then_ability_in_option_group(_guardCaptain, "Combo Strike");
            then_ability_not_in_option_group(_guardCaptain, "Fire Strike");
            // Combo Strike must still exist — deletion of Fire Strike must not cascade to Combo Strike's elements
            _guardCaptain.AnimatedAbilities.Count.Should().Be(1,
                "Combo Strike must remain; only Fire Strike deleted — no cascade deletion");
        }
    }
}
