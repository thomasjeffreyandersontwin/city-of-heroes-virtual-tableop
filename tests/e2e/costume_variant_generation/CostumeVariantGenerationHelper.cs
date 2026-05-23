using System;
using System.IO;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CostumeVariantGeneration
{
    public class CostumeVariantGenerationHelper
    {
        protected AppDriver Driver;

        protected static readonly string CostumesDirectory = @"C:\Games\CoH\costumes";

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenCohCostumesDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        protected void GivenOriginalBackupAt(string filePath, string characterName)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, "original_costume_content_for_" + characterName);
        }

        protected void GivenNoOriginalBackup(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        protected void GivenVariantAlreadyExists(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, "old_variant_content");
        }

        protected void GivenGhostCostumeFileExists(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, "existing_ghost_content");
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenHvtGeneratesPersistentFxVariant(string sourceFilePath)
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.InvokeGeneratePersistentFxVariant(sourceFilePath);
        }

        protected void WhenHvtCreatesGhostCostumeFile(string sourceFilePath, string characterName)
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.InvokeCreateGhostCostumeFile(sourceFilePath, characterName);
        }

        protected void WhenVariantWriteFails()
        {
            if (Driver == null)
            {
                Driver = new AppDriver();
                Driver.LaunchForStateSimulation();
            }
            Driver.SimulateVariantWriteFailure();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenPersistentFxVariantExists(string expectedLayers)
        {
            Assert.IsTrue(Driver.DoesPersistentFxVariantExist(),
                "Persistent-FX variant not found");
            string layers = Driver.GetPersistentFxLayers();
            Assert.IsTrue(layers.Contains(expectedLayers),
                string.Format("FX layers mismatch. Expected contains: '{0}'", expectedLayers));
        }

        protected void ThenNoPersistentFxVariant()
        {
            Assert.IsFalse(Driver.DoesPersistentFxVariantExist(),
                "Persistent-FX variant should not exist");
        }

        protected void ThenGhostCostumeFileExists(string expectedNaming, string expectedTreatment)
        {
            string path = Driver.GetLastGhostCostumeFilePath();
            Assert.IsNotNull(path, "No ghost costume file created");
            Assert.IsTrue(path.Contains(expectedNaming),
                string.Format("Naming convention mismatch. Expected '{0}' in path", expectedNaming));
            Assert.IsTrue(Driver.HasGhostMaterialTreatment(path),
                "Ghost material treatment not applied");
        }

        protected void ThenNoGhostCostumeFile()
        {
            string path = Driver.GetLastGhostCostumeFilePath();
            Assert.IsTrue(string.IsNullOrEmpty(path), "Ghost costume file should not be created");
        }

        protected void ThenErrorReported(string expectedFragment)
        {
            string error = Driver.GetLastGameBridgeError();
            Assert.IsNotNull(error, "Expected error but none reported");
            Assert.IsTrue(error.Contains(expectedFragment),
                string.Format("Error does not contain '{0}'", expectedFragment));
        }

        protected void ThenOriginalBackupNotModified(string filePath)
        {
            string content = File.ReadAllText(filePath);
            Assert.IsTrue(content.StartsWith("original_costume_content"),
                "Original backup was modified");
        }
    }
}
