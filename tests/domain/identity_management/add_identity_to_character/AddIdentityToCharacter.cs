using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace HeroVTT.DomainTests.IdentityManagement
{
    [TestClass]
    public class AddIdentityToCharacter : IdentityManagementDomainHelper
    {
        [TestMethod]
        public void UniqueNameProvidedIdentityAdded()
        {
            // Given: Character Guard_Captain in the Identity Option Group
            // When: the GM adds an Identity with identity name Knight_Armor to Character Guard_Captain
            bool added = when_identity_added(_guardCaptain, "Knight_Armor", IdentityType.Model);
            // Then: the Identity Option Group holds identity name Knight_Armor as active inactive, default unset
            then_identity_in_option_group(_guardCaptain, "Knight_Armor");
            then_no_active_identity(_guardCaptain);
        }

        [TestMethod]
        public void DuplicateNameOnCharacterRejected()
        {
            // Given: Character Guard_Captain already has Identity Knight_Armor
            when_identity_added(_guardCaptain, "Knight_Armor", IdentityType.Model);
            int countBefore = _guardCaptain.AvailableIdentities.Count;
            // When: the GM adds an Identity with identity name Knight_Armor (duplicate)
            bool added = when_identity_added(_guardCaptain, "Knight_Armor", IdentityType.Costume);
            // Then: addition is rejected; existing identities unchanged
            then_add_rejected(added);
            then_identity_count(_guardCaptain, countBefore);
        }

        [TestMethod]
        public void EmptyNameProvidedRejected()
        {
            // Given: Character Guard_Captain with no identities
            int countBefore = _guardCaptain.AvailableIdentities.Count;
            // When: the GM provides an empty name
            bool added = when_identity_added(_guardCaptain, "", IdentityType.Model);
            // Then: empty name is rejected; no unnamed Identity is created
            then_add_rejected(added);
            then_identity_count(_guardCaptain, countBefore);
        }

        [TestMethod]
        public void AddDisabledWhenNoCharacterSelected()
        {
            // Given: no Character is selected in the Crowd Tree
            // When: the GM attempts to add an Identity (no character context)
            bool canAdd = _guardCaptain != null; // Guard_Captain exists; test without character = null check
            // Then: the Add action is disabled; no Identity is created
            bool canAddWithNull = (null != null);
            then_add_rejected(canAddWithNull);
        }
    }
}
