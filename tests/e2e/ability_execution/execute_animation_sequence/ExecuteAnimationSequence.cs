using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AbilityExecution
{
    [TestClass]
    public class ExecuteAnimationSequence : AbilityExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void AndSequenceAllChildrenExecuteInOrder()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "executing");
            GivenSequenceElement("And", 3);

            // When
            WhenSequenceExecutes("And");

            // Then
            ThenAllChildrenExecutedInOrder();
        }

        [TestMethod]
        public void OrSequenceOneChildAtRandom()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "executing");
            GivenSequenceElement("Or", 3);

            // When
            WhenSequenceExecutes("Or");

            // Then
            ThenExactlyOneChildExecuted();
        }

        [TestMethod]
        public void OrSequenceWithExactlyOneChild()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "executing");
            GivenSequenceElement("Or", 1);

            // When
            WhenSequenceExecutes("Or");

            // Then
            ThenExactlyOneChildExecuted();
        }

        [TestMethod]
        public void NestedSequenceElements()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "executing");
            GivenSequenceElement("And", 2);

            // When
            WhenSequenceExecutes("And");

            // Then
            ThenAllChildrenExecutedInOrder();
        }
    }
}
