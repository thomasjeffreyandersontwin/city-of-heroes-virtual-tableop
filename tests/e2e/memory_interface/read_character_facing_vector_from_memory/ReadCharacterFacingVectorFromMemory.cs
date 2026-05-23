using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class ReadCharacterFacingVectorFromMemory : MemoryInterfaceHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidPointerNormalRead()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenMemoryPointerState("facingVector", "valid");

            // When
            WhenMovementNeedsFacingVector();

            // Then
            ThenFacingVectorReturned();
        }

        [TestMethod]
        public void StalePointerRefreshFirst()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenMemoryPointerState("facingVector", "stale");

            // When
            WhenMovementNeedsFacingVector();

            // Then
            ThenFacingVectorReturned();
        }
    }
}
