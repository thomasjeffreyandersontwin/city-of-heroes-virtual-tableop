using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CostumeVariantGeneration
{
    [TestClass]
    public class CreatePersistentFxCostumeVariants : CostumeVariantGenerationHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SuccessfulGenerationVariantCreated()
        {
            // Given
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenOriginalBackupAt(@"C:\Games\CoH\costumes\guard_original.costume", "Guard_Captain");

            // When
            WhenHvtGeneratesPersistentFxVariant(@"C:\Games\CoH\costumes\guard_original.costume");

            // Then
            ThenPersistentFxVariantExists("FX overlaid on source costume data");
        }

        [TestMethod]
        public void OriginalDoesNotExistGenerationFails()
        {
            // Given
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenNoOriginalBackup(@"C:\Games\CoH\costumes\guard_original.costume");

            // When
            WhenHvtGeneratesPersistentFxVariant(@"C:\Games\CoH\costumes\guard_original.costume");

            // Then
            ThenNoPersistentFxVariant();
            ThenErrorReported("missing original backup");
        }

        [TestMethod]
        public void VariantAlreadyExistsOverwrittenWithFreshVersion()
        {
            // Given
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenOriginalBackupAt(@"C:\Games\CoH\costumes\guard_original.costume", "Guard_Captain");
            GivenVariantAlreadyExists(@"C:\Games\CoH\costumes\guard_fx.costume");

            // When
            WhenHvtGeneratesPersistentFxVariant(@"C:\Games\CoH\costumes\guard_original.costume");

            // Then
            ThenPersistentFxVariantExists("FX overlaid on source costume data");
        }

        [TestMethod]
        public void VariantWriteFailsOriginalNotModified()
        {
            // Given
            GivenCohCostumesDirectory(@"C:\Games\CoH\costumes");
            GivenOriginalBackupAt(@"C:\Games\CoH\costumes\guard_original.costume", "Guard_Captain");

            // When
            WhenVariantWriteFails();

            // Then
            ThenErrorReported("variant");
            ThenOriginalBackupNotModified(@"C:\Games\CoH\costumes\guard_original.costume");
        }
    }
}
