using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameBridgeInitialization
{
    [TestClass]
    public class InjectRequiredKeybindsIntoGame : GameBridgeInitializationHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SuccessfulInjectionWritesKeybindFile()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenGameLoadedEventPublished();

            // When
            WhenGameBridgeInjectsRequiredKeybinds(@"C:\Games\CoH");

            // Then
            ThenKeybindFileExistsAt(@"C:\Games\CoH\data\hvt_binds.txt");
        }

        [TestMethod]
        public void ReInjectionInSameSessionRefreshesBindings()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenGameLoadedEventPublished();

            // When
            WhenGameBridgeInjectsRequiredKeybinds(@"C:\Games\CoH");

            // Then
            ThenKeybindFileExistsAt(@"C:\Games\CoH\data\hvt_binds.txt");
        }

        [TestMethod]
        public void KeybindFileWriteFailsReportsError()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenGameLoadedEventPublished();

            // When
            WhenKeybindFileWriteFails();

            // Then
            ThenGameBridgeReportsError("keybind injection failure");
        }

        [TestMethod]
        public void KeybindFileLoadCommandFailsReportsError()
        {
            // Given
            GivenGameBridgeWithInitializationState("ready");
            GivenGameLoadedEventPublished();

            // When
            WhenBindLoadFileCommandFails();

            // Then
            ThenGameBridgeReportsError("keybinds could not be loaded");
        }
    }
}
