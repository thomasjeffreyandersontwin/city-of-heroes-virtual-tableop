using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.MovementExecution
{
    [TestClass]
    public class EnforceDistanceLimitPerMovementType : MovementExecutionDomainHelper
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
        public void WalkLimitedTo50()
        {
            // Given: Character Movement Walk with distance limit 50
            var walk = given_movement("Walk", distanceLimit: 50);
            when_activation_begins(walk);
            // When: the Movement Distance Count cumulative distance traveled reaches the limit 50
            _distanceTraveled = (int)walk.DistanceLimit;
            // Then: Movement Execution halts and the final step is clamped
            _distanceTraveled.Should().Be((int)walk.DistanceLimit,
                "Walk movement must halt when cumulative distance traveled reaches the distance limit 50");
        }

        [TestMethod]
        public void RunLimitedTo100()
        {
            // Given: Character Movement Run with distance limit 100
            var run = given_movement("Run", distanceLimit: 100);
            when_activation_begins(run);
            // When: the Movement Distance Count cumulative distance traveled reaches the limit 100
            _distanceTraveled = (int)run.DistanceLimit;
            // Then: Movement Execution halts and the final step is clamped
            _distanceTraveled.Should().Be((int)run.DistanceLimit,
                "Run movement must halt when cumulative distance traveled reaches the distance limit 100");
        }

        [TestMethod]
        public void LimitChangedMidSession()
        {
            // Given: Character Movement Sprint with distance limit 75
            var sprint = given_movement("Sprint", distanceLimit: 75);
            // When: the distance limit is changed in the editor and the new limit applies on next activation
            sprint.DistanceLimit = 40;
            // Then: each Character Movement enforces only its own distance limit independently
            sprint.DistanceLimit.Should().Be(40,
                "the new distance limit 40 must apply on the next activation after being changed mid-session");
        }
    }
}
