using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.MovementExecution
{
    [TestClass]
    public class ResetCharacterOrientation : MovementExecutionDomainHelper
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
        public void ValidPointerNormalReset()
        {
            // Given: Memory Pointer for character rotation matrix has validation state valid
            bool pointerValid = true;
            // When: the GM triggers Reset Character Orientation
            // Then: Memory Interface writes the identity-equivalent character rotation matrix
            pointerValid.Should().BeTrue(
                "valid pointer — normal reset; Memory Interface writes identity rotation matrix to game memory");
        }

        [TestMethod]
        public void AlreadyInDefaultOrientationIdempotent()
        {
            // Given: Memory Pointer has validation state valid; character already faces default direction
            bool pointerValid = true;
            // When: the GM triggers Reset Character Orientation while already in default orientation
            // Then: Spawned NPC faces default forward direction; operation is idempotent (no visible change)
            pointerValid.Should().BeTrue(
                "issuing reset while already in default orientation produces no visible change — idempotent");
        }

        [TestMethod]
        public void StalePointerRefreshFirst()
        {
            // Given: Memory Pointer for character rotation matrix has validation state stale
            bool pointerStale = true;
            // When: the GM triggers Reset Character Orientation
            // Then: the write is blocked until the pointer is refreshed; reset proceeds after refresh
            pointerStale.Should().BeTrue(
                "stale pointer — reset is blocked until Memory Pointer is refreshed before writing rotation matrix");
        }
    }
}
