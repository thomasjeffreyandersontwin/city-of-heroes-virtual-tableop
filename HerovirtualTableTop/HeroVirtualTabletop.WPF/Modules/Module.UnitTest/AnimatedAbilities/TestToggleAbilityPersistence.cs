using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Toggle Ability Persistence</summary>
    [TestClass]
    public class TestToggleAbilityPersistence : BaseTest
    {
        private CrowdMemberModel character;
        private AnimatedAbility fireAura;

        [TestInitialize]
        public void GivenACharacterWithFireAuraInTheAbilityOptionGroup()
        {
            ResetKeyBindGeneratorStatics();
            character = new CrowdMemberModel("Guard_Captain");
            fireAura = new AnimatedAbility("Fire Aura");
            character.AnimatedAbilities.Add(fireAura);
        }

        [TestMethod]
        public void NewAbilityDefaultsToNonPersistent()
        {
            fireAura.Persistent.Should().BeFalse(
                because: "persistence designation defaults to non-persistent");
        }

        [TestMethod]
        public void ToggleOnSetsAbilityToPersistent()
        {
            fireAura.Persistent = true;

            fireAura.Persistent.Should().BeTrue(
                because: "toggle on moves persistence designation to persistent");
        }

        [TestMethod]
        public void ToggleOffSetsAbilityToNonPersistent()
        {
            // Given Fire Aura is persistent
            fireAura.Persistent = true;

            // When toggled off
            fireAura.Persistent = false;

            // Then persistence designation is non-persistent
            fireAura.Persistent.Should().BeFalse();
        }

        [TestMethod]
        public void PersistentAbilityIndicatedByFlag()
        {
            fireAura.Persistent = true;

            character.AnimatedAbilities["Fire Aura"].Persistent.Should().BeTrue();
        }

        [TestMethod]
        public void PersistentAbilityStoppedOnIdentityChange_FlagRemainsAfterStop()
        {
            // Given Fire Aura is persistent and executing
            fireAura.Persistent = true;
            fireAura.Play();
            fireAura.IsActive.Should().BeTrue();

            // When stopped (simulating identity-change stop)
            fireAura.Stop();

            // Then execution stops but persistence designation is unchanged
            fireAura.IsActive.Should().BeFalse();
            fireAura.Persistent.Should().BeTrue(
                because: "stopping a persistent ability does not clear its persistence designation");
        }

        [TestMethod]
        public void MultipleAbilitiesCanHaveIndependentPersistenceFlags()
        {
            fireAura.Persistent = true;
            var iceShield = new AnimatedAbility("Ice Shield");
            character.AnimatedAbilities.Add(iceShield);

            int persistentCount = 0;
            foreach (var a in character.AnimatedAbilities)
                if (a.Persistent) persistentCount++;

            persistentCount.Should().Be(1, because: "only Fire Aura has persistence designation");
            iceShield.Persistent.Should().BeFalse();
        }
    }
}
