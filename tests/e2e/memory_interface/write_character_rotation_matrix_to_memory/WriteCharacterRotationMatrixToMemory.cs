using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class WriteCharacterRotationMatrixToMemory : MemoryInterfaceHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidPointerNormalWrite()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenMemoryPointerState("rotationMatrix", "valid");

            // When
            WhenMovementComputesNewFacing();

            // Then
            ThenRotationMatrixWritten();
        }

        [TestMethod]
        public void StalePointerRefreshFirst()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenMemoryPointerState("rotationMatrix", "stale");

            // When
            WhenMovementComputesNewFacing();

            // Then
            ThenRotationMatrixWritten();
        }
    }
}
