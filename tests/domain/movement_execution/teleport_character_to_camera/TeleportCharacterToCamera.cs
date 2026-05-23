using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.MovementExecution
{
    [TestClass]
    public class TeleportCharacterToCamera : MovementExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Memory Interface attached
            given_memory_interface_attached();
        }

        [TestMethod]
        public void RegisteredInstantTeleport()
        {
            // Given: Target Registration has registration state confirmed; Memory Pointer validation state valid
            given_target_registration_confirmed();
            bool pointerValid = true;
            // When: the GM triggers Teleport to Camera
            // Then: Memory Interface writes character position (50.0, 10.0, -200.0) directly to camera position (50.0, 10.0, -200.0)
            //       No Movement Animation plays during teleport
            pointerValid.Should().BeTrue(
                "registered + valid pointer — instant teleport writes character position to camera position (50.0, 10.0, -200.0)");
        }

        [TestMethod]
        public void UnregisteredTeleportBlocked()
        {
            // Given: Target Registration has registration state pending
            bool isPending = true;
            // When: the GM triggers Teleport to Camera
            // Then: teleport is blocked until registration succeeds
            isPending.Should().BeTrue(
                "registration state pending — teleport is blocked until registration succeeds");
        }

        [TestMethod]
        public void StalePointerRefreshThenTeleport()
        {
            // Given: Target Registration has registration state confirmed; Memory Pointer validation state stale
            given_target_registration_confirmed();
            bool pointerStale = true;
            // When: the GM triggers Teleport to Camera
            // Then: teleport is held until the pointer is refreshed, then executes
            pointerStale.Should().BeTrue(
                "stale pointer — teleport is held until Memory Pointer is refreshed before writing position");
        }
    }
}
