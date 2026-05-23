using System;
using System.IO;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityRendering
{
    public class IdentityRenderingHelper
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

        protected void GivenModelListLoaded(params string[] availableModels)
        {
            Driver.SetModelListState("loaded");
            Driver.SetAvailableModels(availableModels);
        }

        protected void GivenCharacterWithName(string characterName)
        {
            Driver.EnsureCharacterExists(characterName);
        }

        protected void GivenModelIdentity(string identityName, string modelName)
        {
            Driver.SetIdentityAsModel(identityName, modelName);
        }

        protected void GivenCostumeIdentity(string identityName, string costumeSurface)
        {
            Driver.SetIdentityAsCostume(identityName, costumeSurface);
        }

        protected void GivenActiveIdentity(string identityName)
        {
            Driver.SetIdentityActiveState(identityName, "active");
        }

        protected void GivenSpawnedNpc(string characterName, string presence)
        {
            Driver.SetSpawnedNpcState(characterName, presence);
        }

        protected void GivenPersistentAbilitiesActive(string characterName)
        {
            Driver.SetPersistentAbilitiesActive(characterName, true);
        }

        protected void GivenNoPersistentAbilities(string characterName)
        {
            Driver.SetPersistentAbilitiesActive(characterName, false);
        }

        protected void GivenCostumeFileAt(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if (!File.Exists(filePath))
                File.WriteAllText(filePath, "costume_data_placeholder");
        }

        protected void GivenNoCostumeFileAt(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        protected void GivenIdentityActivationCompleted()
        {
            Driver.ConfirmIdentityActivationCompleted();
        }

        protected void GivenNoSpawnAnimationConfigured()
        {
            Driver.SetSpawnAnimationConfigured(false);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenGmActivatesModelIdentity(string characterName, string identityName)
        {
            Driver.InvokeActivateModelIdentity(characterName, identityName);
        }

        protected void WhenGmActivatesCostumeIdentity(string characterName, string identityName)
        {
            Driver.InvokeActivateCostumeIdentity(characterName, identityName);
        }

        protected void WhenGmSetsActiveDesignation(string identityName)
        {
            Driver.InvokeSetActiveIdentity(identityName);
        }

        protected void WhenGameBridgeExecutesIdentityActivation(string identityName)
        {
            Driver.InvokeIdentityActivation(identityName);
        }

        protected void WhenIdentitySwitchInitiated(string newIdentityName)
        {
            Driver.InvokeIdentitySwitch(newIdentityName);
        }

        protected void WhenGameBridgeIssuesSpawnAnimation()
        {
            Driver.InvokeSpawnAnimation();
        }

        protected void WhenIdentityLoadCompletes()
        {
            Driver.ConfirmIdentityLoadComplete();
        }

        protected void WhenAnimationCommandFails()
        {
            Driver.SimulateAnimationFailure();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenSpawnedNpcPresent(string characterName)
        {
            string presence = Driver.GetSpawnedNpcPresence(characterName);
            Assert.AreEqual("present", presence,
                string.Format("Expected NPC '{0}' present but got '{1}'", characterName, presence));
        }

        protected void ThenSpawnedNpcAbsent(string characterName)
        {
            string presence = Driver.GetSpawnedNpcPresence(characterName);
            Assert.AreEqual("absent", presence,
                string.Format("Expected NPC '{0}' absent but got '{1}'", characterName, presence));
        }

        protected void ThenTargetByNameCommandIssued(string targetNamePayload)
        {
            string target = Driver.GetLastTargetResolution();
            Assert.AreEqual(targetNamePayload, target,
                string.Format("Expected target '{0}' but got '{1}'", targetNamePayload, target));
        }

        protected void ThenLoadCostumeCommandIssued(string costumeFilePath)
        {
            string actual = Driver.GetLastLoadedCostumePath();
            Assert.AreEqual(costumeFilePath, actual,
                string.Format("Expected costume load '{0}' but got '{1}'", costumeFilePath, actual));
        }

        protected void ThenActivationBlocked(string reason)
        {
            string message = Driver.GetLastValidationMessage();
            Assert.IsNotNull(message, "Expected activation blocked");
            Assert.IsTrue(message.Contains(reason),
                string.Format("Block message does not contain '{0}'. Actual: '{1}'", reason, message));
        }

        protected void ThenActiveIndicatorShown(string identityName)
        {
            Assert.IsTrue(Driver.IsActiveIndicatorVisible(identityName),
                string.Format("Active indicator not shown for '{0}'", identityName));
        }

        protected void ThenPersistentAbilitiesStopped(string characterName)
        {
            Assert.IsFalse(Driver.ArePersistentAbilitiesActive(characterName),
                string.Format("Persistent abilities should be stopped for '{0}'", characterName));
        }

        protected void ThenDeleteNpcCommandIssued(string characterName)
        {
            Assert.IsTrue(Driver.WasDeleteNpcCommandIssued(characterName),
                string.Format("Expected delete NPC command for '{0}'", characterName));
        }

        protected void ThenSpawnNpcCommandIssued(string characterName)
        {
            Assert.IsTrue(Driver.WasSpawnNpcCommandIssued(characterName),
                string.Format("Expected spawn NPC command for '{0}'", characterName));
        }

        protected void ThenAnimationPlayed(string characterName)
        {
            Assert.IsTrue(Driver.WasAnimationPlayed(characterName),
                string.Format("Expected animation on '{0}'", characterName));
        }

        protected void ThenNpcRenderedAtRest(string characterName)
        {
            Assert.IsTrue(Driver.IsNpcRenderedAtRest(characterName),
                string.Format("Expected NPC '{0}' rendered at rest", characterName));
        }

        protected void ThenGameBridgeLogsFailure()
        {
            Assert.IsNotNull(Driver.GetLastGameBridgeError(), "Expected logged failure");
        }

        protected void ThenIdentityStillActive(string identityName)
        {
            string active = Driver.GetIdentityActiveDesignation(identityName);
            Assert.AreEqual("active", active,
                string.Format("Identity '{0}' should still be active", identityName));
        }
    }
}
