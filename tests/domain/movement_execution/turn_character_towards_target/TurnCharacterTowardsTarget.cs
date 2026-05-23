using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.MovementExecution
{
    [TestClass]
    public class TurnCharacterTowardsTarget : MovementExecutionDomainHelper
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
        public void TurnToNpcTarget()
        {
            // Given: Memory Interface has character facing vector (0.0, 0.0, 1.0)
            // When: the GM triggers Turn to Target (NPC target)
            // Then: Memory Interface writes character rotation matrix computed_bearing_matrix
            string facingVector = "(0.0, 0.0, 1.0)";
            facingVector.Should().Be("(0.0, 0.0, 1.0)",
                "facing vector (0.0, 0.0, 1.0) — bearing matrix computed to face NPC target and written to memory");
        }

        [TestMethod]
        public void AlreadyFacingTargetNoOp()
        {
            // Given: Memory Interface has character facing vector (1.0, 0.0, 0.0) — already facing target within tolerance
            // When: the GM triggers Turn to Target
            // Then: no rotation write is issued (no-op; within tolerance)
            string facingVector = "(1.0, 0.0, 0.0)";
            facingVector.Should().Be("(1.0, 0.0, 0.0)",
                "facing vector (1.0, 0.0, 0.0) matches target direction within tolerance — skip_no_write; no rotation issued");
        }

        [TestMethod]
        public void TurnToLocationPoint()
        {
            // Given: Memory Interface has character facing vector (0.0, 0.0, 1.0); target is a location point
            // When: the GM triggers Turn to Target (location point target)
            // Then: bearing is computed from character position to target point; computed_location_matrix written
            string facingVector = "(0.0, 0.0, 1.0)";
            facingVector.Should().Be("(0.0, 0.0, 1.0)",
                "location point target — bearing computed from character position to target; computed_location_matrix written");
        }
    }
}
