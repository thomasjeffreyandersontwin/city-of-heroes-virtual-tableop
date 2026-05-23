using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Movements;

namespace HeroVTT.DomainTests.MovementExecution
{
    public class MovementExecutionDomainHelper
    {
        protected Character _character;
        protected CharacterMovement _activeMovement;
        protected int _distanceTraveled;

        [TestInitialize]
        public void Init()
        {
            _character = new Character("Guard_Captain_01");
            _distanceTraveled = 0;
        }

        // Given helpers

        protected CharacterMovement given_movement(string name, float distanceLimit = 0f)
        {
            CharacterMovement m = new CharacterMovement(name, _character);
            if (distanceLimit > 0f) m.DistanceLimit = distanceLimit;
            return m;
        }

        protected void given_movement_active(CharacterMovement movement)
        {
            _activeMovement = movement;
            if (!_character.Movements.ContainsKey(movement.Name))
                _character.Movements.Add(movement);
        }

        protected void given_memory_interface_attached()
        {
            // Memory interface attached — simulated by NoOpGameCommandExecutor in Tier 1 tests
        }

        protected void given_target_registration_confirmed()
        {
            // Target registration confirmed — simulated state for domain tests
        }

        // When helpers

        protected void when_movement_begins(CharacterMovement movement)
        {
            movement.IsActive = true;
        }

        protected void when_movement_halts(CharacterMovement movement)
        {
            movement.IsActive = false;
        }

        protected void when_activation_begins(CharacterMovement movement)
        {
            _distanceTraveled = 0;
            movement.IsActive = true;
        }

        // Then helpers

        protected void then_movement_active(CharacterMovement movement)
        {
            movement.IsActive.Should().BeTrue(
                string.Format("Movement '{0}' must be active", movement.Name));
        }

        protected void then_movement_not_active(CharacterMovement movement)
        {
            movement.IsActive.Should().BeFalse(
                string.Format("Movement '{0}' must not be active", movement.Name));
        }

        protected void then_distance_traveled(int expected)
        {
            _distanceTraveled.Should().Be(expected,
                string.Format("Distance traveled must be {0}", expected));
        }
    }
}
