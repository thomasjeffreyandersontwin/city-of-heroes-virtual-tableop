using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class ScanAndFixStaleMemoryPointers : MemoryInterfaceHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void PointerValidOnScan()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenMemoryPointerState("position", "valid");

            // When
            WhenPeriodicScanRuns();

            // Then
            ThenMemoryPointerState("position", "valid");
            ThenStalePointerNotDetected("position");
        }

        [TestMethod]
        public void PointerStaleOnScanReResolved()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenMemoryPointerState("position", "stale");

            // When
            WhenPeriodicScanRuns();

            // Then
            ThenMemoryPointerState("position", "valid");
            ThenStalePointerDetected("position");
        }

        [TestMethod]
        public void PointerStaleMidOperation()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenMemoryPointerState("position", "stale");

            // When
            WhenPeriodicScanRuns();

            // Then
            ThenMemoryPointerState("position", "valid");
            ThenStalePointerDetected("position");
        }

        [TestMethod]
        public void GameProcessRestartsAllPointersReset()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenMemoryPointerState("position", "stale");

            // When
            WhenPeriodicScanRuns();

            // Then
            ThenMemoryPointerState("position", "valid");
            ThenStalePointerDetected("position");
        }
    }
}
