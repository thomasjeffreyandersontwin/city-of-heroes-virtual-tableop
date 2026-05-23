using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MovementExecution
{
    [TestClass]
    public class TrackMovementDistanceCount : MovementExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ActivationBeginsResetToZero()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "50");

            // When
            WhenMovementActivationBegins();

            // Then
            ThenCumulativeDistance(0);
        }

        [TestMethod]
        public void AfterStepsReachesLimit()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "50");

            // When
            WhenMovementActivationBegins();
            WhenStepsIssued(10);

            // Then
            ThenCumulativeDistance(50);
            ThenMovementHalted();
        }

        [TestMethod]
        public void NoLimitDistanceTrackedButNoHalt()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "absent");

            // When
            WhenMovementActivationBegins();
            WhenStepsIssued(15);

            // Then
            ThenCumulativeDistance(75);
        }
    }
}
