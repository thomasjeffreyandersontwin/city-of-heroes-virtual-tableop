using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimationElementAuthoring
{
    [TestClass]
    public class AddSequenceElement : AnimationElementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void AddAndSequenceElement()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");

            // When
            WhenGmAddsSequenceElement("And");

            // Then
            ThenSequenceElementHasType("And");
            ThenElementAtBottom();
        }

        [TestMethod]
        public void AddOrSequenceElement()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");

            // When
            WhenGmAddsSequenceElement("Or");

            // Then
            ThenSequenceElementHasType("Or");
            ThenElementAtBottom();
        }

        [TestMethod]
        public void AndSequenceExecutesAllChildrenInOrder()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenSequenceElement("And", 3);

            // When
            WhenSequenceElementExecutes("And");

            // Then
            ThenAllChildrenExecutedInOrder();
        }

        [TestMethod]
        public void OrSequenceExecutesOneChildAtRandom()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenSequenceElement("Or", 3);

            // When
            WhenSequenceElementExecutes("Or");

            // Then
            ThenExactlyOneChildExecuted();
        }

        [TestMethod]
        public void EmptySequenceElementAtExecutionTime()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenSequenceElement("And", 0);

            // When
            WhenSequenceElementExecutes("And");

            // Then
            ThenElementProducesNoOp();
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void ExecutionTypeChangedOnExistingElement()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenSequenceElement("And", 2);

            // When
            WhenGmChangesSequenceType("Or");

            // Then
            ThenSequenceElementHasType("Or");
        }
    }
}
