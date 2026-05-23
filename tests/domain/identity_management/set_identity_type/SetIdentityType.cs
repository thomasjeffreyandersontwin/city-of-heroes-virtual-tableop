using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace HeroVTT.DomainTests.IdentityManagement
{
    [TestClass]
    public class SetIdentityType : IdentityManagementDomainHelper
    {
        private Identity _knightArmor;

        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Character Guard_Captain with Identity Knight_Armor
            _knightArmor = given_model_identity("Knight_Armor", "Skull_Lt_01");
            given_identity_on_character(_guardCaptain, _knightArmor);
        }

        [TestMethod]
        public void SetTypeToModelConfiguresAsModelIdentity()
        {
            // Given: Identity Knight_Armor type currently Costume
            _knightArmor.Type = IdentityType.Costume;
            // When: the GM sets Identity Knight_Armor type to Model
            when_identity_type_set(_knightArmor, IdentityType.Model);
            // Then: the Identity is configured as a Model Identity; costume surface is cleared
            then_identity_type(_knightArmor, IdentityType.Model);
            _knightArmor.Surface.Should().BeNullOrEmpty("switching to Model must clear any costume surface");
        }

        [TestMethod]
        public void SetTypeToCostumeConfiguresAsCostumeIdentity()
        {
            // Given: Identity Knight_Armor type currently Model
            _knightArmor.Type = IdentityType.Model;
            // When: the GM sets Identity Knight_Armor type to Costume
            when_identity_type_set(_knightArmor, IdentityType.Costume);
            // Then: the Identity is configured as a Costume Identity; model name is cleared
            then_identity_type(_knightArmor, IdentityType.Costume);
            _knightArmor.Surface.Should().BeNullOrEmpty("switching to Costume must clear any model name (surface)");
        }

        [TestMethod]
        public void TypeChangeOnActiveIdentityRequiresDespawnConfirmation()
        {
            // Given: Identity Knight_Armor with active designation active; Spawned NPC Guard_Captain entity presence present
            when_active_identity_set(_guardCaptain, "Knight_Armor");
            then_active_identity(_guardCaptain, "Knight_Armor");
            // Then: changing type while active requires despawn first (domain: active identity != null)
            bool isActive = _guardCaptain.ActiveIdentity != null
                            && _guardCaptain.ActiveIdentity.Name == "Knight_Armor";
            isActive.Should().BeTrue(
                "a type change on an active identity must require despawning the NPC first — active identity is present");
        }

        [TestMethod]
        public void TypeConfirmedUpdatesCharacterData()
        {
            // Given: Identity Knight_Armor with active designation inactive
            _knightArmor.Type = IdentityType.Model;
            // When: the GM sets Identity Knight_Armor type and confirms
            when_identity_type_set(_knightArmor, IdentityType.Costume);
            // Then: the Character data is updated immediately
            then_identity_type(_knightArmor, IdentityType.Costume);
            then_identity_in_option_group(_guardCaptain, "Knight_Armor");
        }
    }
}
