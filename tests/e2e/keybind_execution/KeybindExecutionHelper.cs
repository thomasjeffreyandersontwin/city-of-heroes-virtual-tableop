using System;
using System.IO;
using System.Threading;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.KeybindExecution
{
    public class KeybindExecutionHelper
    {
        protected AppDriver Driver;

        protected static readonly string GameDirectory = @"C:\Games\CoH";
        protected static readonly string KeyBindFilePath = @"C:\Games\CoH\data\hvt_cmd.txt";

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenGameBridgeReady()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenGameBridgeNotReady(string state)
        {
            Driver.SetGameBridgeState(state);
        }

        protected void GivenModelListLoaded()
        {
            Driver.SetModelListState("loaded");
        }

        protected void GivenNativeBridgeInitialized()
        {
            Driver.SetNativeBridgeInitialized(true);
        }

        protected void GivenGameCommand(string commandType, string targetName, string slashCommandComposition)
        {
            Driver.SetPendingGameCommand(commandType, targetName, slashCommandComposition);
        }

        protected void GivenCharacterWithName(string characterName)
        {
            Driver.EnsureCharacterExists(characterName);
        }

        protected void GivenModelIdentityWithModelName(string modelName)
        {
            Driver.SetModelIdentityModelName(modelName);
        }

        protected void GivenSpawnedNpc(string characterName, string presenceInGameWorld)
        {
            Driver.SetSpawnedNpcState(characterName, presenceInGameWorld);
        }

        protected void GivenCostumeFileAt(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if (!File.Exists(filePath))
                File.WriteAllText(filePath, "costume_data_placeholder");
        }

        protected void GivenKeybindFileAt(string filePath, string entries)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, entries);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenGameBridgeGeneratesKeybindFile()
        {
            Driver.InvokeGenerateKeybindFile();
        }

        protected void WhenGameBridgeExecutesSpawnNpcCommand(string characterName, string modelName)
        {
            Driver.InvokeSpawnNpcCommand(characterName, modelName);
        }

        protected void WhenGameBridgeExecutesTargetByNameCommand(string targetNamePayload)
        {
            Driver.InvokeTargetByNameCommand(targetNamePayload);
        }

        protected void WhenGameBridgeExecutesLoadCostumeCommand(string costumeFilePath)
        {
            Driver.InvokeLoadCostumeCommand(costumeFilePath);
        }

        protected void WhenGameBridgeExecutesDeleteNpcCommand(string targetNamePayload)
        {
            Driver.InvokeDeleteNpcCommand(targetNamePayload);
        }

        protected void WhenTargetChainedWithLoadCostume(string characterName, string costumeFile)
        {
            Driver.InvokeTargetThenLoadCostume(characterName, costumeFile);
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenKeybindFileWritten(string filePath, string expectedEntries)
        {
            Assert.IsTrue(File.Exists(filePath),
                string.Format("Expected keybind file at '{0}'", filePath));
            string content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains(expectedEntries),
                string.Format("Keybind file missing entries. Expected: '{0}'", expectedEntries));
        }

        protected void ThenNoKeybindFileWritten(string filePath)
        {
            Assert.IsFalse(File.Exists(filePath),
                string.Format("Expected no keybind file but found '{0}'", filePath));
        }

        protected void ThenSpawnedNpcHasPresence(string characterName, string expectedPresence)
        {
            string actual = Driver.GetSpawnedNpcPresence(characterName);
            Assert.AreEqual(expectedPresence, actual,
                string.Format("Expected NPC '{0}' presence '{1}' but got '{2}'",
                    characterName, expectedPresence, actual));
        }

        protected void ThenTargetByNameResolves(string targetNamePayload)
        {
            string actual = Driver.GetLastTargetResolution();
            Assert.AreEqual(targetNamePayload, actual,
                string.Format("Expected target resolution '{0}' but got '{1}'", targetNamePayload, actual));
        }

        protected void ThenCommandRejected(string reason)
        {
            string error = Driver.GetLastGameBridgeError();
            Assert.IsNotNull(error, "Expected rejection but no error reported");
            Assert.IsTrue(error.Contains(reason),
                string.Format("Expected rejection reason containing '{0}' but got '{1}'", reason, error));
        }

        protected void ThenGameBridgeReportsError(string expectedFragment)
        {
            string error = Driver.GetLastGameBridgeError();
            Assert.IsNotNull(error, "Expected error but none reported");
            Assert.IsTrue(error.Contains(expectedFragment),
                string.Format("Error does not contain '{0}'. Actual: '{1}'", expectedFragment, error));
        }

        protected void ThenCostumeAppliedToNpc(string characterName)
        {
            Assert.IsTrue(Driver.IsCostumeAppliedToNpc(characterName),
                string.Format("Expected costume applied to '{0}'", characterName));
        }
    }
}
