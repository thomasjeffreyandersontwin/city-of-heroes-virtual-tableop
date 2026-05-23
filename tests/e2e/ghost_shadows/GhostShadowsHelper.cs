using System;
using System.IO;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GhostShadows
{
    public class GhostShadowsHelper
    {
        protected AppDriver Driver;

        protected static readonly string CostumesDirectory = @"C:\Games\CoH\costumes";

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenGameBridgeReady()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenGameBridgeNotReady()
        {
            Driver.SetGameBridgeState("polling");
        }

        protected void GivenCharacterWithName(string characterName)
        {
            Driver.EnsureCharacterExists(characterName);
        }

        protected void GivenModelIdentityActive(string identityName, string modelName)
        {
            Driver.SetIdentityAsModel(identityName, modelName);
            Driver.SetIdentityActiveState(identityName, "active");
        }

        protected void GivenCostumeIdentityActive(string identityName)
        {
            Driver.SetIdentityAsCostume(identityName, null);
            Driver.SetIdentityActiveState(identityName, "active");
        }

        protected void GivenIdentityInactive(string identityName)
        {
            Driver.SetIdentityActiveState(identityName, "inactive");
        }

        protected void GivenOriginalBackupAt(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, "original_costume_content");
        }

        protected void GivenNoOriginalBackup(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        protected void GivenGhostNpcPresent(string ghostNpcName)
        {
            Driver.SetSpawnedNpcState(ghostNpcName, "present");
        }

        protected void GivenGhostNpcAbsent(string ghostNpcName)
        {
            Driver.SetSpawnedNpcState(ghostNpcName, "absent");
        }

        protected void GivenGhostShadowActive(string characterName)
        {
            Driver.SetGhostShadowState(characterName, "active");
        }

        protected void GivenGhostShadowInactive(string characterName)
        {
            Driver.SetGhostShadowState(characterName, "inactive");
        }

        protected void GivenSpawnedNpc(string characterName, string presence)
        {
            Driver.SetSpawnedNpcState(characterName, presence);
        }

        protected void GivenCohCostumesDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        protected void GivenGhostCostumeFileExists(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, "ghost_costume_data");
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenGmChoosesAddGhost(string characterName)
        {
            Driver.InvokeAddGhost(characterName);
        }

        protected void WhenHvtGeneratesGhostCostumeFile(string sourceFilePath)
        {
            Driver.InvokeGenerateGhostCostumeFile(sourceFilePath);
        }

        protected void WhenGameBridgePerformsGhostAlignment(string characterName)
        {
            Driver.InvokeGhostAlignment(characterName);
        }

        protected void WhenGmChoosesRemoveGhost(string characterName)
        {
            Driver.InvokeRemoveGhost(characterName);
        }

        protected void WhenCharacterClearedFromDesktop(string characterName)
        {
            Driver.InvokeClearFromDesktop(characterName);
        }

        protected void WhenGhostCostumeWriteFails()
        {
            Driver.SimulateGhostCostumeWriteFailure();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenGhostShadowState(string characterName, string expectedState)
        {
            string actual = Driver.GetGhostShadowState(characterName);
            Assert.AreEqual(expectedState, actual,
                string.Format("Ghost shadow on '{0}': expected '{1}' got '{2}'",
                    characterName, expectedState, actual));
        }

        protected void ThenGhostNpcPresence(string ghostNpcName, string expectedPresence)
        {
            string actual = Driver.GetSpawnedNpcPresence(ghostNpcName);
            Assert.AreEqual(expectedPresence, actual,
                string.Format("Ghost NPC '{0}': expected presence '{1}' got '{2}'",
                    ghostNpcName, expectedPresence, actual));
        }

        protected void ThenGhostCostumeFileExists(string filePath, string expectedNamingConvention)
        {
            Assert.IsTrue(File.Exists(filePath),
                string.Format("Expected ghost costume file at '{0}'", filePath));
            Assert.IsTrue(filePath.Contains(expectedNamingConvention),
                string.Format("Ghost file naming convention mismatch. Expected '{0}' in path",
                    expectedNamingConvention));
        }

        protected void ThenGhostMaterialTreatmentApplied(string filePath)
        {
            string content = File.ReadAllText(filePath);
            Assert.IsTrue(Driver.HasGhostMaterialTreatment(filePath),
                "Ghost material treatment not applied");
        }

        protected void ThenGhostAligned(string ghostNpcName, string expectedAlignment)
        {
            string actual = Driver.GetGhostAlignment(ghostNpcName);
            Assert.AreEqual(expectedAlignment, actual,
                string.Format("Ghost '{0}' alignment: expected '{1}' got '{2}'",
                    ghostNpcName, expectedAlignment, actual));
        }

        protected void ThenGhostIndicatorShown(string identityName)
        {
            Assert.IsTrue(Driver.IsGhostIndicatorVisible(identityName),
                string.Format("Ghost indicator not shown for '{0}'", identityName));
        }

        protected void ThenGhostIndicatorCleared(string identityName)
        {
            Assert.IsFalse(Driver.IsGhostIndicatorVisible(identityName),
                string.Format("Ghost indicator should be cleared for '{0}'", identityName));
        }

        protected void ThenAddGhostDisabled(string reason)
        {
            string message = Driver.GetLastValidationMessage();
            Assert.IsNotNull(message, "Expected disabled indicator");
            Assert.IsTrue(message.Contains(reason),
                string.Format("Disabled reason does not contain '{0}'", reason));
        }

        protected void ThenAddGhostEnabled()
        {
            Assert.IsTrue(Driver.IsAddGhostEnabled(), "Add Ghost should be enabled");
        }

        protected void ThenErrorReported(string expectedFragment)
        {
            string error = Driver.GetLastGameBridgeError();
            Assert.IsNotNull(error, "Expected error but none reported");
            Assert.IsTrue(error.Contains(expectedFragment),
                string.Format("Error does not contain '{0}'", expectedFragment));
        }

        protected void ThenOriginalBackupNotModified(string filePath, string expectedContent)
        {
            string actual = File.ReadAllText(filePath);
            Assert.AreEqual(expectedContent, actual, "Original backup was modified");
        }

        protected void ThenNoGhostCostumeFileCreated(string filePath)
        {
            Assert.IsFalse(File.Exists(filePath),
                string.Format("Ghost costume file should not exist at '{0}'", filePath));
        }
    }
}
