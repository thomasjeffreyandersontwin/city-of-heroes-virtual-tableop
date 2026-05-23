using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CostumeFileManagement
{
    [TestClass]
    public class StoreCostumeFilesInCohCostumesDirectory : CostumeFileManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SuccessfulWriteCostumeFileCreated()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenCostumeIdentityWithSurface(@"C:\Games\CoH\costumes\guard.costume");

            // When
            WhenHvtWritesCostumeFile("Guard_Captain");

            // Then
            ThenCostumeFileExistsAt(@"C:\Games\CoH\costumes\guard.costume");
        }

        [TestMethod]
        public void FileAlreadyExistsOverwritten()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenCostumeFileAt(@"C:\Games\CoH\costumes\guard.costume", "old_data");
            GivenCostumeIdentityWithSurface(@"C:\Games\CoH\costumes\guard.costume");

            // When
            WhenHvtWritesCostumeFile("Guard_Captain");

            // Then
            ThenCostumeFileExistsAt(@"C:\Games\CoH\costumes\guard.costume");
        }

        [TestMethod]
        public void DirectoryMissingCreatedBeforeWrite()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectoryMissing(@"C:\Games\CoH\costumes");
            GivenCostumeIdentityWithSurface(@"C:\Games\CoH\costumes\guard.costume");

            // When
            WhenHvtWritesCostumeFile("Guard_Captain");

            // Then
            ThenDirectoryCreated(@"C:\Games\CoH\costumes");
            ThenCostumeFileExistsAt(@"C:\Games\CoH\costumes\guard.costume");
        }

        [TestMethod]
        public void DirectoryReadOnlyWriteFailsReportsError()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenCohCostumesDirectoryReadOnly(@"C:\Games\CoH\costumes");

            // When
            WhenHvtWritesCostumeFile("Guard_Captain");

            // Then
            ThenErrorReported("file write error");
        }
    }
}
