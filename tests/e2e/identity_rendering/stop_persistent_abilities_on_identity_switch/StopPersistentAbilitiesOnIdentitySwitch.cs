using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityRendering
{
    [TestClass]
    public class StopPersistentAbilitiesOnIdentitySwitch : IdentityRenderingHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ActivePersistentAbilitiesStoppedBeforeDespawn()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenActiveIdentity("Old_Look");
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenPersistentAbilitiesActive("Guard_Captain");

            // When
            WhenIdentitySwitchInitiated("Dragon_Model");

            // Then
            ThenPersistentAbilitiesStopped("Guard_Captain");
            ThenDeleteNpcCommandIssued("Guard_Captain");
        }

        [TestMethod]
        public void NoPersistentAbilitiesStopStepSkipped()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenActiveIdentity("Old_Look");
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenNoPersistentAbilities("Guard_Captain");

            // When
            WhenIdentitySwitchInitiated("Dragon_Model");

            // Then
            ThenDeleteNpcCommandIssued("Guard_Captain");
        }

        [TestMethod]
        public void PersistentAbilityStopFailsSwitchContinues()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenActiveIdentity("Old_Look");
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenPersistentAbilitiesActive("Guard_Captain");

            // When
            WhenIdentitySwitchInitiated("Dragon_Model");

            // Then
            ThenGameBridgeLogsFailure();
            ThenDeleteNpcCommandIssued("Guard_Captain");
        }

        [TestMethod]
        public void StoppedAbilitiesRemainStoppedAfterSwitch()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenActiveIdentity("Old_Look");
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenPersistentAbilitiesActive("Guard_Captain");

            // When
            WhenIdentitySwitchInitiated("Dragon_Model");

            // Then
            ThenPersistentAbilitiesStopped("Guard_Captain");
        }
    }
}
