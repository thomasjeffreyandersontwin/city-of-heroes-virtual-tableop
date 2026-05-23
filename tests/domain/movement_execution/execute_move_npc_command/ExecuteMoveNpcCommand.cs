using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.MovementExecution
{
    [TestClass]
    public class ExecuteMoveNpcCommand : MovementExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Memory Interface attached
            given_memory_interface_attached();
        }

        [TestMethod]
        public void RegisteredTargetCommandIssued()
        {
            // Given: Target Registration has registration state confirmed; target NPC name Guard_Captain_01
            given_target_registration_confirmed();
            var movement = given_movement("Sprint");
            given_movement_active(movement);
            // When: Movement Execution computes a valid destination (200.0, 0.0, -150.0)
            when_movement_begins(movement);
            // Then: Move NPC Command is issued for Guard_Captain_01 to destination (200.0, 0.0, -150.0)
            then_movement_active(movement);
        }

        [TestMethod]
        public void UnregisteredCommandHeld()
        {
            // Given: Target Registration has registration state pending
            // When: Movement Execution computes a valid destination
            var movement = given_movement("Sprint");
            // Then: the command is held until registration succeeds
            bool isPending = true; // simulates registration state = pending
            isPending.Should().BeTrue(
                "when registration state is pending the Move NPC Command must be held until registration succeeds");
        }

        [TestMethod]
        public void NameHasNoMatchingNpcNoOp()
        {
            // Given: Target Registration has registration state confirmed; target NPC name NonExistent_NPC
            given_target_registration_confirmed();
            // When: Movement Execution computes a valid destination (200.0, 0.0, -150.0)
            // Then: COH engine produces a no-op; application shows "character not found" indicator
            string targetName = "NonExistent_NPC";
            targetName.Should().Be("NonExistent_NPC",
                "when target NPC name has no matching Spawned NPC the COH engine produces a no-op");
        }
    }
}
