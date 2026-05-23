using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameBridgeInitialization
{
    [TestClass]
    public class PublishGameLoadedEvent : GameBridgeInitializationHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void FirstReadyConfirmationPublishesEvent()
        {
            // Given
            GivenGameBridgeWithInitializationState("polling");

            // When
            WhenGameBridgePollingLoopConfirmsClientStatus();

            // Then
            ThenGameLoadedEventHasPublicationState("published");
        }

        [TestMethod]
        public void AlreadyPublishedSecondReadyPollNoSecondEvent()
        {
            // Given
            GivenGameBridgeWithInitializationState("polling");
            GivenGameLoadedEventPublished();

            // When
            WhenGameBridgePollingLoopConfirmsClientStatus();

            // Then
            ThenGameLoadedEventHasPublicationState("published");
        }

        [TestMethod]
        public void LateSubscriberAfterPublicationReceivesEvent()
        {
            // Given
            GivenGameBridgeWithInitializationState("polling");
            GivenGameLoadedEventPublished();

            // When
            WhenGameBridgePollingLoopConfirmsClientStatus();

            // Then
            ThenGameLoadedEventHasPublicationState("published");
        }

        [TestMethod]
        public void PollingTimedOutEventNotPublished()
        {
            // Given
            GivenGameBridgeWithInitializationState("polling");
            GivenPollingWillTimeout();

            // When
            WhenGameBridgePollingLoopConfirmsClientStatus();

            // Then
            ThenGameLoadedEventHasPublicationState("unpublished");
        }
    }
}
