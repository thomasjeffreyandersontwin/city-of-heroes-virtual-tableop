using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MovementExecution
{
    [TestClass]
    public class ResetCharacterOrientation : MovementExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidPointerNormalReset()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenMemoryPointerState("rotationMatrix", "valid");

            // When
            WhenGmTriggersResetOrientation();

            // Then
            ThenCharacterFacesDefaultOrientation();
        }

        [TestMethod]
        public void AlreadyInDefaultOrientationIdempotent()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenMemoryPointerState("rotationMatrix", "valid");

            // When
            WhenGmTriggersResetOrientation();

            // Then
            ThenCharacterFacesDefaultOrientation();
        }

        [TestMethod]
        public void StalePointerRefreshFirst()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenMemoryPointerState("rotationMatrix", "stale");

            // When
            WhenGmTriggersResetOrientation();

            // Then
            ThenCharacterFacesDefaultOrientation();
        }
    }
}
