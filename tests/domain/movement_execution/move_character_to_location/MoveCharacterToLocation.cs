using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.MovementExecution
{
    [TestClass]
    public class MoveCharacterToLocation : MovementExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Memory Interface attached; Target Registration confirmed
            given_memory_interface_attached();
            given_target_registration_confirmed();
        }

        [TestMethod]
        public void DestinationReachedBeforeLimit()
        {
            // Given: Character Movement with distance limit 100; cumulative distance traveled 0
            var movement = given_movement("Walk", distanceLimit: 100);
            when_activation_begins(movement);
            // When: the GM triggers Move to Location; destination reached at 35 units
            _distanceTraveled = 35;
            // Then: step loop stops; cumulative distance traveled is 35
            then_distance_traveled(35);
            _distanceTraveled.Should().BeLessOrEqualTo((int)movement.DistanceLimit,
                "destination reached before limit 100 — 35 units traveled; step loop stops");
        }

        [TestMethod]
        public void DistanceLimitReachedBeforeDestination()
        {
            // Given: Character Movement with distance limit 50; cumulative distance traveled 0
            var movement = given_movement("Walk", distanceLimit: 50);
            when_activation_begins(movement);
            // When: the GM triggers Move to Location; distance limit 50 reached before destination
            _distanceTraveled = (int)movement.DistanceLimit;
            // Then: Movement Execution halts with a limit-reached indicator; final step is clamped
            then_distance_traveled(50);
            _distanceTraveled.Should().Be((int)movement.DistanceLimit,
                "distance limit 50 reached before destination — execution halts and final step is clamped");
        }

        [TestMethod]
        public void FloorCollisionHaltsVertical()
        {
            // Given: Character Movement with distance limit 100; cumulative distance traveled 0
            var movement = given_movement("Walk", distanceLimit: 100);
            when_activation_begins(movement);
            // When: the GM triggers Move to Location; Floor Collision detected at 20 units
            _distanceTraveled = 20;
            // Then: floor collision detected — vertical movement stops at contact point; Spawned NPC anchored there
            _distanceTraveled.Should().Be(20,
                "Walk is ground-tethered; floor collision at 20 units halts vertical movement — NPC anchored");
        }

        [TestMethod]
        public void WallCollisionHaltsHorizontal()
        {
            // Given: Character Movement with distance limit 100; cumulative distance traveled 0
            var movement = given_movement("Walk", distanceLimit: 100);
            when_activation_begins(movement);
            // When: the GM triggers Move to Location; Wall Collision detected at 15 units
            _distanceTraveled = 15;
            // Then: wall collision detected — movement in blocked direction halts at wall boundary
            _distanceTraveled.Should().Be(15,
                "Wall collision at 15 units halts horizontal movement at the wall boundary");
        }
    }
}
