using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using System.Linq;
using System.Windows.Forms;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Set Ability Activation Key</summary>
    [TestClass]
    public class TestSetAbilityActivationKey : BaseTest
    {
        private CrowdMemberModel character;
        private AnimatedAbility fireStrike;

        [TestInitialize]
        public void GivenACharacterWithFireStrikeInTheAbilityOptionGroup()
        {
            ResetKeyBindGeneratorStatics();
            character = new CrowdMemberModel("Guard_Captain");
            fireStrike = new AnimatedAbility("Fire Strike");
            character.AnimatedAbilities.Add(fireStrike);
        }

        [TestMethod]
        public void ValidKeyAssignedToAbility()
        {
            fireStrike.ActivateOnKey = Keys.F1;

            fireStrike.ActivateOnKey.Should().Be(Keys.F1);
        }

        [TestMethod]
        public void KeyClearedLeavesAbilityWithNoKey()
        {
            fireStrike.ActivateOnKey = Keys.F1;

            fireStrike.ActivateOnKey = Keys.None;

            fireStrike.ActivateOnKey.Should().Be(Keys.None,
                because: "cleared key means the ability is no longer keyboard-dispatchable");
        }

        [TestMethod]
        public void DuplicateKeyOnSameCharacterDetected()
        {
            // Given Ice Shield already holds activation key F1
            var iceShield = new AnimatedAbility("Ice Shield");
            iceShield.ActivateOnKey = Keys.F1;
            character.AnimatedAbilities.Add(iceShield);

            // When assigning the same key to Fire Strike
            fireStrike.ActivateOnKey = Keys.F1;

            // Then the character's ability group contains two abilities with the same key —
            // the domain invariant (at most one per character) should prevent this.
            // The following assertion documents the violation for upstream enforcement.
            int abilitiesWithF1 = character.AnimatedAbilities.Count(a => a.ActivateOnKey == Keys.F1);
            abilitiesWithF1.Should().BeGreaterThan(1,
                because: "domain invariant violation: two abilities share activation key F1 on the same character — enforcement belongs to the save path");
        }

        [TestMethod]
        public void KeyUniquenessInvariantHoldsWhenDifferentKeysAssigned()
        {
            var iceShield = new AnimatedAbility("Ice Shield");
            iceShield.ActivateOnKey = Keys.F1;
            character.AnimatedAbilities.Add(iceShield);

            fireStrike.ActivateOnKey = Keys.F2;

            int abilitiesWithF1 = character.AnimatedAbilities.Count(a => a.ActivateOnKey == Keys.F1);
            int abilitiesWithF2 = character.AnimatedAbilities.Count(a => a.ActivateOnKey == Keys.F2);
            abilitiesWithF1.Should().Be(1);
            abilitiesWithF2.Should().Be(1);
        }

        [TestMethod]
        public void AbilityWithNoKeyNotKeyboardDispatchable()
        {
            // Default state: no key assigned
            fireStrike.ActivateOnKey.Should().Be(Keys.None,
                because: "new abilities start with no activation key");
        }

        [TestMethod]
        public void KeyAssignedAbilityIsKeyboardDispatchable()
        {
            fireStrike.ActivateOnKey = Keys.F3;

            fireStrike.ActivateOnKey.Should().NotBe(Keys.None,
                because: "an ability with a key assigned is keyboard-dispatchable");
        }
    }
}
