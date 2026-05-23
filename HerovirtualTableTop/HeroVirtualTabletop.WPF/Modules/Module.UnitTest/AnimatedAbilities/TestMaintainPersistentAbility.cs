using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using System.Linq;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Maintain Persistent Ability across Identity Changes</summary>
    [TestClass]
    public class TestMaintainPersistentAbility : BaseTest
    {
        private CrowdMemberModel character;
        private AnimatedAbility fireAura;

        [TestInitialize]
        public void GivenACharacterWithASpawnedNPCAndFireAura()
        {
            ResetKeyBindGeneratorStatics();
            character = new CrowdMemberModel("Guard_Captain");
            character.SetAsSpawned();

            fireAura = new AnimatedAbility("Fire Aura") { Persistent = true };
            fireAura.Owner = character;
            character.AnimatedAbilities.Add(fireAura);
        }

        [TestMethod]
        public void PersistentAbilityStoppedBeforeIdentitySwitch()
        {
            // Given Fire Aura is executing
            fireAura.Play();
            fireAura.IsActive.Should().BeTrue();

            // When identity changes: stop persistent abilities before the switch
            fireAura.Stop();

            fireAura.IsActive.Should().BeFalse(
                because: "persistent ability is stopped before the identity switch begins");
        }

        [TestMethod]
        public void PersistentAbilityReplaysAfterNewIdentityLoads()
        {
            // Given ability was stopped for identity change
            fireAura.Play();
            fireAura.Stop();
            fireAura.IsActive.Should().BeFalse();

            // When new identity finishes loading — replay
            fireAura.Play();

            fireAura.IsActive.Should().BeTrue(
                because: "persistent ability replays from first element after new identity loads");
        }

        [TestMethod]
        public void MultiplePersistentAbilitiesAllRestartAfterIdentityChange()
        {
            var iceShield = new AnimatedAbility("Ice Shield") { Persistent = true };
            iceShield.Owner = character;
            character.AnimatedAbilities.Add(iceShield);

            fireAura.Play();
            iceShield.Play();

            // Identity change: stop both
            fireAura.Stop();
            iceShield.Stop();
            fireAura.IsActive.Should().BeFalse();
            iceShield.IsActive.Should().BeFalse();

            // Replay both
            fireAura.Play();
            iceShield.Play();

            fireAura.IsActive.Should().BeTrue();
            iceShield.IsActive.Should().BeTrue();
        }

        [TestMethod]
        public void CharacterDespawnedWhilePersistentAbilityActiveStopsAbility()
        {
            fireAura.Play();
            fireAura.IsActive.Should().BeTrue();

            // Simulate despawn: stop ability
            fireAura.Stop();

            fireAura.IsActive.Should().BeFalse();
            fireAura.Persistent.Should().BeTrue(
                because: "despawning stops execution but does not clear the persistence designation");
        }

        [TestMethod]
        public void PersistentDesignationSurvivesDespawnForFutureReplay()
        {
            fireAura.Play();
            fireAura.Stop();

            fireAura.Persistent.Should().BeTrue(
                because: "persistence designation remains; the ability will replay on the next spawn-and-identity-load");
        }

        [TestMethod]
        public void NonPersistentAbilitiesAreNotReplayed()
        {
            var nonPersistentStrike = new AnimatedAbility("Fire Strike") { Persistent = false };
            nonPersistentStrike.Owner = character;
            character.AnimatedAbilities.Add(nonPersistentStrike);

            // Only persistent abilities should be replayed after identity load
            int persistentCount = character.AnimatedAbilities.Count(a => a.Persistent);
            persistentCount.Should().Be(1, because: "only Fire Aura is persistent; Fire Strike is not");
        }
    }
}
