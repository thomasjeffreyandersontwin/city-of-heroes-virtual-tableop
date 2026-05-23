using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AbilityExecution
{
    [TestClass]
    public class StopActiveAbility : AbilityExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void StopHaltsExecutingAbility()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "executing");

            // When
            WhenGmStopsAbility("Fire Strike");

            // Then
            ThenAbilityHasExecutionState("Fire Strike", "stopped");
        }

        [TestMethod]
        public void StopWhenNothingExecuting()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "stopped");

            // When
            WhenGmStopsAbility("Fire Strike");

            // Then
            ThenAbilityHasExecutionState("Fire Strike", "stopped");
        }

        [TestMethod]
        public void StopPersistentAbilityDoesNotClearPersistence()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenAnimatedAbility("Fire Aura", "executing");
            GivenAbilityPersistent("Fire Aura");

            // When
            WhenGmStopsAbility("Fire Aura");

            // Then
            ThenAbilityHasExecutionState("Fire Aura", "stopped");
            ThenAbilityPersistence("Fire Aura", "persistent");
        }

        [TestMethod]
        public void StopMidPauseElement()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "executing");
            GivenPauseElementActive("5 seconds");

            // When
            WhenGmStopsAbility("Fire Strike");

            // Then
            ThenStopCompletesImmediately();
            ThenAbilityHasExecutionState("Fire Strike", "stopped");
        }
    }
}
