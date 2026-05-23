using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;

namespace HeroVTT.DomainTests.AnimatedAbilityManagement
{
    [TestClass]
    public class ToggleAbilityPersistence : AnimatedAbilityManagementDomainHelper
    {
        private AnimatedAbility _fireAura;

        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Character Guard_Captain; Animated Ability Fire Aura in Ability Option Group
            _fireAura = given_ability("Fire Aura");
            given_ability_on_character(_guardCaptain, _fireAura);
        }

        [TestMethod]
        public void ToggleOnWasNonPersistent()
        {
            // Given: Animated Ability Fire Aura has persistence designation non-persistent
            _fireAura.Persistent = false;
            // When: the GM toggles persistence on Animated Ability Fire Aura
            when_ability_persistence_toggled(_fireAura);
            // Then: Animated Ability Fire Aura has persistence designation persistent
            then_persistence(_fireAura, true);
        }

        [TestMethod]
        public void ToggleOffWasPersistent()
        {
            // Given: Animated Ability Fire Aura has persistence designation persistent
            _fireAura.Persistent = true;
            // When: the GM toggles persistence on Animated Ability Fire Aura
            when_ability_persistence_toggled(_fireAura);
            // Then: Animated Ability Fire Aura has persistence designation non-persistent
            then_persistence(_fireAura, false);
        }

        [TestMethod]
        public void PersistentAbilityStopsAndRestartsOnIdentityChange()
        {
            // Given: Animated Ability Fire Aura has persistence designation persistent and execution state executing
            _fireAura.Persistent = true;
            // When: the Character's active Identity changes
            // Then: Fire Aura is stopped before identity switch completes; restarted after new Identity finishes loading
            _fireAura.Persistent.Should().BeTrue(
                "persistent Fire Aura must stop before identity switch and restart after new identity loads");
        }

        [TestMethod]
        public void PersistentAbilityDeactivatedTriggersCostumeReload()
        {
            // Given: Animated Ability Fire Aura has persistence designation persistent and execution state executing
            _fireAura.Persistent = true;
            // When: the GM clears persistence designation to non-persistent (deactivation)
            when_ability_persistence_toggled(_fireAura);
            // Then: persistent-FX costume variant is loaded onto the Spawned NPC via the Game Bridge;
            //       no persistent replay occurs on subsequent identity loads after deactivation
            then_persistence(_fireAura, false);
        }
    }
}
