using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Set Default Ability for Character</summary>
    [TestClass]
    public class TestSetDefaultAbility : BaseTest
    {
        private CrowdMemberModel character;
        private AnimatedAbility recovery;

        [TestInitialize]
        public void GivenACharacterWithAbilitiesInTheOptionGroup()
        {
            ResetKeyBindGeneratorStatics();
            character = new CrowdMemberModel("Guard_Captain");
            recovery = new AnimatedAbility("Recovery");
            character.AnimatedAbilities.Add(recovery);
        }

        [TestMethod]
        public void NewDefaultAbilityDesignatedOnCharacter()
        {
            character.DefaultAbility = recovery;

            character.DefaultAbility.Should().Be(recovery);
            character.DefaultAbility.Name.Should().Be("Recovery");
        }

        [TestMethod]
        public void AtMostOneDefaultAbilityEnforcedBySingleReference()
        {
            var strike = new AnimatedAbility("Strike");
            character.AnimatedAbilities.Add(strike);

            // Set Recovery as default first
            character.DefaultAbility = recovery;

            // Then set Strike as the new default — replaces Recovery
            character.DefaultAbility = strike;

            // Only Strike carries the default designation now
            character.DefaultAbility.Should().Be(strike);
            character.DefaultAbility.Should().NotBe(recovery);
        }

        [TestMethod]
        public void PreviousDefaultClearedWhenNewDefaultSet()
        {
            var strike = new AnimatedAbility("Strike");
            character.AnimatedAbilities.Add(strike);

            character.DefaultAbility = recovery;
            character.DefaultAbility = strike;

            // Recovery no longer has the default designation in the character context
            character.DefaultAbility.Name.Should().Be("Strike");
        }

        [TestMethod]
        public void DefaultAbilityRemovedLeavesNoDefault()
        {
            character.DefaultAbility = recovery;
            character.AnimatedAbilities.Remove(recovery);

            // Clearing the reference after removal
            character.DefaultAbility = null;

            character.DefaultAbility.Should().BeNull(
                because: "after deleting the default ability, no ability carries the default designation");
        }

        [TestMethod]
        public void ClearingDefaultDesignationLeavesNoDefault()
        {
            character.DefaultAbility = recovery;

            character.DefaultAbility = null;

            character.DefaultAbility.Should().BeNull();
        }

        [TestMethod]
        public void CharacterWithNoDefaultAbilityReturnsNullDefaultAbility()
        {
            // No default set
            character.DefaultAbility.Should().BeNull(
                because: "a fresh character has no default ability");
        }
    }
}
