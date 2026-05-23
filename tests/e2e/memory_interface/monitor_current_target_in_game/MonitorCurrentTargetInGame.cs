using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class MonitorCurrentTargetInGame : MemoryInterfaceHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void TargetChangesDuringMovement()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenSessionActive();
            GivenCurrentTarget("Guard_Captain_01");

            // When
            WhenTargetChanges("Villain_03");

            // Then
            ThenCurrentTarget("Villain_03");
            ThenMovementExecutionNotified();
        }

        [TestMethod]
        public void TargetClearedByGm()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenSessionActive();
            GivenCurrentTarget("Guard_Captain_01");

            // When
            WhenTargetChanges("empty");

            // Then
            ThenCurrentTarget("empty");
            ThenMovementCommandsBlocked();
        }

        [TestMethod]
        public void TargetRestoredAfterClear()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenSessionActive();
            GivenCurrentTarget("empty");

            // When
            WhenTargetChanges("Guard_Captain_01");

            // Then
            ThenCurrentTarget("Guard_Captain_01");
            ThenMovementExecutionNotified();
        }
    }
}
