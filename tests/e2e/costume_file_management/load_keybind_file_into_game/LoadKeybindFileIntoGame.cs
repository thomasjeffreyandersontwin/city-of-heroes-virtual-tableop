using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CostumeFileManagement
{
    [TestClass]
    public class LoadKeybindFileIntoGame : CostumeFileManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void FileExistsCommandsExecuteProcessedByCoh()
        {
            // Given
            GivenGameBridgeReady();
            GivenNativeBridgeInitialized();
            GivenKeybindFileAt(@"C:\Games\CoH\data\hvt_cmd.txt",
                "F1 /spawnnpc Guard_Captain Skull_Lt_01");

            // When
            WhenGameBridgeIssuesBindLoadFile(@"C:\Games\CoH\data\hvt_cmd.txt");

            // Then
            ThenKeybindFileProcessedByCoh(@"C:\Games\CoH\data\hvt_cmd.txt");
        }

        [TestMethod]
        public void ChainedCommandsInEntryExecutedSequentially()
        {
            // Given
            GivenGameBridgeReady();
            GivenNativeBridgeInitialized();
            GivenKeybindFileAt(@"C:\Games\CoH\data\hvt_cmd.txt",
                "F1 /target_name Guard$$loadcostume guard.costume");

            // When
            WhenGameBridgeIssuesBindLoadFile(@"C:\Games\CoH\data\hvt_cmd.txt");

            // Then
            ThenKeybindFileProcessedByCoh(@"C:\Games\CoH\data\hvt_cmd.txt");
        }

        [TestMethod]
        public void KeybindFileDoesNotExistAtLoadTimeFailureSurfaced()
        {
            // Given
            GivenGameBridgeReady();
            GivenNativeBridgeInitialized();
            GivenNoKeybindFileAt(@"C:\Games\CoH\data\missing.txt");

            // When
            WhenGameBridgeIssuesBindLoadFile(@"C:\Games\CoH\data\missing.txt");

            // Then
            ThenErrorReported("load failure");
        }

        [TestMethod]
        public void LoadInstructionWhenBridgeNotReadyRejected()
        {
            // Given
            GivenGameBridgeReady();
            Driver.SetGameBridgeState("polling");

            // When
            WhenGameBridgeIssuesBindLoadFile(@"C:\Games\CoH\data\hvt_cmd.txt");

            // Then
            ThenErrorReported("not ready");
        }
    }
}
