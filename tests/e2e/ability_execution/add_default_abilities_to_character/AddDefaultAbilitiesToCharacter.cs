using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AbilityExecution
{
    [TestClass]
    public class AddDefaultAbilitiesToCharacter : AbilityExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void DefaultAbilitiesAddedToEmptyCharacter()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenEmptyAbilitiesOptionGroup("Guard_Captain");

            // When
            WhenAddDefaultAbilitiesApplied("Guard_Captain");

            // Then
            ThenDefaultAbilitiesPresent();
        }

        [TestMethod]
        public void DefaultAbilitiesConfiguration()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenEmptyAbilitiesOptionGroup("Guard_Captain");

            // When
            WhenAddDefaultAbilitiesApplied("Guard_Captain");

            // Then
            ThenAbilityCount(20);
        }

        [TestMethod]
        public void DuplicateNamesNotReAdded()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenAbilityAlreadyExists("Guard_Captain", "Recovery");

            // When
            WhenAddDefaultAbilitiesApplied("Guard_Captain");

            // Then
            ThenAbilityCount(20);
        }

        [TestMethod]
        public void AllTwentyAddedToFreshCharacter()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenEmptyAbilitiesOptionGroup("Guard_Captain");

            // When
            WhenAddDefaultAbilitiesApplied("Guard_Captain");

            // Then
            ThenAbilityCount(20);
            ThenDefaultAbilitiesPresent();
        }
    }
}
