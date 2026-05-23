using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Movements;
using System.Windows.Forms;

namespace HeroVTT.DomainTests.CharacterMovementAuthoring
{
    public class CharacterMovementAuthoringDomainHelper
    {
        protected Character _character;

        [TestInitialize]
        public void Init()
        {
            _character = new Character("Guard_Captain");
        }

        // Given helpers

        protected CharacterMovement given_movement(string name)
        {
            return new CharacterMovement(name, _character);
        }

        protected void given_movement_on_character(Character character, CharacterMovement movement)
        {
            if (!character.Movements.ContainsKey(movement.Name))
                character.Movements.Add(movement);
        }

        // When helpers

        protected bool when_movement_added(Character character, string name)
        {
            if (character.Movements.ContainsKey(name)) return false;
            CharacterMovement m = new CharacterMovement(name, character);
            character.Movements.Add(m);
            return true;
        }

        protected void when_movement_removed(Character character, string name)
        {
            if (character.Movements.ContainsKey(name))
                character.Movements.Remove(name);
        }

        protected void when_distance_limit_set(CharacterMovement movement, float limit)
        {
            movement.DistanceLimit = limit;
        }

        protected void when_activation_key_set(CharacterMovement movement, string key)
        {
            if (string.IsNullOrEmpty(key))
                movement.ActivationKey = Keys.None;
            else
                movement.ActivationKey = (Keys)System.Enum.Parse(typeof(Keys), key);
        }

        protected void when_default_movement_set(Character character, string name)
        {
            if (character.Movements.ContainsKey(name))
                character.DefaultMovement = character.Movements[name];
        }

        protected void when_default_movement_cleared(Character character)
        {
            character.DefaultMovement = null;
        }

        protected void when_default_movements_added(Character character)
        {
            string[] defaults = new[] { "Walk", "Run", "Swim" };
            for (int i = 0; i < defaults.Length; i++)
            {
                if (!character.Movements.ContainsKey(defaults[i]))
                {
                    CharacterMovement m = new CharacterMovement(defaults[i], character);
                    character.Movements.Add(m);
                }
            }
        }

        // Then helpers

        protected void then_movement_in_group(Character character, string name)
        {
            character.Movements.ContainsKey(name).Should().BeTrue(
                string.Format("Movement '{0}' must be in the Movement Option Group", name));
        }

        protected void then_movement_not_in_group(Character character, string name)
        {
            character.Movements.ContainsKey(name).Should().BeFalse(
                string.Format("Movement '{0}' must not be in the group after removal", name));
        }

        protected void then_movement_count(Character character, int expected)
        {
            character.Movements.Count.Should().Be(expected,
                string.Format("Movement Option Group must contain exactly {0} movements", expected));
        }

        protected void then_default_movement(Character character, string name)
        {
            character.DefaultMovement.Should().NotBeNull();
            character.DefaultMovement.Name.Should().Be(name,
                string.Format("'{0}' must carry the default movement designation", name));
        }

        protected void then_no_default_movement(Character character)
        {
            character.DefaultMovement.Should().BeNull("no movement should carry the default designation");
        }

        protected void then_activation_key(CharacterMovement movement, string expected)
        {
            if (expected == null)
                movement.ActivationKey.Should().Be(Keys.None,
                    "activation key must be cleared (None) when unset");
            else
                movement.ActivationKey.ToString().Should().Be(expected,
                    string.Format("Activation key must be '{0}'", expected));
        }

        protected void then_add_rejected(bool result)
        {
            result.Should().BeFalse("duplicate or invalid name must cause rejection");
        }
    }
}
