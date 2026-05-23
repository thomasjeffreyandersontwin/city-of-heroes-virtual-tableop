using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class ReadCameraPositionFromMemory : MemoryInterfaceHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void RigActiveNormalRead()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenCameraRigState("active");
            GivenMemoryPointerState("cameraPosition", "valid");

            // When
            WhenCameraRelativeCommandTriggered();

            // Then
            ThenCameraPositionReturned("50.0", "10.0", "-200.0");
        }

        [TestMethod]
        public void RigInactiveRawCoordsUsed()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenCameraRigState("inactive");
            GivenMemoryPointerState("cameraPosition", "valid");

            // When
            WhenCameraRelativeCommandTriggered();

            // Then
            ThenCameraPositionReturned("50.0", "10.0", "-200.0");
        }

        [TestMethod]
        public void StalePointerRefreshFirst()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenCameraRigState("active");
            GivenMemoryPointerState("cameraPosition", "stale");

            // When
            WhenCameraRelativeCommandTriggered();

            // Then
            ThenCameraPositionReturned("50.0", "10.0", "-200.0");
        }
    }
}
