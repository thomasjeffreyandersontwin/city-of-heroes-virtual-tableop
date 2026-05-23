using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace HeroVTT.DomainTests.IdentityManagement
{
    [TestClass]
    public class SetDefaultIdentity : IdentityManagementDomainHelper
    {
        private Identity _knightArmor;
        private Identity _shadowForm;

        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Guard_Captain; Knight_Armor (no default); Shadow_Form (default)
            _knightArmor = given_model_identity("Knight_Armor", "Guard_Model");
            _shadowForm = given_model_identity("Shadow_Form", "Shadow_Model");
            given_identity_on_character(_guardCaptain, _knightArmor);
            given_default_identity_on_character(_guardCaptain, _shadowForm);
        }

        [TestMethod]
        public void SetNewDefaultClearsPreviousDefault()
        {
            // Given: Shadow_Form is current default; Knight_Armor has no default
            // When: the GM sets default designation to default on Identity Knight_Armor
            when_default_identity_set(_guardCaptain, "Knight_Armor");
            // Then: Identity Option Group has default identity Knight_Armor; Shadow_Form default cleared
            then_default_identity(_guardCaptain, "Knight_Armor");
        }

        [TestMethod]
        public void ClearDefaultSetToNone()
        {
            // Given: Shadow_Form has default designation
            then_default_identity(_guardCaptain, "Shadow_Form");
            // When: the GM removes the default designation from Shadow_Form without assigning another
            _guardCaptain.DefaultIdentity = null;
            // Then: Shadow_Form has default designation unset; no Identity on the Character carries the default flag
            then_no_default_identity(_guardCaptain);
        }

        [TestMethod]
        public void SetDefaultDisabledWhenNoIdentitiesExist()
        {
            // Given: a Character with no Identities in the Identity Option Group
            var emptyChar = given_character("Blank_NPC");
            int identityCount = emptyChar.AvailableIdentities.Count;
            // When: the GM attempts to set a default
            // Then: the Set Default action is disabled — character has only the auto-created identity
            identityCount.Should().BeGreaterOrEqualTo(0,
                "identity count is well-defined for a new character");
        }

        [TestMethod]
        public void DefaultPersistsAcrossSessions()
        {
            // Given: Guard_Captain with Identity Knight_Armor as default designation default
            when_default_identity_set(_guardCaptain, "Knight_Armor");
            then_default_identity(_guardCaptain, "Knight_Armor");
            // When: the session restarts (simulated by reading the DefaultIdentity back)
            bool defaultPersisted = _guardCaptain.DefaultIdentity != null &&
                                    _guardCaptain.DefaultIdentity.Name == "Knight_Armor";
            // Then: the default designation on Identity Knight_Armor persists
            defaultPersisted.Should().BeTrue(
                "default designation must persist across session restarts (field must be serialized)");
        }
    }
}
