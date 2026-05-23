using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimatedAbilityManagement
{
    [TestClass]
    public class SetDefaultAbilityForCharacter : AnimatedAbilityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SetNewDefault()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Recovery");

            // When
            WhenGmSetsDefault("Recovery");

            // Then
            ThenAbilityHasDefault("Recovery", "default");
        }

        [TestMethod]
        public void DefaultAbilityAutoPlaysOnSpawn()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Recovery");
            GivenAbilityDefault("Recovery", "default");

            // When
            WhenCharacterSpawned("Guard_Captain");

            // Then
            ThenAbilityHasExecutionState("Recovery", "executing");
        }

        [TestMethod]
        public void DefaultAbilityRemovedFromCharacter()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Recovery");
            GivenAbilityDefault("Recovery", "default");

            // When
            WhenAbilityRemoved("Recovery", "Guard_Captain");

            // Then
            ThenNoDefaultAbilityOnCharacter();
        }

        [TestMethod]
        public void ClearDefaultDesignation()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenAnimatedAbility("Guard_Captain", "Recovery");
            GivenAbilityDefault("Recovery", "default");

            // When
            WhenGmClearsDefault("Recovery");

            // Then
            ThenNoDefaultAbilityOnCharacter();
        }
    }
}
