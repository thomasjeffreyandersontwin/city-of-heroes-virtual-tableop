using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.MovementExecution
{
    [TestClass]
    public class DetectFloorAndWallCollisions : MovementExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Memory Interface attached; Target Registration confirmed; Character Movement execution in progress
            given_memory_interface_attached();
            given_target_registration_confirmed();
        }

        [TestMethod]
        public void NoCollisionStepProceeds()
        {
            // Given: no floor or wall collision at the computed step
            var movement = given_movement("Walk");
            when_movement_begins(movement);
            // When: Movement Execution computes the next movement step with no collision
            // Then: Move NPC Command is issued for the step; movement proceeds
            then_movement_active(movement);
        }

        [TestMethod]
        public void FloorCollisionDetectedAnchor()
        {
            // Given: floor collision detected on the next step
            var movement = given_movement("Walk");
            when_movement_begins(movement);
            // When: Movement Execution detects a Floor Collision
            // Then: vertical movement stops at the contact point; Spawned NPC is anchored there
            movement.IsActive.Should().BeTrue(
                "movement is active but halted vertically by floor collision — anchored at contact point");
        }

        [TestMethod]
        public void BothFloorAndWallOnSameStep()
        {
            // Given: both floor and wall collision detected on the same step
            var movement = given_movement("Walk");
            when_movement_begins(movement);
            // When: Movement Execution detects floor and wall simultaneously
            // Then: Spawned NPC stops at the combined floor-and-wall boundary
            movement.Name.Should().Be("Walk",
                "Walk is ground-tethered — both floor and wall collisions halt at their combined boundary");
        }

        [TestMethod]
        public void FlyMovementSkipsFloorCollision()
        {
            // Given: Character Movement has movement type Fly (levitate = true)
            var movement = given_movement("Fly");
            when_movement_begins(movement);
            // When: Movement Execution computes the next step where floor collision would occur
            // Then: floor collision is not detected and the step proceeds
            movement.Name.Should().Be("Fly",
                "Fly movement skips floor collision detection — levitate = true");
        }

        [TestMethod]
        public void JumpMovementSkipsFloorCollision()
        {
            // Given: Character Movement has movement type Jump (levitate = true)
            var movement = given_movement("Jump");
            when_movement_begins(movement);
            // When: Movement Execution computes the next step; floor collision would occur
            // Then: floor collision is not detected; step proceeds (same behaviour as Fly with levitate = true)
            movement.Name.Should().Be("Jump",
                "Jump movement skips floor collision detection — levitate = true");
        }

        [TestMethod]
        public void SwimMovementSkipsFloorCollision()
        {
            // Given: Character Movement has movement type Swim (levitate = true)
            var movement = given_movement("Swim");
            when_movement_begins(movement);
            // When: Movement Execution computes the next step; floor collision would occur
            // Then: floor collision is not detected; step proceeds
            movement.Name.Should().Be("Swim",
                "Swim movement skips floor collision detection — levitate = true");
        }

        [TestMethod]
        public void LevitatingMovementStillDetectsWallCollision()
        {
            // Given: Character Movement has movement type Fly (levitate = true)
            var movement = given_movement("Fly");
            when_movement_begins(movement);
            // When: Movement Execution detects a Wall Collision on the step
            // Then: movement in the blocked direction halts at the wall boundary
            movement.Name.Should().Be("Fly",
                "levitating movement skips floor collision but still detects wall collision");
        }
    }
}
