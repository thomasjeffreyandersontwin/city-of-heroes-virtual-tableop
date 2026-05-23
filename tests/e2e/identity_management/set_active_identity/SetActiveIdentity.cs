using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityManagement
{
    [TestClass]
    public class SetActiveIdentity : IdentityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ModelIdentityActivatedNpcSpawned()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenModelIdentity("Dragon_Model", "Skull_Lt_01");

            // When
            WhenGmSetsActiveDesignation("Dragon_Model");

            // Then
            ThenSpawnedNpcPresence("Guard_Captain", "present");
        }

        [TestMethod]
        public void CostumeIdentityActivatedSpawnTargetLoadSequence()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenCostumeIdentity("Knight_Armor", @"C:\Games\CoH\costumes\guard.costume");
            GivenCostumeFileAt(@"C:\Games\CoH\costumes\guard.costume");

            // When
            WhenGmSetsActiveDesignation("Knight_Armor");

            // Then
            ThenSpawnedNpcPresence("Guard_Captain", "present");
        }

        [TestMethod]
        public void SwitchFromExistingActiveIdentityDespawnsOld()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Old_Look", "active", "unset");
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenModelIdentity("Dragon_Model", "Skull_Lt_01");

            // When
            WhenGmSetsActiveDesignation("Dragon_Model");

            // Then
            ThenSpawnedNpcPresence("Guard_Captain", "present");
            ThenIdentityExistsWithState("Old_Look", "inactive", "unset");
        }

        [TestMethod]
        public void BridgeNotReadyActivationBlocked()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameBridgeNotReady();
            GivenCharacterSelected("Guard_Captain");
            GivenModelIdentity("Dragon_Model", "Skull_Lt_01");

            // When
            WhenGmSetsActiveDesignation("Dragon_Model");

            // Then
            ThenSetActiveBlocked("game not connected");
            ThenNoGameCommandsIssued();
        }

        [TestMethod]
        public void CostumeIdentityWithNoSurfaceActivationBlocked()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenCostumeIdentity("Bare_Armor", null);

            // When
            WhenGmSetsActiveDesignation("Bare_Armor");

            // Then
            ThenSetActiveBlocked("no costume surface");
            ThenNoGameCommandsIssued();
        }

        [TestMethod]
        public void ActiveIndicatorVisibleInUiAfterActivation()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenModelIdentity("Dragon_Model", "Skull_Lt_01");

            // When
            WhenGmSetsActiveDesignation("Dragon_Model");

            // Then
            ThenIdentityExistsWithState("Dragon_Model", "active", "unset");
        }
    }
}
