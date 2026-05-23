using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using System.Windows.Forms;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Refresh Ability Activation Eligibility</summary>
    [TestClass]
    public class TestAbilityActivationEligibility : BaseTest
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

        private static bool IsEligible(AnimatedAbility ability, bool characterSpawned)
        {
            bool hasKey = ability.ActivateOnKey != Keys.None;
            bool notExecuting = !ability.IsActive;
            return hasKey && notExecuting && characterSpawned;
        }

        [TestMethod]
        public void EligibleWhenKeyAssignedNotExecutingAndCharacterSpawned()
        {
            fireStrike.ActivateOnKey = Keys.F1;

            bool eligible = IsEligible(fireStrike, characterSpawned: true);

            eligible.Should().BeTrue(
                because: "activation key assigned + not executing + character spawned = eligible");
        }

        [TestMethod]
        public void IneligibleWhenNoActivationKeyAssigned()
        {
            // Default: no key
            bool eligible = IsEligible(fireStrike, characterSpawned: true);

            eligible.Should().BeFalse(
                because: "ability with no activation key is ineligible for keyboard dispatch");
        }

        [TestMethod]
        public void IneligibleWhenAbilityCurrentlyExecuting()
        {
            fireStrike.ActivateOnKey = Keys.F1;
            fireStrike.Play();
            fireStrike.IsActive.Should().BeTrue();

            bool eligible = IsEligible(fireStrike, characterSpawned: true);

            eligible.Should().BeFalse(
                because: "an executing ability is ineligible for dispatch");
        }

        [TestMethod]
        public void IneligibleWhenCharacterNotSpawned()
        {
            fireStrike.ActivateOnKey = Keys.F1;
            // character.HasBeenSpawned is false by default

            bool eligible = IsEligible(fireStrike, characterSpawned: character.HasBeenSpawned);

            eligible.Should().BeFalse(
                because: "a non-spawned character's abilities are ineligible");
        }

        [TestMethod]
        public void EligibilityRefreshesWhenCharacterBecomeSpawned()
        {
            fireStrike.ActivateOnKey = Keys.F1;

            // Before spawn
            bool beforeSpawn = IsEligible(fireStrike, characterSpawned: character.HasBeenSpawned);
            beforeSpawn.Should().BeFalse();

            // After spawn (simulate with SetAsSpawned)
            character.SetAsSpawned();
            bool afterSpawn = IsEligible(fireStrike, characterSpawned: character.HasBeenSpawned);

            afterSpawn.Should().BeTrue(
                because: "eligibility refreshes to eligible once the character is spawned");
        }

        [TestMethod]
        public void EligibilityRefreshesToEligibleAfterAbilityCompletes()
        {
            fireStrike.ActivateOnKey = Keys.F1;
            character.SetAsSpawned();

            fireStrike.Play();
            IsEligible(fireStrike, characterSpawned: true).Should().BeFalse(
                because: "while executing, ability is ineligible for re-dispatch");

            fireStrike.Stop();
            IsEligible(fireStrike, characterSpawned: true).Should().BeTrue(
                because: "after execution stops, the ability is eligible for the next key press");
        }
    }
}
