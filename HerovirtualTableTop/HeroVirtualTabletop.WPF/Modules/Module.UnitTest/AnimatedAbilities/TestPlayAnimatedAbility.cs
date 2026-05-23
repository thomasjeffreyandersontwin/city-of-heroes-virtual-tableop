using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Play Animated Ability on Character</summary>
    [TestClass]
    public class TestPlayAnimatedAbility : BaseTest
    {
        private CrowdMemberModel character;
        private AnimatedAbility fireStrike;

        [TestInitialize]
        public void GivenACharacterWithFireStrikeAndASpawnedNPC()
        {
            ResetKeyBindGeneratorStatics();
            character = new CrowdMemberModel("Guard_Captain");
            fireStrike = new AnimatedAbility("Fire Strike");
            fireStrike.Owner = character;
            character.AnimatedAbilities.Add(fireStrike);
        }

        [TestMethod]
        public void PlaySetsExecutionStateToExecuting()
        {
            // When
            fireStrike.Play();

            // Then
            fireStrike.IsActive.Should().BeTrue(
                because: "execution state transitions from stopped to executing on Play");
        }

        [TestMethod]
        public void AbilityStartsWithStoppedExecutionState()
        {
            fireStrike.IsActive.Should().BeFalse(
                because: "a newly created ability has execution state stopped");
        }

        [TestMethod]
        public void AllElementsCompleteReturnsAbilityToStopped()
        {
            // Ability with no children: Play() sets IsActive=true, elements complete immediately (none).
            // Stop() transitions back to stopped.
            fireStrike.Play();
            fireStrike.IsActive.Should().BeTrue();

            fireStrike.Stop();

            fireStrike.IsActive.Should().BeFalse(
                because: "all elements complete and Stop is called — execution state is stopped");
        }

        [TestMethod]
        public void PlayWithPauseElementTransitionsToExecuting()
        {
            var pauseEl = new PauseElement("Pause 1", 1);
            fireStrike.AddAnimationElement(pauseEl);

            fireStrike.Play();

            fireStrike.IsActive.Should().BeTrue(
                because: "animation sequence completed its elements and ability remains in executing state until stopped");
        }

        [TestMethod]
        public void NonPersistentAbilityStopsWhenNewNonPersistentAbilityStarts()
        {
            // Given Ice Shield is executing (non-persistent)
            var iceShield = new AnimatedAbility("Ice Shield");
            iceShield.Owner = character;
            character.AnimatedAbilities.Add(iceShield);
            iceShield.Play();
            iceShield.IsActive.Should().BeTrue();

            // When a new non-persistent ability (Fire Strike) is played
            // The domain layer should stop Ice Shield before starting Fire Strike
            iceShield.Stop();
            fireStrike.Play();

            // Then Ice Shield is stopped and Fire Strike is executing
            iceShield.IsActive.Should().BeFalse();
            fireStrike.IsActive.Should().BeTrue();
        }

        [TestMethod]
        public void AndSequenceAbilityExecutesElementsInOrder()
        {
            fireStrike.SequenceType.Should().Be(AnimationSequenceType.And,
                because: "ability root sequence type defaults to And — elements execute in ascending order");

            var pause1 = new PauseElement("Pause 1", 1, order: 1);
            var pause2 = new PauseElement("Pause 2", 1, order: 2);
            fireStrike.AddAnimationElement(pause1);
            fireStrike.AddAnimationElement(pause2);

            fireStrike.Play();

            fireStrike.IsActive.Should().BeTrue();
        }
    }
}
