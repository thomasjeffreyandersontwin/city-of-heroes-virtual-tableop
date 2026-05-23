using System;
using System.Collections.Generic;
using System.IO;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ModelBrowser
{
    public class ModelBrowserHelper
    {
        protected AppDriver Driver;

        protected static readonly string GameDirectory = @"C:\Games\CoH";
        protected static readonly string ModelsTxtPath = @"C:\Games\CoH\Models.txt";

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenGameBridgeReady()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenGameLoadedEventPublished()
        {
            Driver.SetGameLoadedEventState("published");
        }

        protected void GivenModelListLoaded(params string[] models)
        {
            Driver.SetModelListState("loaded");
            Driver.SetAvailableModels(models);
        }

        protected void GivenModelListNotLoaded()
        {
            Driver.SetModelListState("not loaded");
        }

        protected void GivenModelsTxtAt(string filePath, string[] modelEntries)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllLines(filePath, modelEntries);
        }

        protected void GivenModelsTxtAbsent(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        protected void GivenModelsTxtEmpty(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, "");
        }

        protected void GivenModelBrowserOpen()
        {
            Driver.OpenModelBrowser();
        }

        protected void GivenModelSelected(string archetypeName)
        {
            Driver.SelectModelInBrowser(archetypeName);
        }

        protected void GivenNoModelsSelected()
        {
            Driver.ClearModelSelection();
        }

        protected void GivenExistingCrowdWithName(string crowdName)
        {
            Driver.EnsureCrowdExists(crowdName);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenHvtReadsModelsTxt(string filePath)
        {
            Driver.InvokeLoadModelsTxt(filePath);
        }

        protected void WhenGmSelectsModel(string archetypeName)
        {
            Driver.SelectModelInBrowser(archetypeName);
        }

        protected void WhenGmDeselectsModel(string archetypeName)
        {
            Driver.DeselectModelInBrowser(archetypeName);
        }

        protected void WhenGmChoosesCreateCrowdFromSelection()
        {
            Driver.InvokeCreateCrowdFromSelection();
        }

        protected void WhenGmConfirmsCrowdCreation(string[] selectedModels)
        {
            foreach (string model in selectedModels)
                Driver.SelectModelInBrowser(model);
            Driver.InvokeCreateCrowdFromSelection();
        }

        protected void WhenGmEntersFilter(string filterTerm)
        {
            Driver.EnterModelBrowserFilter(filterTerm);
        }

        protected void WhenGmClearsFilter()
        {
            Driver.ClearModelBrowserFilter();
        }

        protected void WhenGmCancelsModelBrowser()
        {
            Driver.CancelModelBrowser();
        }

        protected void WhenGmAttemptsOpenModelBrowser()
        {
            Driver.AttemptOpenModelBrowser();
        }

        protected void WhenSessionEndsAndNewBegins()
        {
            Driver.RestartSession();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenModelListHasState(string expectedState)
        {
            string actual = Driver.GetModelListLoadedState();
            Assert.AreEqual(expectedState, actual,
                string.Format("Model list state: expected '{0}' got '{1}'", expectedState, actual));
        }

        protected void ThenModelListContains(params string[] expectedModels)
        {
            var actual = Driver.GetAvailableModels();
            foreach (string model in expectedModels)
            {
                Assert.IsTrue(actual.Contains(model),
                    string.Format("Model list missing '{0}'. Actual: [{1}]",
                        model, string.Join(", ", actual)));
            }
        }

        protected void ThenModelListIsEmpty()
        {
            var actual = Driver.GetAvailableModels();
            Assert.AreEqual(0, actual.Count, "Expected empty model list");
        }

        protected void ThenModelBrowserShowsNoModelsMessage()
        {
            Assert.IsTrue(Driver.IsNoModelsMessageVisible(),
                "Expected 'no models available' message");
        }

        protected void ThenModelMarkedAsSelected(string archetypeName)
        {
            Assert.IsTrue(Driver.IsModelSelected(archetypeName),
                string.Format("Model '{0}' should be selected", archetypeName));
        }

        protected void ThenModelNotSelected(string archetypeName)
        {
            Assert.IsFalse(Driver.IsModelSelected(archetypeName),
                string.Format("Model '{0}' should not be selected", archetypeName));
        }

        protected void ThenCreateCrowdButtonEnabled()
        {
            Assert.IsTrue(Driver.IsCreateCrowdFromSelectionEnabled(),
                "Create Crowd from Selection should be enabled");
        }

        protected void ThenCreateCrowdButtonDisabled()
        {
            Assert.IsFalse(Driver.IsCreateCrowdFromSelectionEnabled(),
                "Create Crowd from Selection should be disabled");
        }

        protected void ThenCrowdCreatedWithCharacterCount(int expectedCount)
        {
            int actual = Driver.GetLastCreatedCrowdCharacterCount();
            Assert.AreEqual(expectedCount, actual,
                string.Format("Expected crowd with {0} characters, got {1}", expectedCount, actual));
        }

        protected void ThenCharacterHasModelIdentity(string characterName, string expectedModelName)
        {
            string actual = Driver.GetCharacterModelIdentityName(characterName);
            Assert.AreEqual(expectedModelName, actual,
                string.Format("Character '{0}' model identity: expected '{1}' got '{2}'",
                    characterName, expectedModelName, actual));
        }

        protected void ThenCharacterNameIs(string expectedName)
        {
            Assert.IsTrue(Driver.CharacterExistsWithName(expectedName),
                string.Format("Expected character '{0}' not found", expectedName));
        }

        protected void ThenModelBrowserDisabled(string reason)
        {
            Assert.IsFalse(Driver.IsModelBrowserEnabled(),
                "Model browser should be disabled");
            string message = Driver.GetLastValidationMessage();
            Assert.IsTrue(message != null && message.Contains(reason),
                string.Format("Expected disabled reason containing '{0}'", reason));
        }

        protected void ThenModelBrowserEnabled()
        {
            Assert.IsTrue(Driver.IsModelBrowserEnabled(), "Model browser should be enabled");
        }

        protected void ThenCrowdRepositoryUnchanged()
        {
            Assert.IsFalse(Driver.WasCrowdCreated(), "Crowd repository should be unchanged");
        }

        protected void ThenErrorReported(string expectedFragment)
        {
            string error = Driver.GetLastGameBridgeError();
            Assert.IsNotNull(error, "Expected error but none reported");
            Assert.IsTrue(error.Contains(expectedFragment),
                string.Format("Error does not contain '{0}'", expectedFragment));
        }
    }
}
