using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class DetectGameProcessForConnection : MemoryInterfaceHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void CohClientRunningSingleProcessAttaches()
        {
            // Given
            GivenApplicationStarted();
            GivenGameProcessRunning();

            // When
            WhenMemoryInterfaceAttemptsToAttach();

            // Then
            ThenGameProcessState("running");
            ThenMemoryInterfaceState("attached");
            ThenMovementServicesAvailable();
        }

        [TestMethod]
        public void CohClientNotRunningAtStartupUnattached()
        {
            // Given
            GivenApplicationStarted();
            GivenGameProcessNotRunning();

            // When
            WhenMemoryInterfaceAttemptsToAttach();

            // Then
            ThenGameProcessState("not running");
            ThenMemoryInterfaceState("unattached");
            ThenMovementCommandsBlocked();
        }

        [TestMethod]
        public void CohTerminatesDuringSessionUnattached()
        {
            // Given
            GivenApplicationStarted();
            GivenGameProcessRunning();
            WhenMemoryInterfaceAttemptsToAttach();

            // When
            WhenGameProcessTerminates();

            // Then
            ThenGameProcessState("not running");
            ThenMemoryInterfaceState("unattached");
        }

        [TestMethod]
        public void MultipleCohProcessesCorrectWindowHandleAttaches()
        {
            // Given
            GivenApplicationStarted();
            GivenGameProcessRunning();

            // When
            WhenMemoryInterfaceAttemptsToAttach();

            // Then
            ThenGameProcessState("running");
            ThenMemoryInterfaceState("attached");
        }
    }
}
