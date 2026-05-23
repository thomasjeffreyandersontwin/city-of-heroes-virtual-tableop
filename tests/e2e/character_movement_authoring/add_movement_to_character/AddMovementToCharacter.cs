using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CharacterMovementAuthoring
{
    [TestClass]
    public class AddMovementToCharacter : CharacterMovementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void NewMovementAdded()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");

            // When
            WhenGmAddsMovement("Sprint");

            // Then
            ThenMovementExists("Sprint", "Walk");
        }

        [TestMethod]
        public void DuplicateNameRejected()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Sprint", "Walk");

            // When
            WhenGmAddsMovement("Sprint");

            // Then
            ThenMovementRejected();
        }

        [TestMethod]
        public void NoCharacterSelectedActionDisabled()
        {
            // Given
            GivenNoCharacterSelected();

            // Then
            ThenAddActionDisabled();
        }
    }
}
