using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MovementExecution
{
    [TestClass]
    public class EnforceDistanceLimitPerMovementType : MovementExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void WalkLimitedTo50()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovement("Walk", "Walk", "50");

            // When
            WhenDistanceLimitReached();

            // Then
            ThenDistanceLimitEnforced("Walk", 50);
            ThenMovementHalted();
        }

        [TestMethod]
        public void RunLimitedTo100()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovement("Run", "Run", "100");

            // When
            WhenDistanceLimitReached();

            // Then
            ThenDistanceLimitEnforced("Run", 100);
            ThenMovementHalted();
        }

        [TestMethod]
        public void LimitChangedMidSession()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovement("Sprint", "Run", "75");

            // When
            WhenDistanceLimitReached();

            // Then
            ThenDistanceLimitEnforced("Sprint", 75);
            ThenMovementHalted();
        }
    }
}
