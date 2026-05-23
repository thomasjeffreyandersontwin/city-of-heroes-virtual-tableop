using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CharacterMovementAuthoring
{
    [TestClass]
    public class RemoveMovementFromCharacter : CharacterMovementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void RemoveNonDefaultMovement()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Sprint", "Walk");
            GivenMovementWithDefaultDesignation("Sprint", "unset");
            GivenMovementWithActivationKey("Sprint", "S");

            // When
            WhenGmRemovesMovement("Sprint");

            // Then
            ThenMovementNotInList("Sprint");
            ThenMovementKeyFreed("S");
        }

        [TestMethod]
        public void RemoveTheDefaultMovement()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Walk", "Walk");
            GivenMovementWithDefaultDesignation("Walk", "default");
            GivenMovementWithActivationKey("Walk", "W");

            // When
            WhenGmRemovesMovement("Walk");

            // Then
            ThenMovementNotInList("Walk");
        }

        [TestMethod]
        public void NoMovementSelectedRemoveDisabled()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");

            // Then
            ThenRemoveActionDisabled();
        }
    }
}
