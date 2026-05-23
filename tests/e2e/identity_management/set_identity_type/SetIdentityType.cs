using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityManagement
{
    [TestClass]
    public class SetIdentityType : IdentityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SetTypeToModelConfiguresAsModelIdentity()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Knight_Armor", "inactive", "unset");

            // When
            WhenGmSetsIdentityType("Knight_Armor", "Model");

            // Then
            ThenIdentityListShowsTypeIndicator("Knight_Armor", "Model");
            ThenCostumeSurfaceCleared("Knight_Armor");
        }

        [TestMethod]
        public void SetTypeToCostumeConfiguresAsCostumeIdentity()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Knight_Armor", "inactive", "unset");

            // When
            WhenGmSetsIdentityType("Knight_Armor", "Costume");

            // Then
            ThenIdentityListShowsTypeIndicator("Knight_Armor", "Costume");
            ThenModelNameCleared("Knight_Armor");
        }

        [TestMethod]
        public void TypeChangeOnActiveIdentityRequiresDespawnConfirmation()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Knight_Armor", "active", "unset");
            GivenSpawnedNpc("Guard_Captain", "present");

            // When
            WhenGmSetsIdentityType("Knight_Armor", "Model");

            // Then
            ThenDespawnConfirmationShown();
        }

        [TestMethod]
        public void TypeConfirmedUpdatesCharacterData()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Knight_Armor", "inactive", "unset");

            // When
            WhenGmSetsIdentityType("Knight_Armor", "Model");

            // Then
            ThenIdentityListShowsTypeIndicator("Knight_Armor", "Model");
        }
    }
}
