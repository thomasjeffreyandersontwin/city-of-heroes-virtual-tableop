using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CharacterMovementAuthoring
{
    [TestClass]
    public class EditMovementParameters : CharacterMovementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ChangeMovementTypeToFly()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Sprint", "Walk");

            // When
            WhenGmEditsMovementAndSaves("Sprint", "Fly", "100", "F");

            // Then
            ThenMovementExists("Sprint", "Fly");
            ThenMovementHasKey("Sprint", "F");
        }

        [TestMethod]
        public void SetDistanceLimit()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Sprint", "Walk");

            // When
            WhenGmEditsMovementAndSaves("Sprint", "Walk", "50", "unset");

            // Then
            ThenMovementExists("Sprint", "Walk");
        }

        [TestMethod]
        public void CancelWithoutSaving()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Sprint", "Walk");

            // When
            WhenGmCancelsMovementEditor();

            // Then
            ThenMovementExists("Sprint", "Walk");
        }

        [TestMethod]
        public void SaveWithEmptyNameRejected()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Sprint", "Walk");

            // When
            WhenGmEditsMovementAndSaves("Sprint", "Walk", "absent", "unset");

            // Then
            ThenValidationErrorShown("name");
        }
    }
}
