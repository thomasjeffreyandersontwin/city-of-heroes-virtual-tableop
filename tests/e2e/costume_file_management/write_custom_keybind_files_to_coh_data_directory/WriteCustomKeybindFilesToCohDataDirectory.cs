using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CostumeFileManagement
{
    [TestClass]
    public class WriteCustomKeybindFilesToCohDataDirectory : CostumeFileManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SuccessfulWriteKeybindFileCreated()
        {
            // Given
            GivenGameBridgeReady();
            GivenKeybindEntriesAssembled("F1 /spawnnpc Guard Skull_Lt_01");

            // When
            WhenGameBridgeWritesKeybindFile();

            // Then
            ThenKeybindFileExistsAt(@"C:\Games\CoH\data\hvt_cmd.txt",
                "F1 /spawnnpc Guard Skull_Lt_01");
        }

        [TestMethod]
        public void FileAlreadyExistsOverwrittenWithCurrentEntries()
        {
            // Given
            GivenGameBridgeReady();
            GivenKeybindFileAt(@"C:\Games\CoH\data\hvt_cmd.txt", "old_entries");
            GivenKeybindEntriesAssembled("F1 /target_name Guard");

            // When
            WhenGameBridgeWritesKeybindFile();

            // Then
            ThenKeybindFileExistsAt(@"C:\Games\CoH\data\hvt_cmd.txt", "F1 /target_name Guard");
        }

        [TestMethod]
        public void DataDirectoryDoesNotExistReportsError()
        {
            // Given
            GivenGameBridgeReady();
            GivenDataDirectoryMissing();
            GivenKeybindEntriesAssembled("F1 /spawnnpc Guard");

            // When
            WhenGameBridgeWritesKeybindFile();

            // Then
            ThenErrorReported("directory not found");
        }

        [TestMethod]
        public void KeybindFileWriteFailsReportsError()
        {
            // Given
            GivenGameBridgeReady();
            GivenKeybindEntriesAssembled("F1 /spawnnpc Guard");

            // When
            WhenWriteFails();

            // Then
            ThenErrorReported("write failure");
        }
    }
}
