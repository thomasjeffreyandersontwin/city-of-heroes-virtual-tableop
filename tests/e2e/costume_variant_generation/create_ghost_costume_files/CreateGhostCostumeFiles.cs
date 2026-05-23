using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CostumeVariantGeneration
{
    [TestClass]
    public class CreateGhostCostumeFiles : CostumeVariantGenerationHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SuccessfulCreationGhostFileWritten()
        {
            // Given
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenOriginalBackupAt(@"C:\Games\CoH\costumes\guard_original.costume", "Guard_Captain");

            // When
            WhenHvtCreatesGhostCostumeFile(@"C:\Games\CoH\costumes\guard_original.costume",
                "Guard_Captain");

            // Then
            ThenGhostCostumeFileExists("guard_ghost.costume", "reduced-opacity on all parts");
        }

        [TestMethod]
        public void OriginalMissingCreationFails()
        {
            // Given
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenNoOriginalBackup(@"C:\Games\CoH\costumes\shadow_original.costume");

            // When
            WhenHvtCreatesGhostCostumeFile(@"C:\Games\CoH\costumes\shadow_original.costume",
                "Shadow_Knight");

            // Then
            ThenNoGhostCostumeFile();
            ThenErrorReported("missing original");
        }

        [TestMethod]
        public void FileAlreadyExistsOverwrittenWithFreshVersion()
        {
            // Given
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenOriginalBackupAt(@"C:\Games\CoH\costumes\archer_original.costume", "Frost_Archer");
            GivenGhostCostumeFileExists(@"C:\Games\CoH\costumes\archer_ghost.costume");

            // When
            WhenHvtCreatesGhostCostumeFile(@"C:\Games\CoH\costumes\archer_original.costume",
                "Frost_Archer");

            // Then
            ThenGhostCostumeFileExists("archer_ghost.costume", "reduced-opacity on all parts");
        }

        [TestMethod]
        public void MultipleCharactersSeparateGhostFiles()
        {
            // Given
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenOriginalBackupAt(@"C:\Games\CoH\costumes\guard_original.costume", "Guard_Captain");
            GivenOriginalBackupAt(@"C:\Games\CoH\costumes\archer_original.costume", "Frost_Archer");

            // When
            WhenHvtCreatesGhostCostumeFile(@"C:\Games\CoH\costumes\guard_original.costume",
                "Guard_Captain");

            // Then
            ThenGhostCostumeFileExists("guard_ghost.costume", "reduced-opacity on all parts");
        }
    }
}
