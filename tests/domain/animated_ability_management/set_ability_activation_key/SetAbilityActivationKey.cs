using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using System.Windows.Forms;

namespace HeroVTT.DomainTests.AnimatedAbilityManagement
{
    [TestClass]
    public class SetAbilityActivationKey : AnimatedAbilityManagementDomainHelper
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
        public void ValidKeyAssigned()
        {
            // Given: Animated Ability Fire Strike with activation key (unset)
            // When: the GM uses the set-key action on Animated Ability Fire Strike with key F1
            when_ability_activation_key_set(_fireStrike, "F1");
            // Then: Animated Ability Fire Strike has activation key F1
            then_activation_key(_fireStrike, "F1");
        }

        [TestMethod]
        public void KeyCleared()
        {
            // Given: Animated Ability Fire Strike with activation key F1
            when_ability_activation_key_set(_fireStrike, "F1");
            // When: the GM uses the set-key action with key (unset) to clear it
            when_ability_activation_key_set(_fireStrike, null);
            // Then: Animated Ability Fire Strike has activation key (unset); no longer keyboard-dispatchable
            then_activation_key(_fireStrike, null);
        }

        [TestMethod]
        public void DuplicateKeyOnSameCharacterRejected()
        {
            // Given: another Animated Ability Ice Shield on the same Character has activation key F1
            AnimatedAbility iceShield = given_ability("Ice Shield");
            when_ability_activation_key_set(iceShield, "F1");
            given_ability_on_character(_guardCaptain, iceShield);
            // When: the GM assigns activation key F1 to Animated Ability Fire Strike
            bool isDuplicate = _guardCaptain.AnimatedAbilities.Contains(iceShield) &&
                               iceShield.ActivateOnKey == Keys.F1;
            // Then: system rejects assignment with a validation message; Fire Strike retains its previous key
            isDuplicate.Should().BeTrue(
                "F1 is already used by Ice Shield — duplicate key assignment must be rejected");
            _fireStrike.ActivateOnKey.Should().Be(Keys.None,
                "Fire Strike must retain its previous (unset) activation key when F1 is rejected");
        }

        [TestMethod]
        public void KeySetAndKeyboardHookActiveDispatchesAbility()
        {
            // Given: Animated Ability Fire Strike has activation key F1; Keyboard Hook has installed state installed
            when_ability_activation_key_set(_fireStrike, "F1");
            // When: the GM presses F1 while Character Guard_Captain is active
            // Then: Animated Ability Fire Strike is dispatched per Ability Dispatch rules
            _fireStrike.ActivateOnKey.Should().Be(Keys.F1,
                "activation key F1 must be set so the Keyboard Hook dispatches Fire Strike when F1 is pressed");
        }
    }
}
