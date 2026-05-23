using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityRendering
{
    [TestClass]
    public class SwitchActiveIdentityOnSpawnedCharacter : IdentityRenderingHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SwitchToModelIdentityDeleteOldSpawnNew()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenActiveIdentity("Old_Look");
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenModelIdentity("Dragon_Model", "Skull_Lt_01");

            // When
            WhenIdentitySwitchInitiated("Dragon_Model");

            // Then
            ThenDeleteNpcCommandIssued("Guard_Captain");
            ThenSpawnNpcCommandIssued("Guard_Captain");
            ThenSpawnedNpcPresent("Guard_Captain");
        }

        [TestMethod]
        public void SwitchToCostumeIdentityDeleteOldSpawnTargetLoad()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenActiveIdentity("Old_Look");
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenCostumeIdentity("Knight_Armor", @"C:\Games\CoH\costumes\guard.costume");
            GivenCostumeFileAt(@"C:\Games\CoH\costumes\guard.costume");

            // When
            WhenIdentitySwitchInitiated("Knight_Armor");

            // Then
            ThenDeleteNpcCommandIssued("Guard_Captain");
            ThenSpawnedNpcPresent("Guard_Captain");
            ThenLoadCostumeCommandIssued(@"C:\Games\CoH\costumes\guard.costume");
        }

        [TestMethod]
        public void DeleteOldNpcFailsAlreadyGoneSwitchContinues()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenActiveIdentity("Old_Look");
            GivenSpawnedNpc("Guard_Captain", "absent");
            GivenModelIdentity("Dragon_Model", "Skull_Lt_01");

            // When
            WhenIdentitySwitchInitiated("Dragon_Model");

            // Then
            ThenSpawnedNpcPresent("Guard_Captain");
        }

        [TestMethod]
        public void SwitchCompletesUiIndicatorsUpdated()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenActiveIdentity("Old_Look");
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenModelIdentity("Dragon_Model", "Skull_Lt_01");

            // When
            WhenIdentitySwitchInitiated("Dragon_Model");

            // Then
            ThenActiveIndicatorShown("Dragon_Model");
        }
    }
}
