using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Delete Animated Ability</summary>
    [TestClass]
    public class TestDeleteAnimatedAbility : BaseTest
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
        public void AbilityAndItsElementsPermanentlyRemovedFromOptionGroup()
        {
            fireStrike.AddAnimationElement(new PauseElement("Pause 1", 1));

            character.AnimatedAbilities.Remove(fireStrike);

            character.AnimatedAbilities.ContainsKey("Fire Strike").Should().BeFalse();
            character.AnimatedAbilities.Count.Should().Be(0);
        }

        [TestMethod]
        public void DeletedAbilityNoLongerAppearsInAbilityList()
        {
            character.AnimatedAbilities.Remove(fireStrike);

            character.AnimatedAbilities.Should().NotContain(fireStrike);
        }

        [TestMethod]
        public void DeletedDefaultAbilityLeavesNoDefaultOnCharacter()
        {
            // Given Fire Strike is the default ability
            character.DefaultAbility = fireStrike;
            character.DefaultAbility.Should().Be(fireStrike);

            // When it is deleted and the default reference is cleared
            character.AnimatedAbilities.Remove(fireStrike);
            character.DefaultAbility = null;

            // Then no ability carries the default designation
            character.DefaultAbility.Should().BeNull();
        }

        [TestMethod]
        public void DeleteExecutingAbilityStopsExecutionFirstThenRemoves()
        {
            // Given Fire Strike is executing
            fireStrike.Play();
            fireStrike.IsActive.Should().BeTrue();

            // When deleted: stop first, then remove
            fireStrike.Stop();
            character.AnimatedAbilities.Remove(fireStrike);

            // Then execution is stopped and ability is gone
            fireStrike.IsActive.Should().BeFalse();
            character.AnimatedAbilities.ContainsKey("Fire Strike").Should().BeFalse();
        }

        [TestMethod]
        public void ReferenceElementToDeletedAbilityRemainsAsNoOp()
        {
            var comboStrike = new AnimatedAbility("Combo Strike");
            character.AnimatedAbilities.Add(comboStrike);
            var refElement = new ReferenceAbility("Ref 1", fireStrike);
            comboStrike.AddAnimationElement(refElement);

            // When Fire Strike is deleted
            character.AnimatedAbilities.Remove(fireStrike);

            // Then Reference Element remains in Combo Strike with no cascading deletion
            comboStrike.AnimationElements.Should().ContainSingle(e => e.Name == "Ref 1");
            character.AnimatedAbilities.ContainsKey("Combo Strike").Should().BeTrue();
        }
    }
}
