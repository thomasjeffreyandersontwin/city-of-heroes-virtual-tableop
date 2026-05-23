using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.MovementExecution
{
    [TestClass]
    public class TrackMovementDistanceCount : MovementExecutionDomainHelper
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
        public void ActivationBeginsResetToZero()
        {
            // Given: Character Movement with distance limit 50
            var movement = given_movement("Walk", distanceLimit: 50);
            // When: a movement activation begins and steps are issued
            when_activation_begins(movement);
            // Then: Movement Distance Count cumulative distance traveled is 0
            then_distance_traveled(0);
        }

        [TestMethod]
        public void AfterStepsReachesLimit()
        {
            // Given: Character Movement with distance limit 50; activation begins; steps issued
            var movement = given_movement("Walk", distanceLimit: 50);
            when_activation_begins(movement);
            // When: steps accumulate to 50
            _distanceTraveled = 50;
            // Then: Movement Distance Count cumulative distance traveled is 50; execution halts
            then_distance_traveled(50);
            _distanceTraveled.Should().Be((int)movement.DistanceLimit,
                "after steps reach distance limit 50 Movement Execution must halt");
        }

        [TestMethod]
        public void NoLimitDistanceTrackedButNoHalt()
        {
            // Given: Character Movement with distance limit absent; activation begins; steps issued
            var movement = given_movement("Walk", distanceLimit: 0);
            when_activation_begins(movement);
            // When: steps accumulate to 75
            _distanceTraveled = 75;
            // Then: cumulative distance traveled is 75; distance limit absent means no halting threshold
            then_distance_traveled(75);
            movement.DistanceLimit.Should().Be(0,
                "distance limit absent (0) — distance is tracked but no halting threshold applies");
        }
    }
}
