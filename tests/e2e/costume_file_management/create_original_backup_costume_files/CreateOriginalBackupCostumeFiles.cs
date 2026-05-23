using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CostumeFileManagement
{
    [TestClass]
    public class CreateOriginalBackupCostumeFiles : CostumeFileManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void FirstModificationBackupMade()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenCostumeFileAt(@"C:\Games\CoH\costumes\guard.costume", "original_body_data");

            // When
            WhenHvtCreatesOriginalBackup("Guard_Captain");

            // Then
            ThenOriginalBackupExistsAt(@"C:\Games\CoH\costumes\guard_original.costume");
        }

        [TestMethod]
        public void BackupAlreadyExistsNotOverwritten()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenCostumeFileAt(@"C:\Games\CoH\costumes\guard.costume", "modified_data");
            GivenOriginalBackupCostumeFileAt(@"C:\Games\CoH\costumes\guard_original.costume");

            // When
            WhenHvtCreatesOriginalBackup("Guard_Captain");

            // Then
            ThenOriginalBackupNotOverwritten(@"C:\Games\CoH\costumes\guard_original.costume",
                "original_costume_data");
        }

        [TestMethod]
        public void BackupWriteFailsModificationHalted()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenCostumeFileAt(@"C:\Games\CoH\costumes\guard.costume", "original_data");

            // When
            WhenWriteFails();

            // Then
            ThenErrorReported("backup");
            ThenCostumeFileHasData(@"C:\Games\CoH\costumes\guard.costume", "original_data");
        }

        [TestMethod]
        public void NoPriorCostumeExistsBackupSkipped()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenNoCostumeFileFor("Guard_Captain");

            // When
            WhenHvtCreatesOriginalBackup("Guard_Captain");

            // Then
            ThenNoBackupCreated(@"C:\Games\CoH\costumes\guard_captain_original.costume");
        }
    }
}
