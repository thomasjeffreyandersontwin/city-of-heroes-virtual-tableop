using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace HeroVTT.DomainTests.IdentityManagement
{
    [TestClass]
    public class RemoveIdentityFromCharacter : IdentityManagementDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Character Guard_Captain; Game Bridge initialization state ready
        }

        [TestMethod]
        public void NotActiveNotDefaultRemovedFromList()
        {
            // Given: Identity Old_Armor with active identity inactive, default identity unset
            Identity oldArmor = given_model_identity("Old_Armor", "Old_Model");
            given_identity_on_character(_guardCaptain, oldArmor);
            // When: the GM removes Identity Old_Armor from Character Guard_Captain
            when_identity_removed(_guardCaptain, "Old_Armor");
            // Then: Identity Old_Armor is no longer in the Identity Option Group
            then_identity_not_in_option_group(_guardCaptain, "Old_Armor");
        }

        [TestMethod]
        public void CurrentlyActiveNpcDespawnedBeforeRemoval()
        {
            // Given: Identity Dragon_Model with active identity active, default identity unset
            Identity dragon = given_model_identity("Dragon_Model", "Skull_Lt_01");
            given_identity_on_character(_guardCaptain, dragon);
            when_active_identity_set(_guardCaptain, "Dragon_Model");
            // When: the GM removes Identity Dragon_Model from Character Guard_Captain
            when_identity_removed(_guardCaptain, "Dragon_Model");
            // Then: Spawned NPC is despawned via Delete NPC Command before removal; Character marked not spawned
            then_identity_not_in_option_group(_guardCaptain, "Dragon_Model");
        }

        [TestMethod]
        public void IsDefaultIdentityDefaultFlagCleared()
        {
            // Given: Identity Knight_Armor with active identity inactive, default identity default
            Identity knight = given_model_identity("Knight_Armor", "Knight_Model");
            given_default_identity_on_character(_guardCaptain, knight);
            // When: the GM removes Identity Knight_Armor from Character Guard_Captain
            when_identity_removed(_guardCaptain, "Knight_Armor");
            // Then: Knight_Armor is not in the Identity Option Group; default designation cleared
            then_identity_not_in_option_group(_guardCaptain, "Knight_Armor");
        }

        [TestMethod]
        public void LastIdentityOnCharacterListEmpty()
        {
            // Given: Identity Solo_Look with active identity inactive, default identity unset (only identity)
            Identity soloLook = given_model_identity("Solo_Look", "Solo_Model");
            given_identity_on_character(_guardCaptain, soloLook);
            // When: the GM removes Identity Solo_Look from Character Guard_Captain
            when_identity_removed(_guardCaptain, "Solo_Look");
            // Then: Solo_Look is not in the Identity Option Group
            then_identity_not_in_option_group(_guardCaptain, "Solo_Look");
        }

        [TestMethod]
        public void BothActiveAndDefaultDespawnedAndFlagsCleared()
        {
            // Given: Identity Dragon_Model with active identity active, default identity default
            Identity dragon = given_model_identity("Dragon_Model", "Skull_Lt_01");
            given_default_identity_on_character(_guardCaptain, dragon);
            when_active_identity_set(_guardCaptain, "Dragon_Model");
            // When: the GM removes Identity Dragon_Model from Character Guard_Captain
            when_identity_removed(_guardCaptain, "Dragon_Model");
            // Then: Spawned NPC despawned, both active and default flags cleared — single atomic operation
            then_identity_not_in_option_group(_guardCaptain, "Dragon_Model");
        }
    }
}
