using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameBridgeInitialization
{
    [TestClass]
    public class PollUntilGameClientIsLoaded : GameBridgeInitializationHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void PollReturnsGameLoadedTransitionsToReady()
        {
            // Given
            GivenGameBridgeWithInitializationState("polling");

            // When
            WhenGameBridgePollsGameState();

            // Then
            ThenGameBridgeHasInitializationState("ready");
            ThenGameLoadedEventHasPublicationState("published");
        }

        [TestMethod]
        public void PollReturnsNotReadyContinuesPolling()
        {
            // Given
            GivenGameBridgeWithInitializationState("polling");
            GivenPollWillReturnNotReady();

            // When
            WhenGameBridgePollsGameState();

            // Then
            ThenGameBridgeHasInitializationState("polling");
            ThenGameLoadedEventHasPublicationState("unpublished");
        }

        [TestMethod]
        public void PollingTimesOutRemainsPolling()
        {
            // Given
            GivenGameBridgeWithInitializationState("polling");
            GivenPollingWillTimeout();

            // When
            WhenGameBridgePollsGameState();

            // Then
            ThenGameBridgeHasInitializationState("polling");
            ThenGameLoadedEventHasPublicationState("unpublished");
        }

        [TestMethod]
        public void GameCommandAttemptedWhilePollingRejected()
        {
            // Given
            GivenGameBridgeWithInitializationState("polling");

            // When
            WhenSlashCommandSubmitted("/spawnnpc Guard_01");

            // Then
            ThenGameBridgeHasInitializationState("polling");
            ThenGameLoadedEventHasPublicationState("unpublished");
        }

        [TestMethod]
        public void AlreadyReadyRedundantNotReadyIgnored()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");

            // When
            WhenGameBridgePollsGameState();

            // Then
            ThenGameBridgeHasInitializationState("ready");
            ThenGameLoadedEventHasPublicationState("published");
        }
    }
}
