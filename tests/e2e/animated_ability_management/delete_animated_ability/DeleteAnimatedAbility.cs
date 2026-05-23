using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimatedAbilityManagement
{
    [TestClass]
    public class DeleteAnimatedAbility : AnimatedAbilityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void AbilityAndElementsPermanentlyRemoved()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");

            // When
            WhenGmDeletesAbility("Fire Strike");

            // Then
            ThenAbilityNotInList("Fire Strike");
        }

        [TestMethod]
        public void DeletedAbilityWasTheDefault()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");
            GivenAbilityDefault("Fire Strike", "default");

            // When
            WhenGmDeletesAbility("Fire Strike");

            // Then
            ThenNoDefaultAbilityOnCharacter();
        }

        [TestMethod]
        public void DeletedAbilityIsCurrentlyExecuting()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");
            GivenAbilityExecutionState("Fire Strike", "executing");

            // When
            WhenGmDeletesAbility("Fire Strike");

            // Then
            ThenAbilityNotInList("Fire Strike");
        }

        [TestMethod]
        public void ReferenceElementPointsToDeletedAbility()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Fire Strike");
            GivenAnimatedAbility("Guard_Captain", "Combo Strike");
            GivenReferenceElementPointsTo("Combo Strike", "Fire Strike");

            // When
            WhenGmDeletesAbility("Fire Strike");

            // Then
            ThenAbilityNotInList("Fire Strike");
        }
    }
}
