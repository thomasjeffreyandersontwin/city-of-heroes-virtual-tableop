using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.MovementExecution
{
    [TestClass]
    public class AnimateMovement : MovementExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: a Character Movement is active on a Spawned NPC
        }

        [TestMethod]
        public void WalkMovementBegins()
        {
            // Given: Character Movement has movement type Walk
            var movement = given_movement("Walk");
            // When: Movement Execution begins the movement
            when_movement_begins(movement);
            // Then: Movement Animation has active animation cycle walk
            movement.Name.Should().Be("Walk", "movement must be Walk");
            then_movement_active(movement);
        }

        [TestMethod]
        public void RunMovementBegins()
        {
            // Given: Character Movement has movement type Run
            var movement = given_movement("Run");
            // When: Movement Execution begins the movement
            when_movement_begins(movement);
            // Then: Movement Animation has active animation cycle run
            movement.Name.Should().Be("Run", "movement must be Run");
            then_movement_active(movement);
        }

        [TestMethod]
        public void SwimMovementBegins()
        {
            // Given: Character Movement has movement type Swim
            var movement = given_movement("Swim");
            // When: Movement Execution begins the movement
            when_movement_begins(movement);
            // Then: Movement Animation has active animation cycle swim
            movement.Name.Should().Be("Swim", "movement must be Swim");
            then_movement_active(movement);
        }

        [TestMethod]
        public void FlyMovementBegins()
        {
            // Given: Character Movement has movement type Fly
            var movement = given_movement("Fly");
            // When: Movement Execution begins the movement
            when_movement_begins(movement);
            // Then: Movement Animation has active animation cycle fly
            movement.Name.Should().Be("Fly", "movement must be Fly");
            then_movement_active(movement);
        }

        [TestMethod]
        public void JumpMovementBegins()
        {
            // Given: Character Movement has movement type Jump
            var movement = given_movement("Jump");
            // When: Movement Execution begins the movement
            when_movement_begins(movement);
            // Then: Movement Animation has active animation cycle jump
            movement.Name.Should().Be("Jump", "movement must be Jump");
            then_movement_active(movement);
        }

        [TestMethod]
        public void MovementHaltsAnimationStops()
        {
            // Given: Character Movement has movement type Walk and is active
            var movement = given_movement("Walk");
            when_movement_begins(movement);
            // When: Movement Execution halts the movement
            when_movement_halts(movement);
            // Then: Movement Animation has active animation cycle stopped; Spawned NPC returns to idle pose
            then_movement_not_active(movement);
        }
    }
}
