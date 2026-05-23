using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GhostShadows
{
    [TestClass]
    public class CreateGhostCostumeFileFromOriginal : GhostShadowsHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SuccessfulGenerationGhostCostumeCreated()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenOriginalBackupAt(@"C:\Games\CoH\costumes\guard_original.costume");

            // When
            WhenHvtGeneratesGhostCostumeFile(@"C:\Games\CoH\costumes\guard_original.costume");

            // Then
            ThenGhostCostumeFileExists(@"C:\Games\CoH\costumes\guard_ghost.costume",
                "guard_ghost.costume");
        }

        [TestMethod]
        public void OriginalDoesNotExistGenerationFails()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenNoOriginalBackup(@"C:\Games\CoH\costumes\guard_original.costume");

            // When
            WhenHvtGeneratesGhostCostumeFile(@"C:\Games\CoH\costumes\guard_original.costume");

            // Then
            ThenErrorReported("missing original backup");
        }

        [TestMethod]
        public void GhostFileAlreadyExistsRegeneratedFromOriginal()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenOriginalBackupAt(@"C:\Games\CoH\costumes\guard_original.costume");
            GivenGhostCostumeFileExists(@"C:\Games\CoH\costumes\guard_ghost.costume");

            // When
            WhenHvtGeneratesGhostCostumeFile(@"C:\Games\CoH\costumes\guard_original.costume");

            // Then
            ThenGhostCostumeFileExists(@"C:\Games\CoH\costumes\guard_ghost.costume",
                "guard_ghost.costume");
        }

        [TestMethod]
        public void GhostCostumeWriteFailsOriginalNotModified()
        {
            // Given
            GivenGameBridgeReady();
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenOriginalBackupAt(@"C:\Games\CoH\costumes\guard_original.costume");

            // When
            WhenGhostCostumeWriteFails();

            // Then
            ThenErrorReported("write error");
            ThenOriginalBackupNotModified(@"C:\Games\CoH\costumes\guard_original.costume",
                "original_costume_content");
        }
    }
}
