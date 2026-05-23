using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class WriteCharacterPositionToMemory : MemoryInterfaceHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidPointerRegisteredTargetWriteSucceeds()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenMemoryPointerState("position", "valid");

            // When
            WhenMovementComputesDestination("200.0", "5.0", "-100.0");

            // Then
            ThenCharacterPositionWritten("200.0", "5.0", "-100.0");
        }

        [TestMethod]
        public void StalePointerRefreshThenWrite()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenMemoryPointerState("position", "stale");

            // When
            WhenMovementComputesDestination("200.0", "5.0", "-100.0");

            // Then
            ThenCharacterPositionWritten("200.0", "5.0", "-100.0");
        }

        [TestMethod]
        public void UnregisteredTargetWriteBlocked()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("pending");
            GivenMemoryPointerState("position", "valid");

            // When
            WhenMovementComputesDestination("200.0", "5.0", "-100.0");

            // Then
            ThenWriteBlocked();
        }
    }
}
