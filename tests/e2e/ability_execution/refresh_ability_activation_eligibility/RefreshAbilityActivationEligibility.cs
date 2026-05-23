using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AbilityExecution
{
    [TestClass]
    public class RefreshAbilityActivationEligibility : AbilityExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void KeyAssignedNotExecutingCharacterSpawnedIsEligible()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "stopped");
            GivenAbilityWithKey("Fire Strike", "F1");

            // Then
            ThenEligibilityState("Fire Strike", "eligible");
        }

        [TestMethod]
        public void NoActivationKeyAssignedIsIneligible()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "stopped");

            // Then
            ThenEligibilityState("Fire Strike", "ineligible");
        }

        [TestMethod]
        public void AbilityCurrentlyExecutingIsIneligible()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenSpawnedNpcPresent("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "executing");
            GivenAbilityWithKey("Fire Strike", "F1");

            // Then
            ThenEligibilityState("Fire Strike", "ineligible");
        }

        [TestMethod]
        public void CharacterNotSpawnedIsIneligible()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenCharacterNotSpawned("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "stopped");
            GivenAbilityWithKey("Fire Strike", "F1");

            // Then
            ThenEligibilityState("Fire Strike", "ineligible");
        }

        [TestMethod]
        public void EligibilityRefreshesWhenConditionsChange()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterWithName("Guard_Captain");
            GivenCharacterNotSpawned("Guard_Captain");
            GivenAnimatedAbility("Fire Strike", "stopped");
            GivenAbilityWithKey("Fire Strike", "F1");

            // When
            WhenCharacterSpawned("Guard_Captain");

            // Then
            ThenEligibilityState("Fire Strike", "eligible");
        }
    }
}
