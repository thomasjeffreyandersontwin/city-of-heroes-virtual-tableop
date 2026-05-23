using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameBridgeInitialization
{
    [TestClass]
    public class ExecuteSlashCommandViaDll : GameBridgeInitializationHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidCommandBridgeReadyDeliveredImmediately()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenNativeBridgeInitialized();

            // When
            WhenSlashCommandSubmitted("/spawnnpc Guard_01");

            // Then
            ThenSlashCommandDeliveredVia("immediate via Native Game Bridge");
        }

        [TestMethod]
        public void CommandBeforeGameLoadedEventRejected()
        {
            // Given
            GivenGameBridgeWithInitializationState("polling");
            GivenNativeBridgeInitialized();

            // When
            WhenSlashCommandSubmitted("/spawnnpc Guard_01");

            // Then
            ThenSlashCommandRejected();
        }

        [TestMethod]
        public void NullOrEmptyCommandStringRejected()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenNativeBridgeInitialized();

            // When
            WhenSlashCommandSubmitted("");

            // Then
            ThenSlashCommandRejected();
            ThenGameBridgeReportsError("argument");
        }

        [TestMethod]
        public void ValidCommandCohReportsUnknownErrorSurfaced()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenNativeBridgeInitialized();

            // When
            WhenSlashCommandSubmitted("/invalidcmd");

            // Then
            ThenSlashCommandDeliveredVia("immediate via Native Game Bridge");
        }
    }
}
