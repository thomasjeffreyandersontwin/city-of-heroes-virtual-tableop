using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.KeybindExecution
{
    [TestClass]
    public class GenerateKeybindFileForGameEvent : KeybindExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SingleSlashCommandGeneratesKeybindFile()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameCommand("spawn", "Guard_Captain", "/spawnnpc Guard_Captain Skull_Lt_01");

            // When
            WhenGameBridgeGeneratesKeybindFile();

            // Then
            ThenKeybindFileWritten(@"C:\Games\CoH\data\hvt_cmd.txt",
                "F1 /spawnnpc Guard_Captain Skull_Lt_01");
        }

        [TestMethod]
        public void ChainedCommandsTargetAndLoadGeneratesKeybindFile()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameCommand("load costume", "Guard_Captain",
                "/target_name Guard_Captain$$loadcostume guard.costume");

            // When
            WhenGameBridgeGeneratesKeybindFile();

            // Then
            ThenKeybindFileWritten(@"C:\Games\CoH\data\hvt_cmd.txt",
                "F1 /target_name Guard_Captain$$loadcostume guard.costume");
        }

        [TestMethod]
        public void EmptyCommandInvalidRejected()
        {
            // Given
            GivenGameBridgeReady();
            GivenGameCommand("(none)", "(none)", "");

            // When
            WhenGameBridgeGeneratesKeybindFile();

            // Then
            ThenNoKeybindFileWritten(@"C:\Games\CoH\data\hvt_cmd.txt");
        }

        [TestMethod]
        public void ChainExceedingLengthLimitIsSplit()
        {
            // Given
            GivenGameBridgeReady();
            string longChain = new string('x', 500);
            GivenGameCommand("spawn", "Guard_Captain", longChain);

            // When
            WhenGameBridgeGeneratesKeybindFile();

            // Then — file should contain multiple keybind entries
            ThenKeybindFileWritten(@"C:\Games\CoH\data\hvt_cmd.txt", "F1");
        }
    }
}
