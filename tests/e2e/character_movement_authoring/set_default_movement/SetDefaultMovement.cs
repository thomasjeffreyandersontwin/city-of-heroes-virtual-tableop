using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CharacterMovementAuthoring
{
    [TestClass]
    public class SetDefaultMovement : CharacterMovementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SetSprintAsDefault()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenTwoMovements("Walk", "Sprint");

            // When
            WhenGmSetsDefaultMovement("Sprint");

            // Then
            ThenMovementHasDefault("Sprint", "default");
        }

        [TestMethod]
        public void PreviousDefaultWalkCleared()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenTwoMovements("Walk", "Sprint");
            GivenMovementWithDefaultDesignation("Walk", "default");

            // When
            WhenGmSetsDefaultMovement("Sprint");

            // Then
            ThenMovementHasDefault("Walk", "unset");
            ThenMovementHasDefault("Sprint", "default");
        }

        [TestMethod]
        public void RemoveDefaultWithoutReplacement()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenTwoMovements("Walk", "Sprint");
            GivenMovementWithDefaultDesignation("Walk", "default");

            // When
            WhenGmSetsDefaultMovement("Walk");

            // Then
            ThenMovementHasDefault("Walk", "unset");
        }
    }
}
