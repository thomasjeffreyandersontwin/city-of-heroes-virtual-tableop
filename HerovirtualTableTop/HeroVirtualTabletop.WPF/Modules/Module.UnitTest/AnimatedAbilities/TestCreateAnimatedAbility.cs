using FluentAssertions;
using Framework.WPF.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Enumerations;
using System.Windows.Forms;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Create Animated Ability</summary>
    [TestClass]
    public class TestCreateAnimatedAbility : BaseTest
    {
        private CrowdMemberModel character;

        [TestInitialize]
        public void GivenACharacterNamedGuardCaptainSelectedInTheCrowdTree()
        {
            ResetKeyBindGeneratorStatics();
            character = new CrowdMemberModel("Guard_Captain");
        }

        [TestMethod]
        public void NewAbilityCreatedWithExpectedDefaultValues()
        {
            // When
            var ability = new AnimatedAbility("Fire Strike");
            character.AnimatedAbilities.Add(ability);

            // Then
            var stored = character.AnimatedAbilities["Fire Strike"];
            stored.Should().NotBeNull();
            stored.ActivateOnKey.Should().Be(Keys.None);
            stored.Persistent.Should().BeFalse();
            stored.IsActive.Should().BeFalse();
            stored.IsAttack.Should().BeFalse();
            stored.AnimationElements.Should().BeEmpty();
        }

        [TestMethod]
        public void NewAbilityAppearsInAbilityOptionGroup()
        {
            var ability = new AnimatedAbility("Fire Strike");

            character.AnimatedAbilities.Add(ability);

            character.AnimatedAbilities.ContainsKey("Fire Strike").Should().BeTrue();
            character.AnimatedAbilities.Count.Should().Be(1);
        }

        [TestMethod]
        public void DuplicateAbilityNameRejected()
        {
            // Given an ability named Fire Strike already exists
            character.AnimatedAbilities.Add(new AnimatedAbility("Fire Strike"));

            // When the GM attempts to create another with the same name
            System.Action addDuplicate = () =>
                character.AnimatedAbilities.Add(new AnimatedAbility("Fire Strike"));

            // Then the system rejects the creation
            addDuplicate.ShouldThrow<DuplicateKeyException>();
            character.AnimatedAbilities.Count.Should().Be(1);
        }

        [TestMethod]
        public void MultipleAbilitiesWithDistinctNamesAllAccepted()
        {
            character.AnimatedAbilities.Add(new AnimatedAbility("Fire Strike"));
            character.AnimatedAbilities.Add(new AnimatedAbility("Ice Shield"));
            character.AnimatedAbilities.Add(new AnimatedAbility("Recovery"));

            character.AnimatedAbilities.Count.Should().Be(3);
            character.AnimatedAbilities.ContainsKey("Fire Strike").Should().BeTrue();
            character.AnimatedAbilities.ContainsKey("Ice Shield").Should().BeTrue();
            character.AnimatedAbilities.ContainsKey("Recovery").Should().BeTrue();
        }

        [TestMethod]
        public void NewAbilityDefaultExecutionStateIsStopped()
        {
            var ability = new AnimatedAbility("Fire Strike");

            ability.IsActive.Should().BeFalse(
                because: "execution state starts as stopped (IsActive=false)");
        }
    }
}
