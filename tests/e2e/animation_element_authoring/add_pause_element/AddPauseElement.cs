using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimationElementAuthoring
{
    [TestClass]
    public class AddPauseElement : AnimationElementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void NormalPauseElementCreated()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");

            // When
            WhenGmAddsPauseElement("2 seconds");

            // Then
            ThenElementExists("Pause", "2 seconds");
            ThenElementAtBottom();
        }

        [TestMethod]
        public void ZeroDurationPauseElementCreated()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");

            // When
            WhenGmAddsPauseElement("0 seconds");

            // Then
            ThenElementExists("Pause", "0 seconds");
            ThenElementAtBottom();
        }

        [TestMethod]
        public void PauseElementBlocksProgressionDuringPlay()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenPauseElement("2 seconds", 1);

            // When
            WhenAbilityExecutesElement("Pause", "2 seconds");

            // Then
            ThenPauseBlocksFor("2 seconds");
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void ZeroDurationPauseIsNoOp()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenPauseElement("0 seconds", 1);

            // When
            WhenAbilityExecutesElement("Pause", "0 seconds");

            // Then
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void AbilityStoppedMidPause()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenPauseElement("5 seconds", 1);

            // When
            WhenGmStopsAbility();

            // Then
            ThenStopCompletesImmediately();
        }
    }
}
