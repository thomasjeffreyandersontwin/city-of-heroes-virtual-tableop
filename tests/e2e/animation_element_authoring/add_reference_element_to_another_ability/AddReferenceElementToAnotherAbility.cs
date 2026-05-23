using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimationElementAuthoring
{
    [TestClass]
    public class AddReferenceElementToAnotherAbility : AnimationElementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ReferenceElementAdded()
        {
            // Given
            GivenAbilityOpenInEditor("Combo Strike");
            GivenAnotherAbilityOnCharacter("Fire Strike");

            // When
            WhenGmAddsReferenceElement("Fire Strike");

            // Then
            ThenElementExists("Reference", "Fire Strike");
            ThenElementAtBottom();
        }

        [TestMethod]
        public void ReferenceElementExecutedInline()
        {
            // Given
            GivenAbilityOpenInEditor("Combo Strike");
            GivenAnotherAbilityOnCharacter("Fire Strike");
            GivenReferenceElement("Fire Strike");

            // When
            WhenAbilityExecutesElement("Reference", "Fire Strike");

            // Then
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void SelfReferenceRejected()
        {
            // Given
            GivenAbilityOpenInEditor("Combo Strike");

            // When
            WhenGmAddsReferenceElement("Combo Strike");

            // Then
            ThenValidationRejected("self");
            ThenNoElementAdded();
        }

        [TestMethod]
        public void ReferencedAbilityDoesNotExistAtExecutionTime()
        {
            // Given
            GivenAbilityOpenInEditor("Combo Strike");
            GivenReferenceElement("Deleted_Ability");

            // When
            WhenAbilityExecutesElement("Reference", "Deleted_Ability");

            // Then
            ThenElementProducesNoOp();
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void CircularReferenceChainRejectedAtSave()
        {
            // Given
            GivenAbilityOpenInEditor("Combo Strike");
            GivenAnotherAbilityOnCharacter("Fire Strike");
            GivenAbilityHasReferenceToSelf("Fire Strike", "Combo Strike");

            // When
            WhenGmAddsReferenceElement("Fire Strike");
            WhenGmSaves();

            // Then
            ThenValidationRejected("circular");
        }
    }
}
