using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimatedAbilityManagement
{
    [TestClass]
    public class EditAnimatedAbility : AnimatedAbilityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void EditOpensPrePopulatedAbilityEditor()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");

            // When
            WhenGmEditsAbility("Fire Strike");

            // Then
            ThenAbilityEditorOpen();
        }

        [TestMethod]
        public void SaveAppliesChanges()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");
            WhenGmEditsAbility("Fire Strike");

            // When
            WhenGmSavesAbilityEditor();

            // Then
            ThenAbilityEditorClosed();
        }

        [TestMethod]
        public void CancelDiscardsChanges()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");
            WhenGmEditsAbility("Fire Strike");

            // When
            WhenGmCancelsAbilityEditor();

            // Then
            ThenAbilityEditorClosed();
        }

        [TestMethod]
        public void DuplicateNameOnSaveRejected()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");
            GivenAnimatedAbility("Guard_Captain", "Ice Shield");
            WhenGmEditsAbility("Fire Strike");

            // When — attempt to save with duplicate name
            WhenGmSavesAbilityEditor();

            // Then
            ThenAbilityEditorOpen();
        }

        [TestMethod]
        public void SuccessfulSaveClosesEditor()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");
            WhenGmEditsAbility("Fire Strike");

            // When
            WhenGmSavesAbilityEditor();

            // Then
            ThenAbilityEditorClosed();
        }
    }
}
