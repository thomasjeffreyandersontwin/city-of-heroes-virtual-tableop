using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityManagement
{
    [TestClass]
    public class AddIdentityToCharacter : IdentityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void UniqueNameProvidedIdentityAdded()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");

            // When
            WhenGmAddsIdentity("Knight_Armor", "Guard_Captain");

            // Then
            ThenIdentityExistsWithState("Knight_Armor", "inactive", "unset");
        }

        [TestMethod]
        public void DuplicateNameOnCharacterRejected()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Knight_Armor", "inactive", "unset");

            // When
            WhenGmAddsIdentity("Knight_Armor", "Guard_Captain");

            // Then
            ThenIdentityRejected("duplicate");
        }

        [TestMethod]
        public void EmptyNameProvidedRejected()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");

            // When
            WhenGmAddsIdentity("", "Guard_Captain");

            // Then
            ThenIdentityRejected("name");
        }

        [TestMethod]
        public void AddDisabledWhenNoCharacterSelected()
        {
            // Given
            GivenGameBridgeReady();
            GivenNoCharacterSelected();

            // Then
            ThenAddIdentityDisabled();
        }
    }
}
