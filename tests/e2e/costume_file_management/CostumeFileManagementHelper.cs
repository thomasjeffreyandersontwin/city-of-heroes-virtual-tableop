using System;
using System.IO;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CostumeFileManagement
{
    public class CostumeFileManagementHelper
    {
        protected AppDriver Driver;

        protected static readonly string CostumesDirectory = @"C:\Games\CoH\costumes";
        protected static readonly string DataDirectory = @"C:\Games\CoH\data";

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenGameBridgeReady()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenCohCostumesDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }

        protected void GivenCohCostumesDirectoryReadOnly(string directoryPath)
        {
            Driver.SimulateReadOnlyDirectory(directoryPath);
        }

        protected void GivenCohCostumesDirectoryMissing(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, true);
        }

        protected void GivenCostumeIdentityWithSurface(string costumeSurface)
        {
            Driver.SetCostumeIdentitySurface(costumeSurface);
        }

        protected void GivenCostumeFileAt(string filePath, string costumeData)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, costumeData);
        }

        protected void GivenOriginalBackupCostumeFileAt(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, "original_costume_data");
        }

        protected void GivenNoCostumeFileFor(string characterName)
        {
            string path = Path.Combine(CostumesDirectory, characterName.ToLower() + ".costume");
            if (File.Exists(path))
                File.Delete(path);
        }

        protected void GivenKeybindEntriesAssembled(string entries)
        {
            Driver.SetPendingKeybindEntries(entries);
        }

        protected void GivenNativeBridgeInitialized()
        {
            Driver.SetNativeBridgeInitialized(true);
        }

        protected void GivenKeybindFileAt(string filePath, string entries)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, entries);
        }

        protected void GivenNoKeybindFileAt(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        protected void GivenDataDirectoryMissing()
        {
            if (Directory.Exists(DataDirectory))
                Directory.Delete(DataDirectory, true);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenHvtWritesCostumeFile(string characterName)
        {
            Driver.InvokeWriteCostumeFile(characterName);
        }

        protected void WhenHvtCreatesOriginalBackup(string characterName)
        {
            Driver.InvokeCreateOriginalBackup(characterName);
        }

        protected void WhenGameBridgeWritesKeybindFile()
        {
            Driver.InvokeWriteKeybindFile();
        }

        protected void WhenGameBridgeIssuesBindLoadFile(string filePath)
        {
            Driver.InvokeBindLoadFile(filePath);
        }

        protected void WhenWriteFails()
        {
            Driver.SimulateWriteFailure();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenCostumeFileExistsAt(string filePath)
        {
            Assert.IsTrue(File.Exists(filePath),
                string.Format("Expected costume file at '{0}'", filePath));
        }

        protected void ThenCostumeFileHasData(string filePath, string expectedDataFragment)
        {
            string content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains(expectedDataFragment),
                string.Format("Costume file missing expected data: '{0}'", expectedDataFragment));
        }

        protected void ThenOriginalBackupExistsAt(string backupFilePath)
        {
            Assert.IsTrue(File.Exists(backupFilePath),
                string.Format("Expected backup at '{0}'", backupFilePath));
        }

        protected void ThenOriginalBackupNotOverwritten(string backupFilePath, string expectedContent)
        {
            string actual = File.ReadAllText(backupFilePath);
            Assert.AreEqual(expectedContent, actual,
                "Original backup was overwritten but should be immutable");
        }

        protected void ThenNoBackupCreated(string backupFilePath)
        {
            Assert.IsFalse(File.Exists(backupFilePath),
                string.Format("Expected no backup but found '{0}'", backupFilePath));
        }

        protected void ThenKeybindFileExistsAt(string filePath, string expectedEntries)
        {
            Assert.IsTrue(File.Exists(filePath),
                string.Format("Expected keybind file at '{0}'", filePath));
            string content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains(expectedEntries),
                string.Format("Keybind file missing entries: '{0}'", expectedEntries));
        }

        protected void ThenKeybindFileProcessedByCoh(string filePath)
        {
            Assert.IsTrue(Driver.WasKeybindFileLoaded(filePath),
                string.Format("Keybind file '{0}' not loaded by COH", filePath));
        }

        protected void ThenErrorReported(string expectedFragment)
        {
            string error = Driver.GetLastGameBridgeError();
            Assert.IsNotNull(error, "Expected error but none reported");
            Assert.IsTrue(error.Contains(expectedFragment),
                string.Format("Error does not contain '{0}'. Actual: '{1}'", expectedFragment, error));
        }

        protected void ThenCostumeFileNotModified(string filePath, string originalContent)
        {
            string actual = File.ReadAllText(filePath);
            Assert.AreEqual(originalContent, actual, "Costume file was modified but should not have been");
        }

        protected void ThenDirectoryCreated(string directoryPath)
        {
            Assert.IsTrue(Directory.Exists(directoryPath),
                string.Format("Expected directory '{0}' to be created", directoryPath));
        }
    }
}
