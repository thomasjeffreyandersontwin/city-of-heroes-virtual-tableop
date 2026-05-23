using System;
using System.IO;
using System.Threading;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameBridgeInitialization
{
    public class GameBridgeInitializationHelper
    {
        protected AppDriver Driver;

        protected static readonly string GameDirectory = @"C:\Games\CoH";
        protected static readonly string DllPath = @"C:\Games\CoH\bin\HookCostume.dll";
        protected static readonly string KeyBindFilePath = @"C:\Games\CoH\data\hvt_binds.txt";
        protected static readonly string CostumesDirectory = @"C:\Games\CoH\costumes";

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenGameBridgeWithInitializationState(string state)
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState(state);
        }

        protected void GivenHookCostumeDllLoaded()
        {
            Driver.SetDllLoadedState("loaded");
        }

        protected void GivenHookCostumeDllNotLoaded()
        {
            Driver.SetDllLoadedState("not loaded");
        }

        protected void GivenInitGameWillReturnFailure()
        {
            Driver.SetInitGameWillFail(true);
        }

        protected void GivenDllLoadWillFail()
        {
            Driver.SetDllLoadWillFail(true);
        }

        protected void GivenPollWillReturnNotReady()
        {
            Driver.SetPollWillReturnNotReady(true);
        }

        protected void GivenPollingWillTimeout()
        {
            Driver.SetPollingWillTimeout(true);
        }

        protected void GivenCohGameDirectoryBasePath(string basePath)
        {
            Driver.SetCohGameDirectory(basePath);
        }

        protected void GivenCohGameDirectoryNotValidated()
        {
            Driver.SetCohGameDirectoryValidated(false);
        }

        protected void GivenGameLoadedEventPublished()
        {
            Driver.SetGameLoadedEventState("published");
        }

        protected void GivenGameLoadedEventUnpublished()
        {
            Driver.SetGameLoadedEventState("unpublished");
        }

        protected void GivenNativeBridgeInitialized()
        {
            Driver.SetNativeBridgeInitialized(true);
        }

        protected void GivenCohCostumesDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenGameBridgeAttemptsLoadHookCostumeDll(string basePath)
        {
            Driver.InvokeLoadHookCostumeDll(basePath);
        }

        protected void WhenGameBridgeAttemptsLoadHookCostumeDll()
        {
            Driver.InvokeLoadHookCostumeDll(null);
        }

        protected void WhenGameBridgeCallsInitGame()
        {
            Driver.InvokeInitGame();
        }

        protected void WhenGameBridgePollsGameState()
        {
            Driver.InvokePollGameState();
        }

        protected void WhenGameBridgeInjectsRequiredKeybinds(string gameDirectory)
        {
            Driver.InvokeInjectKeybinds(gameDirectory);
        }

        protected void WhenGameBridgeExtractsCostumePack(string costumesDirectory)
        {
            Driver.InvokeExtractCostumePack(costumesDirectory);
        }

        protected void WhenGameBridgePollingLoopConfirmsClientStatus()
        {
            Driver.InvokePollingConfirmation();
        }

        protected void WhenGameBridgeInitializesNativeBridge()
        {
            Driver.InvokeInitializeNativeBridge();
        }

        protected void WhenSlashCommandSubmitted(string commandString)
        {
            Driver.InvokeSlashCommand(commandString);
        }

        protected void WhenKeybindFileWriteFails()
        {
            Driver.SimulateKeybindWriteFailure();
        }

        protected void WhenBindLoadFileCommandFails()
        {
            Driver.SimulateBindLoadFileFailure();
        }

        protected void WhenExtractionFailsPartway()
        {
            Driver.SimulateExtractionFailure();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenDllHasLoadedState(string expectedState)
        {
            string actual = Driver.GetDllLoadedState();
            Assert.AreEqual(expectedState, actual,
                string.Format("Expected DLL loaded state '{0}' but got '{1}'", expectedState, actual));
        }

        protected void ThenGameBridgeHasInitializationState(string expectedState)
        {
            string actual = Driver.GetGameBridgeInitializationState();
            Assert.AreEqual(expectedState, actual,
                string.Format("Expected bridge state '{0}' but got '{1}'", expectedState, actual));
        }

        protected void ThenGameLoadedEventHasPublicationState(string expectedState)
        {
            string actual = Driver.GetGameLoadedEventPublicationState();
            Assert.AreEqual(expectedState, actual,
                string.Format("Expected event state '{0}' but got '{1}'", expectedState, actual));
        }

        protected void ThenKeybindFileExistsAt(string filePath)
        {
            Assert.IsTrue(File.Exists(filePath),
                string.Format("Expected keybind file at '{0}' but file not found", filePath));
        }

        protected void ThenKeybindFileContainsEntries(string filePath, string expectedContent)
        {
            string actual = File.ReadAllText(filePath);
            Assert.IsTrue(actual.Contains(expectedContent),
                string.Format("Keybind file missing expected entries. Expected contains: '{0}'", expectedContent));
        }

        protected void ThenCostumesDirectoryAvailable(string directoryPath)
        {
            Assert.IsTrue(Directory.Exists(directoryPath),
                string.Format("Costumes directory '{0}' not available", directoryPath));
        }

        protected void ThenGameBridgeReportsError(string expectedErrorFragment)
        {
            string error = Driver.GetLastGameBridgeError();
            Assert.IsNotNull(error, "Expected a game bridge error but none reported");
            Assert.IsTrue(error.Contains(expectedErrorFragment),
                string.Format("Error message does not contain '{0}'. Actual: '{1}'", expectedErrorFragment, error));
        }

        protected void ThenSlashCommandDeliveredVia(string expectedDeliveryPath)
        {
            string actual = Driver.GetLastCommandDeliveryPath();
            Assert.AreEqual(expectedDeliveryPath, actual,
                string.Format("Expected delivery path '{0}' but got '{1}'", expectedDeliveryPath, actual));
        }

        protected void ThenSlashCommandRejected()
        {
            string actual = Driver.GetLastCommandDeliveryPath();
            Assert.AreEqual("(rejected)", actual, "Expected command to be rejected");
        }

        protected void ThenNoLoadAttemptMade()
        {
            Assert.IsFalse(Driver.WasLoadAttemptMade(), "Expected no load attempt but one was made");
        }
    }
}
