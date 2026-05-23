using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class WaitUntilTargetIsRegisteredAfterSpawn : MemoryInterfaceHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void NpcRegistersWithinTimeout()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenSpawnedNpcJustCreated();

            // When
            WhenMemoryInterfacePollsForRegistration();

            // Then
            ThenTargetRegistrationState("confirmed");
            ThenMovementServicesAvailable();
        }

        [TestMethod]
        public void NpcFailsToRegisterTimeout()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenSpawnedNpcJustCreated();
            GivenTargetRegistrationState("pending");

            // When
            WhenMemoryInterfacePollsForRegistration();

            // Then
            ThenTargetRegistrationState("pending");
            ThenMovementCommandsBlocked();
        }

        [TestMethod]
        public void MovementTriggeredBeforeRegistration()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenSpawnedNpcJustCreated();
            GivenTargetRegistrationState("pending");

            // When
            WhenMovementTriggeredBeforeRegistration();

            // Then
            ThenMovementCommandsBlocked();
        }
    }
}
