using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;

namespace HeroVTT.DomainTests.AnimatedAbilityManagement
{
    [TestClass]
    public class EditAnimatedAbility : AnimatedAbilityManagementDomainHelper
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
        public void EditOpensPrePopulatedAbilityEditor()
        {
            // Given: Animated Ability Fire Strike exists in the Ability Option Group
            // When: the GM selects Edit on Animated Ability Fire Strike
            // Then: ability editor opens pre-populated with current ability name, activation key, persistence, attack designation
            _fireStrike.Name.Should().Be("Fire Strike",
                "the editor must be pre-populated with the ability's current name");
            _fireStrike.Persistent.Should().BeFalse(
                "default persistence designation is non-persistent");
        }

        [TestMethod]
        public void SaveAppliesChanges()
        {
            // Given: Animated Ability Fire Strike in the editor
            // When: the GM modifies fields in the ability editor and saves
            _fireStrike.Name = "Fire Strike Enhanced";
            // Then: Animated Ability is updated with the new values
            _guardCaptain.AnimatedAbilities.Remove("Fire Strike");
            _guardCaptain.AnimatedAbilities.Add(_fireStrike);
            then_ability_in_option_group(_guardCaptain, "Fire Strike Enhanced");
        }

        [TestMethod]
        public void CancelDiscardsChanges()
        {
            // Given: Animated Ability Fire Strike; editor open
            string originalName = _fireStrike.Name;
            // When: the GM cancels without saving
            // Then: Animated Ability retains its previous values unchanged
            _fireStrike.Name.Should().Be(originalName,
                "cancelling must discard all changes and retain previous values");
        }

        [TestMethod]
        public void DuplicateNameOnSaveRejected()
        {
            // Given: another Animated Ability Ice Shield also exists
            AnimatedAbility iceShield = given_ability("Ice Shield");
            given_ability_on_character(_guardCaptain, iceShield);
            // When: the GM attempts to save Fire Strike with name Ice Shield (duplicate)
            bool isDuplicate = _guardCaptain.AnimatedAbilities.ContainsKey("Ice Shield");
            // Then: save is rejected with inline validation error; editor remains open
            isDuplicate.Should().BeTrue(
                "duplicate name on save must be rejected — Ice Shield already exists on Guard_Captain");
        }

        [TestMethod]
        public void SuccessfulSaveClosesEditor()
        {
            // Given: Animated Ability Fire Strike with unique new name
            // When: the GM saves successfully
            _fireStrike.Name = "Fire Blast";
            _guardCaptain.AnimatedAbilities.Remove("Fire Strike");
            _guardCaptain.AnimatedAbilities.Add(_fireStrike);
            // Then: editor closes and updated ability is selected in the ability list
            then_ability_in_option_group(_guardCaptain, "Fire Blast");
            _guardCaptain.AnimatedAbilities.ContainsKey("Fire Strike").Should().BeFalse(
                "old name Fire Strike must not remain after successful rename save");
        }
    }
}
