using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class ReadCharacterPositionFromMemory : MemoryInterfaceHelper
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
            GivenMemoryPointerState("position", "valid");
            GivenCharacterPosition("125.5", "0.0", "-340.2");

            // When
            WhenMovementRequestsPosition();

            // Then
            ThenCharacterPositionReturned("125.5", "0.0", "-340.2");
        }

        [TestMethod]
        public void StalePointerRefreshFirst()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenMemoryPointerState("position", "stale");
            GivenCharacterPosition("125.5", "0.0", "-340.2");

            // When
            WhenMovementRequestsPosition();

            // Then
            ThenCharacterPositionReturned("125.5", "0.0", "-340.2");
        }
    }
}
