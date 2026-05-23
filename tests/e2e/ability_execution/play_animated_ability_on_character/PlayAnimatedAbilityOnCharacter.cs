using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AbilityExecution
{
    [TestClass]
    public class PlayAnimatedAbilityOnCharacter : AbilityExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void CharacterSpawnedPlayBegins()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "stopped");

            // When
            WhenGmPlaysAbility("Fire Strike");

            // Then
            ThenAbilityHasExecutionState("Fire Strike", "executing");
        }

        [TestMethod]
        public void AllElementsCompleteAbilityStops()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "executing");

            // When
            WhenAllElementsComplete("Fire Strike");

            // Then
            ThenAbilityHasExecutionState("Fire Strike", "stopped");
        }

        [TestMethod]
        public void PlayBlockedWhenCharacterNotSpawned()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenNoSpawnedNpc("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "stopped");

            // When
            WhenGmPlaysAbility("Fire Strike");

            // Then
            ThenPlayBlocked();
            ThenNoGameCommandIssued();
        }

        [TestMethod]
        public void AnotherAbilityAlreadyExecutingStopsFirst()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Ice Shield", "executing");
            GivenAnimatedAbility("Fire Strike", "stopped");

            // When
            WhenGmPlaysAbility("Fire Strike");

            // Then
            ThenAbilityHasExecutionState("Ice Shield", "stopped");
            ThenAbilityHasExecutionState("Fire Strike", "executing");
        }
    }
}
