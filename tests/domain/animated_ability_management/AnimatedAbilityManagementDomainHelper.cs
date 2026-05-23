using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using System.Windows.Forms;

namespace HeroVTT.DomainTests.AnimatedAbilityManagement
{
    public class AnimatedAbilityManagementDomainHelper
    {
        protected Character _guardCaptain;

        [TestInitialize]
        public void Init()
        {
            _guardCaptain = new Character("Guard_Captain");
        }

        // Given helpers

        protected AnimatedAbility given_ability(string name)
        {
            return new AnimatedAbility(name, Keys.None, AnimationSequenceType.And, false, 1, _guardCaptain);
        }

        protected void given_ability_on_character(Character character, AnimatedAbility ability)
        {
            if (!character.AnimatedAbilities.ContainsKey(ability.Name))
                character.AnimatedAbilities.Add(ability);
        }

        // When helpers

        protected bool when_ability_created(Character character, string abilityName)
        {
            if (character.AnimatedAbilities.ContainsKey(abilityName)) return false;
            AnimatedAbility ability = new AnimatedAbility(abilityName, Keys.None, AnimationSequenceType.And, false, 1, character);
            character.AnimatedAbilities.Add(ability);
            return true;
        }

        protected void when_ability_deleted(Character character, string abilityName)
        {
            if (character.AnimatedAbilities.ContainsKey(abilityName))
                character.AnimatedAbilities.Remove(abilityName);
        }

        protected void when_ability_activation_key_set(AnimatedAbility ability, string key)
        {
            if (string.IsNullOrEmpty(key))
                ability.ActivateOnKey = Keys.None;
            else
                ability.ActivateOnKey = (Keys)System.Enum.Parse(typeof(Keys), key);
        }

        protected void when_ability_persistence_toggled(AnimatedAbility ability)
        {
            ability.Persistent = !ability.Persistent;
        }

        protected void when_default_ability_set(Character character, string abilityName)
        {
            if (character.AnimatedAbilities.ContainsKey(abilityName))
                character.DefaultAbility = character.AnimatedAbilities[abilityName];
        }

        // Then helpers

        protected void then_ability_in_option_group(Character character, string abilityName)
        {
            character.AnimatedAbilities.ContainsKey(abilityName).Should().BeTrue(
                string.Format("Ability '{0}' must be present in the Ability Option Group", abilityName));
        }

        protected void then_ability_not_in_option_group(Character character, string abilityName)
        {
            character.AnimatedAbilities.ContainsKey(abilityName).Should().BeFalse(
                string.Format("Ability '{0}' must not be present after deletion", abilityName));
        }

        protected void then_ability_count(Character character, int expected)
        {
            character.AnimatedAbilities.Count.Should().Be(expected,
                string.Format("Ability Option Group must have exactly {0} abilities", expected));
        }

        protected void then_creation_rejected(bool created)
        {
            created.Should().BeFalse("duplicate or invalid name must be rejected");
        }

        protected void then_activation_key(AnimatedAbility ability, string expected)
        {
            if (expected == null)
                ability.ActivateOnKey.Should().Be(Keys.None,
                    "activation key must be cleared (None) when unset");
            else
                ability.ActivateOnKey.ToString().Should().Be(expected,
                    string.Format("Activation key must be '{0}'", expected));
        }

        protected void then_persistence(AnimatedAbility ability, bool expected)
        {
            ability.Persistent.Should().Be(expected,
                string.Format("Persistence must be {0}", expected));
        }

        protected void then_default_ability(Character character, string abilityName)
        {
            character.AnimatedAbilities.ContainsKey(abilityName).Should().BeTrue();
            character.DefaultAbility.Should().NotBeNull();
            character.DefaultAbility.Name.Should().Be(abilityName,
                string.Format("'{0}' must carry the default designation", abilityName));
        }

        protected void then_no_default_ability(Character character)
        {
            character.DefaultAbility.Should().BeNull("no ability should carry the default designation after clear");
        }
    }
}
