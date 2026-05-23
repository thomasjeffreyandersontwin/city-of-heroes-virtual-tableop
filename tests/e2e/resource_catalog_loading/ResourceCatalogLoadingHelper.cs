using System;
using System.IO;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ResourceCatalogLoading
{
    public class ResourceCatalogLoadingHelper
    {
        protected AppDriver Driver;

        protected static readonly string CohDataDirectory = @"C:\Games\CoH\data";

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenApplicationStarting()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
        }

        protected void GivenCatalogDataFileExists(string fileName)
        {
            Driver.SetCatalogDataFilePresent(fileName, true);
        }

        protected void GivenCatalogDataFileMissing(string fileName)
        {
            Driver.SetCatalogDataFilePresent(fileName, false);
        }

        protected void GivenResourceCatalogLoaded(string catalogType)
        {
            Driver.SetResourceCatalogState(catalogType, "loaded");
        }

        protected void GivenResourceCatalogNotLoaded(string catalogType)
        {
            Driver.SetResourceCatalogState(catalogType, "not loaded");
        }

        protected void GivenAllCatalogsLoaded()
        {
            Driver.SetResourceCatalogState("FX", "loaded");
            Driver.SetResourceCatalogState("Movement", "loaded");
            Driver.SetResourceCatalogState("Sound", "loaded");
        }

        protected void GivenAbilityEditorOpenForAbility(string abilityName)
        {
            Driver.OpenAbilityEditor(abilityName);
        }

        protected void GivenEmbeddedCsvHasData(string catalogType, string bundledData)
        {
            Driver.SetEmbeddedCsvPresent(catalogType, true);
        }

        protected void GivenEmbeddedCsvAbsent(string catalogType)
        {
            Driver.SetEmbeddedCsvPresent(catalogType, false);
        }

        protected void GivenResourcePickerShowing(string catalogType)
        {
            Driver.OpenResourcePicker(catalogType);
        }

        protected void GivenCatalogEmpty(string catalogType)
        {
            Driver.SetCatalogEntryCount(catalogType, 0);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenApplicationReadsDataFile(string fileName)
        {
            Driver.InvokeLoadCatalogFromFile(fileName);
        }

        protected void WhenApplicationSeedsCatalogFromEmbeddedData(string catalogType)
        {
            Driver.InvokeSeedCatalogFromCsv(catalogType);
        }

        protected void WhenApplicationStarts()
        {
            Driver.InvokeApplicationStartup();
        }

        protected void WhenGmSelectsAddResource(string resourceType)
        {
            Driver.InvokeAddResourceElement(resourceType);
        }

        protected void WhenGmSelectsResourceAndConfirms(string resourceType, string displayName)
        {
            Driver.SelectResourceInPicker(displayName);
            Driver.ConfirmResourcePicker();
        }

        protected void WhenGmDismissesPicker()
        {
            Driver.DismissResourcePicker();
        }

        protected void WhenResourcePickerInteractionAttempted()
        {
            Driver.AttemptResourcePickerInteraction();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenResourceCatalogHasState(string expectedState)
        {
            string actual = Driver.GetResourceCatalogLoadedState();
            Assert.AreEqual(expectedState, actual,
                string.Format("Catalog state: expected '{0}' got '{1}'", expectedState, actual));
        }

        protected void ThenResourcePickerEnabled(string resourceType)
        {
            Assert.IsTrue(Driver.IsResourcePickerEnabled(resourceType),
                string.Format("Resource picker for '{0}' should be enabled", resourceType));
        }

        protected void ThenResourcePickerDisabledOrNotReady(string resourceType)
        {
            Assert.IsFalse(Driver.IsResourcePickerEnabled(resourceType),
                string.Format("Resource picker for '{0}' should be disabled", resourceType));
        }

        protected void ThenResourcePickerShowsEntry(string displayName, string identifier)
        {
            Assert.IsTrue(Driver.ResourcePickerContainsEntry(displayName, identifier),
                string.Format("Picker missing entry: '{0}' / '{1}'", displayName, identifier));
        }

        protected void ThenElementAdded(string elementType, string resourceName)
        {
            Assert.IsTrue(Driver.IsElementInList(elementType, resourceName),
                string.Format("Expected element '{0}: {1}' in list", elementType, resourceName));
        }

        protected void ThenElementAtBottom(string resourceName)
        {
            Assert.IsTrue(Driver.IsElementAtBottomOfList(resourceName),
                string.Format("Element '{0}' should be at bottom of list", resourceName));
        }

        protected void ThenNoElementAdded()
        {
            Assert.IsFalse(Driver.WasElementAddedSinceLastCheck(),
                "Expected no element added");
        }

        protected void ThenPickerShowsEmptyState()
        {
            Assert.IsTrue(Driver.IsResourcePickerShowingEmptyState(),
                "Picker should show empty state");
        }

        protected void ThenAllResourcePickersEnabled()
        {
            Assert.IsTrue(Driver.IsResourcePickerEnabled("FX"), "FX picker should be enabled");
            Assert.IsTrue(Driver.IsResourcePickerEnabled("Movement"), "Movement picker should be enabled");
            Assert.IsTrue(Driver.IsResourcePickerEnabled("Sound"), "Sound picker should be enabled");
        }

        protected void ThenCatalogUnavailableReported(string catalogType)
        {
            string error = Driver.GetLastValidationMessage();
            Assert.IsNotNull(error, "Expected unavailable report");
            Assert.IsTrue(error.Contains(catalogType),
                string.Format("Error should mention '{0}'", catalogType));
        }

        protected void ThenEmbeddedCsvNotRead(string catalogType)
        {
            Assert.IsFalse(Driver.WasEmbeddedCsvRead(catalogType),
                string.Format("Embedded CSV for '{0}' should not be read", catalogType));
        }

        protected void ThenOperationBlockedWithNotReady()
        {
            string msg = Driver.GetLastValidationMessage();
            Assert.IsNotNull(msg, "Expected blocked indication");
            Assert.IsTrue(msg.Contains("not") || msg.Contains("ready"),
                "Expected not-ready indication");
        }
    }
}
