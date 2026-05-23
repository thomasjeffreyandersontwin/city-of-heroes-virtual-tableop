using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CharacterMovementAuthoring
{
    [TestClass]
    public class SetMovementActivationKey : CharacterMovementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void AssignKeyFToSprint()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Sprint", "Walk");

            // When
            WhenGmAssignsMovementKey("Sprint", "F");

            // Then
            ThenMovementHasKey("Sprint", "F");
        }

        [TestMethod]
        public void KeyFAlreadyUsedRejected()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Sprint", "Walk");
            GivenCharacterMovementExists("Run", "Run");
            GivenMovementWithActivationKey("Sprint", "F");

            // When
            WhenGmAssignsMovementKey("Run", "F");

            // Then
            ThenValidationErrorShown("conflict");
        }

        [TestMethod]
        public void ClearActivationKey()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Sprint", "Walk");
            GivenMovementWithActivationKey("Sprint", "F");

            // When
            WhenGmAssignsMovementKey("Sprint", "unset");

            // Then
            ThenMovementHasKey("Sprint", "unset");
        }
    }
}
