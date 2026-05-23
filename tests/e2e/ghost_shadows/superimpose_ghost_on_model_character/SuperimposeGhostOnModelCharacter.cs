using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GhostShadows
{
    [TestClass]
    public class SuperimposeGhostOnModelCharacter : GhostShadowsHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ActiveModelIdentityWithBackupGhostActivated()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenModelIdentityActive("Dragon_Model", "Skull_Lt_01");
            GivenOriginalBackupAt(@"C:\Games\CoH\costumes\guard_original.costume");

            // When
            WhenGmChoosesAddGhost("Guard_Captain");

            // Then
            ThenGhostShadowState("Guard_Captain", "active");
        }

        [TestMethod]
        public void OriginalBackupMissingGhostInactive()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenModelIdentityActive("Dragon_Model", "Skull_Lt_01");
            GivenNoOriginalBackup(@"C:\Games\CoH\costumes\guard_original.costume");

            // When
            WhenGmChoosesAddGhost("Guard_Captain");

            // Then
            ThenGhostShadowState("Guard_Captain", "inactive");
            ThenErrorReported("no original backup found");
        }

        [TestMethod]
        public void GhostShadowOnCostumeIdentityDisabled()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenCostumeIdentityActive("Knight_Armor");

            // When
            WhenGmChoosesAddGhost("Guard_Captain");

            // Then
            ThenAddGhostDisabled("Costume Identity");
        }

        [TestMethod]
        public void GhostShadowOnUnspawnedCharacterBlocked()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenIdentityInactive("Dragon_Model");

            // When
            WhenGmChoosesAddGhost("Guard_Captain");

            // Then
            ThenAddGhostDisabled("character not spawned");
        }

        [TestMethod]
        public void GhostIndicatorShownAfterActivation()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenGhostShadowActive("Guard_Captain");
            GivenGhostNpcPresent("Guard_Captain_Ghost");

            // Then
            ThenGhostIndicatorShown("Dragon_Model");
        }
    }
}
