using System;
using System.IO;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityManagement
{
    public class IdentityManagementHelper
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

        protected void GivenCharacterSelected(string characterName)
        {
            Driver.EnsureCharacterExists(characterName);
            Driver.SelectCharacterInCrowdTree(characterName);
        }

        protected void GivenNoCharacterSelected()
        {
            Driver.ClearCharacterSelection();
        }

        protected void GivenIdentityOnCharacter(string characterName, string identityName,
            string activeDesignation, string defaultDesignation)
        {
            Driver.AddIdentityToCharacter(characterName, identityName, activeDesignation, defaultDesignation);
        }

        protected void GivenModelIdentity(string identityName, string modelName)
        {
            Driver.SetIdentityAsModel(identityName, modelName);
        }

        protected void GivenCostumeIdentity(string identityName, string costumeSurface)
        {
            Driver.SetIdentityAsCostume(identityName, costumeSurface);
        }

        protected void GivenSpawnedNpc(string characterName, string presence)
        {
            Driver.SetSpawnedNpcState(characterName, presence);
        }

        protected void GivenNoIdentitiesOnCharacter(string characterName)
        {
            Driver.ClearIdentitiesOnCharacter(characterName);
        }

        protected void GivenCostumeFileAt(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if (!File.Exists(filePath))
                File.WriteAllText(filePath, "costume_data");
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenGmAddsIdentity(string identityName, string characterName)
        {
            Driver.InvokeAddIdentity(characterName, identityName);
        }

        protected void WhenGmSetsIdentityType(string identityName, string type)
        {
            Driver.InvokeSetIdentityType(identityName, type);
        }

        protected void WhenGmAssignsCostumeSurface(string identityName, string costumeSurface)
        {
            Driver.InvokeAssignCostumeSurface(identityName, costumeSurface);
        }

        protected void WhenGmSetsDefaultDesignation(string identityName)
        {
            Driver.InvokeSetDefaultIdentity(identityName);
        }

        protected void WhenGmRemovesDefaultDesignation(string identityName)
        {
            Driver.InvokeRemoveDefaultDesignation(identityName);
        }

        protected void WhenGmSetsActiveDesignation(string identityName)
        {
            Driver.InvokeSetActiveIdentity(identityName);
        }

        protected void WhenGmRemovesIdentity(string identityName, string characterName)
        {
            Driver.InvokeRemoveIdentity(characterName, identityName);
        }

        protected void WhenSessionRestarts()
        {
            Driver.RestartSession();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenIdentityExistsWithState(string identityName,
            string expectedActive, string expectedDefault)
        {
            string actualActive = Driver.GetIdentityActiveDesignation(identityName);
            string actualDefault = Driver.GetIdentityDefaultDesignation(identityName);
            Assert.AreEqual(expectedActive, actualActive,
                string.Format("Identity '{0}' active: expected '{1}' got '{2}'",
                    identityName, expectedActive, actualActive));
            Assert.AreEqual(expectedDefault, actualDefault,
                string.Format("Identity '{0}' default: expected '{1}' got '{2}'",
                    identityName, expectedDefault, actualDefault));
        }

        protected void ThenIdentityRejected(string reason)
        {
            string error = Driver.GetLastValidationMessage();
            Assert.IsNotNull(error, "Expected rejection but no validation message");
            Assert.IsTrue(error.Contains(reason),
                string.Format("Validation message does not contain '{0}'. Actual: '{1}'", reason, error));
        }

        protected void ThenIdentityListShowsTypeIndicator(string identityName, string expectedType)
        {
            string actual = Driver.GetIdentityTypeIndicator(identityName);
            Assert.AreEqual(expectedType, actual,
                string.Format("Identity '{0}' type indicator: expected '{1}' got '{2}'",
                    identityName, expectedType, actual));
        }

        protected void ThenCostumeSurfaceCleared(string identityName)
        {
            string surface = Driver.GetIdentityCostumeSurface(identityName);
            Assert.IsTrue(string.IsNullOrEmpty(surface),
                string.Format("Expected costume surface cleared on '{0}' but got '{1}'", identityName, surface));
        }

        protected void ThenModelNameCleared(string identityName)
        {
            string model = Driver.GetIdentityModelName(identityName);
            Assert.IsTrue(string.IsNullOrEmpty(model),
                string.Format("Expected model name cleared on '{0}' but got '{1}'", identityName, model));
        }

        protected void ThenCostumeSurfaceField(string identityName, string expectedSurface)
        {
            string actual = Driver.GetIdentityCostumeSurface(identityName);
            Assert.AreEqual(expectedSurface, actual,
                string.Format("Identity '{0}' surface: expected '{1}' got '{2}'",
                    identityName, expectedSurface, actual));
        }

        protected void ThenAddIdentityDisabled()
        {
            Assert.IsFalse(Driver.IsAddIdentityEnabled(), "Add identity should be disabled");
        }

        protected void ThenSetDefaultDisabled()
        {
            Assert.IsFalse(Driver.IsSetDefaultEnabled(), "Set Default should be disabled");
        }

        protected void ThenSetActiveBlocked(string reason)
        {
            string message = Driver.GetLastValidationMessage();
            Assert.IsNotNull(message, "Expected blocked indicator");
            Assert.IsTrue(message.Contains(reason),
                string.Format("Block reason does not contain '{0}'. Actual: '{1}'", reason, message));
        }

        protected void ThenIdentityNotInList(string identityName)
        {
            Assert.IsFalse(Driver.IsIdentityInList(identityName),
                string.Format("Identity '{0}' should not be in list", identityName));
        }

        protected void ThenSpawnedNpcPresence(string characterName, string expectedPresence)
        {
            string actual = Driver.GetSpawnedNpcPresence(characterName);
            Assert.AreEqual(expectedPresence, actual,
                string.Format("NPC '{0}': expected presence '{1}' got '{2}'",
                    characterName, expectedPresence, actual));
        }

        protected void ThenNoGameCommandsIssued()
        {
            Assert.AreEqual(0, Driver.GetGameCommandCount(), "Expected no game commands issued");
        }

        protected void ThenIdentityListEmpty()
        {
            Assert.AreEqual(0, Driver.GetIdentityListCount(), "Expected empty identity list");
        }

        protected void ThenDespawnConfirmationShown()
        {
            Assert.IsTrue(Driver.IsDespawnConfirmationVisible(), "Expected despawn confirmation");
        }
    }
}
