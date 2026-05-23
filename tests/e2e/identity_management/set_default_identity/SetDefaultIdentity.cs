using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityManagement
{
    [TestClass]
    public class SetDefaultIdentity : IdentityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SetNewDefaultClearsPreviousDefault()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Knight_Armor", "inactive", "unset");
            GivenIdentityOnCharacter("Guard_Captain", "Shadow_Form", "inactive", "default");

            // When
            WhenGmSetsDefaultDesignation("Knight_Armor");

            // Then
            ThenIdentityExistsWithState("Knight_Armor", "inactive", "default");
            ThenIdentityExistsWithState("Shadow_Form", "inactive", "unset");
        }

        [TestMethod]
        public void ClearDefaultSetToNone()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Shadow_Form", "inactive", "default");

            // When
            WhenGmRemovesDefaultDesignation("Shadow_Form");

            // Then
            ThenIdentityExistsWithState("Shadow_Form", "inactive", "unset");
        }

        [TestMethod]
        public void SetDefaultDisabledWhenNoIdentitiesExist()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenNoIdentitiesOnCharacter("Guard_Captain");

            // Then
            ThenSetDefaultDisabled();
        }

        [TestMethod]
        public void DefaultPersistsAcrossSessions()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Knight_Armor", "inactive", "default");

            // When
            WhenSessionRestarts();

            // Then
            ThenIdentityExistsWithState("Knight_Armor", "inactive", "default");
        }
    }
}
