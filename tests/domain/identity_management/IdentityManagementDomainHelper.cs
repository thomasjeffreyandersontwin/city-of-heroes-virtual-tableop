using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace HeroVTT.DomainTests.IdentityManagement
{
    public class IdentityManagementDomainHelper
    {
        protected Character _guardCaptain;

        [TestInitialize]
        public void Init()
        {
            _guardCaptain = new Character("Guard_Captain");
        }

        // Given helpers

        protected Character given_character(string name)
        {
            return new Character(name);
        }

        protected Identity given_identity(string name, IdentityType type)
        {
            return new Identity(name, type, name);
        }

        protected Identity given_model_identity(string name, string modelName)
        {
            return new Identity(modelName, IdentityType.Model, name);
        }

        protected Identity given_costume_identity(string name, string costumeSurface)
        {
            Identity id = new Identity(name, IdentityType.Costume, name);
            id.Surface = costumeSurface;
            return id;
        }

        protected void given_identity_on_character(Character character, Identity identity)
        {
            if (!character.AvailableIdentities.ContainsKey(identity.Name))
                character.AvailableIdentities.Add(identity);
        }

        protected void given_default_identity_on_character(Character character, Identity identity)
        {
            if (!character.AvailableIdentities.ContainsKey(identity.Name))
                character.AvailableIdentities.Add(identity);
            character.DefaultIdentity = identity;
        }

        // When helpers

        protected bool when_identity_added(Character character, string identityName, IdentityType type)
        {
            if (character.AvailableIdentities.ContainsKey(identityName) || string.IsNullOrEmpty(identityName))
                return false;
            Identity id = new Identity(identityName, type, identityName);
            character.AvailableIdentities.Add(id);
            return true;
        }

        protected void when_identity_removed(Character character, string identityName)
        {
            if (character.AvailableIdentities.ContainsKey(identityName))
                character.AvailableIdentities.Remove(identityName);
        }

        protected void when_identity_type_set(Identity identity, IdentityType type)
        {
            identity.Type = type;
        }

        protected void when_costume_surface_assigned(Identity identity, string surface)
        {
            identity.Surface = surface;
        }

        protected void when_default_identity_set(Character character, string identityName)
        {
            if (character.AvailableIdentities.ContainsKey(identityName))
                character.DefaultIdentity = character.AvailableIdentities[identityName];
        }

        protected void when_active_identity_set(Character character, string identityName)
        {
            if (character.AvailableIdentities.ContainsKey(identityName))
                character.ActiveIdentity = character.AvailableIdentities[identityName];
        }

        // Then helpers

        protected void then_identity_in_option_group(Character character, string identityName)
        {
            character.AvailableIdentities.ContainsKey(identityName).Should().BeTrue(
                string.Format("Identity '{0}' must be present in the Identity Option Group", identityName));
        }

        protected void then_identity_not_in_option_group(Character character, string identityName)
        {
            character.AvailableIdentities.ContainsKey(identityName).Should().BeFalse(
                string.Format("Identity '{0}' must not be present after removal", identityName));
        }

        protected void then_identity_count(Character character, int expected)
        {
            character.AvailableIdentities.Count.Should().Be(expected,
                string.Format("Identity Option Group must contain exactly {0} identities", expected));
        }

        protected void then_active_identity(Character character, string identityName)
        {
            character.ActiveIdentity.Should().NotBeNull("an active identity must be set");
            character.ActiveIdentity.Name.Should().Be(identityName,
                string.Format("Active identity must be '{0}'", identityName));
        }

        protected void then_no_active_identity(Character character)
        {
            bool isActive = character.ActiveIdentity != null;
            isActive.Should().BeFalse("no identity should be active in this state");
        }

        protected void then_default_identity(Character character, string identityName)
        {
            character.AvailableIdentities.ContainsKey(identityName).Should().BeTrue();
            character.DefaultIdentity.Should().NotBeNull();
            character.DefaultIdentity.Name.Should().Be(identityName,
                string.Format("'{0}' must be the default identity", identityName));
        }

        protected void then_no_default_identity(Character character)
        {
            character.DefaultIdentity.Should().BeNull("no identity should carry the default designation after clear");
        }

        protected void then_identity_type(Identity identity, IdentityType expected)
        {
            identity.Type.Should().Be(expected,
                string.Format("Identity type must be '{0}'", expected));
        }

        protected void then_costume_surface(Identity identity, string expected)
        {
            identity.Surface.Should().Be(expected,
                string.Format("Costume surface must be '{0}'", expected));
        }

        protected void then_add_rejected(bool addResult)
        {
            addResult.Should().BeFalse("duplicate or empty-name addition must be rejected");
        }
    }
}
