using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Stop Active Ability</summary>
    [TestClass]
    public class TestStopActiveAbility : BaseTest
    {
        private CrowdMemberModel character;
        private AnimatedAbility fireStrike;

        [TestInitialize]
        public void GivenACharacterWithFireStrikeInTheAbilityOptionGroup()
        {
            ResetKeyBindGeneratorStatics();
            character = new CrowdMemberModel("Guard_Captain");
            fireStrike = new AnimatedAbility("Fire Strike");
            fireStrike.Owner = character;
            character.AnimatedAbilities.Add(fireStrike);
        }

        [TestMethod]
        public void StopHaltsExecutingAbilityAndSetsStateStopped()
        {
            // Given Fire Strike is executing
            fireStrike.Play();
            fireStrike.IsActive.Should().BeTrue();

            // When stopped
            fireStrike.Stop();

            // Then execution state is stopped
            fireStrike.IsActive.Should().BeFalse();
        }

        [TestMethod]
        public void StopWhenNothingExecutingIsNoOp()
        {
            // Given nothing is executing
            fireStrike.IsActive.Should().BeFalse();

            // When stop is called
            System.Action stop = () => fireStrike.Stop();

            // Then no error is raised
            stop.ShouldNotThrow();
            fireStrike.IsActive.Should().BeFalse();
        }

        [TestMethod]
        public void StopPersistentAbilityDoesNotClearPersistenceDesignation()
        {
            // Given Fire Aura is persistent and executing
            var fireAura = new AnimatedAbility("Fire Aura") { Persistent = true };
            fireAura.Owner = character;
            character.AnimatedAbilities.Add(fireAura);
            fireAura.Play();
            fireAura.IsActive.Should().BeTrue();

            // When stopped
            fireAura.Stop();

            // Then execution stops but persistence designation is preserved
            fireAura.IsActive.Should().BeFalse();
            fireAura.Persistent.Should().BeTrue(
                because: "stopping a persistent ability does not clear its persistence designation; it will replay on next identity load");
        }

        [TestMethod]
        public void StopSetsExecutionStateToStoppedFromExecuting()
        {
            fireStrike.Play();
            fireStrike.Stop();

            fireStrike.IsActive.Should().BeFalse(
                because: "execution state transitions from executing to stopped");
        }

        [TestMethod]
        public void StopMidPauseElementCancelsAndCompletesCleanly()
        {
            var longPause = new PauseElement("Long Pause", 1);
            fireStrike.AddAnimationElement(longPause);

            // Start play to set executing
            fireStrike.Play();

            // Then stop immediately
            System.Action stopDuringPause = () => fireStrike.Stop();
            stopDuringPause.ShouldNotThrow(because: "stop completes cleanly regardless of current element state");
            fireStrike.IsActive.Should().BeFalse();
        }
    }
}
