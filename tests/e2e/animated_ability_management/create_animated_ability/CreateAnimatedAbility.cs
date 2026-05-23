using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimatedAbilityManagement
{
    [TestClass]
    public class CreateAnimatedAbility : AnimatedAbilityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void NewAbilityCreatedOnCharacter()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");

            // When
            WhenGmCreatesAbility("Guard_Captain", "Fire Strike");

            // Then
            ThenAbilityExistsWithState("Fire Strike", "(unset)", "non-persistent", "unset");
        }

        [TestMethod]
        public void DuplicateNameRejected()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");

            // When
            WhenGmCreatesAbility("Guard_Captain", "Fire Strike");

            // Then
            ThenValidationErrorShown("unique");
        }

        [TestMethod]
        public void NoCharacterSelectedActionDisabled()
        {
            // Given
            GivenNoCharacterSelected();

            // Then
            ThenCreateActionDisabled();
        }
    }
}
