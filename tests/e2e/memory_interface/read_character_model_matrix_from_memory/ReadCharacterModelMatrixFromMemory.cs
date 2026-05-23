using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class ReadCharacterModelMatrixFromMemory : MemoryInterfaceHelper
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
            GivenMemoryPointerState("modelMatrix", "valid");

            // When
            WhenMovementNeedsModelMatrix();

            // Then
            ThenModelMatrixReturned();
        }

        [TestMethod]
        public void StalePointerRefreshFirst()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenMemoryPointerState("modelMatrix", "stale");

            // When
            WhenMovementNeedsModelMatrix();

            // Then
            ThenModelMatrixReturned();
        }
    }
}
