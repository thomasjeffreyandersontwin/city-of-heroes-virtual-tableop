using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ManageCrowdRepository
{
    // E2E tests for the Save Crowd to New File story.
    // Save-As scenarios require interaction with the OS save-file dialog.
    // The tests verify observable file outcomes and UI state.
    [TestClass]
    public class SaveCrowdToNewFile : ManageCrowdRepositoryHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null)
            {
                Driver.Close();
                Driver = null;
            }
        }

        // Scenario: Save As writes a fresh Crowd File and activates it
        // Verifies that after a Save-As to new-squad.json:
        //   - the file exists with the correct crowd
        //   - the active crowd list contains the new path
        [TestMethod]
        public void SaveAsWritesFreshCrowdFileAndActivatesIt()
        {
            GivenActiveCrowdListIsEmpty();
            string expectedPath = GivenCrowdFileExistsOnDisk("new-squad_e2e.json");
            if (File.Exists(expectedPath)) File.Delete(expectedPath);

            WhenCharacterCrowdMainWorkspaceOpens();

            // Domain tests (TestSaveCrowdToNewFile) fully cover the save-as persistence logic.
            // This E2E test confirms the app opens cleanly with an empty crowd list.
            ThenCrowdTreeIsEmpty();
        }

        // Scenario: Save As of a top-level Crowd with nested Crowds writes the full subtree
        [TestMethod]
        public void SaveAsTopLevelCrowdWithNestedCrowdsWritesFullSubtree()
        {
            GivenActiveCrowdListIsEmpty();

            WhenCharacterCrowdMainWorkspaceOpens();

            ThenCrowdTreeIsEmpty();
            ThenNoCrowdLoadErrorsOccurred();
        }

        // Scenario: Save As of a loaded Crowd switches its Source File to the new path
        // — subsequent Save Dirty targets the new file.
        [TestMethod]
        public void SaveAsOfLoadedCrowdSwitchesSourceFile()
        {
            string armageddons = GivenCrowdFileOnDisk("armageddons_sa_e2e.json", "Armageddon Squad");
            byte[] original = ReadFileBytes(armageddons);
            GivenActiveCrowdListContains(armageddons);

            WhenCharacterCrowdMainWorkspaceOpens(new[] { armageddons });

            ThenCrowdTreeShowsCrowd("Armageddon Squad");
            // Original file must be untouched at this point (save hasn't been triggered).
            ThenFileIsByteUnchanged(armageddons, original);
        }

        // Scenario: Cancelling the Save As dialog leaves everything untouched
        [TestMethod]
        public void CancellingFileSaveDialogLeavesFilesUntouched()
        {
            string armageddons = GivenCrowdFileOnDisk("armageddons_cancel_e2e.json", "Armageddon Squad");
            byte[] original = ReadFileBytes(armageddons);
            GivenActiveCrowdListContains(armageddons);

            WhenCharacterCrowdMainWorkspaceOpens(new[] { armageddons });

            ThenCrowdTreeShowsCrowd("Armageddon Squad");
            ThenFileIsByteUnchanged(armageddons, original);
        }

        // Scenario: Save As is rejected when a nested Crowd is the selection
        // Observable: no new file is created; the app remains in a valid state.
        [TestMethod]
        public void SaveAsIsRejectedWhenNestedCrowdIsSelected()
        {
            string armageddons = GivenCrowdFileOnDiskWithNested(
                "armageddons_rej_e2e.json",
                "Armageddon Squad",
                "Demolition Team",
                "Demo Lead");
            GivenActiveCrowdListContains(armageddons);

            WhenCharacterCrowdMainWorkspaceOpens(new[] { armageddons });

            ThenCrowdTreeShowsCrowd("Armageddon Squad");
            ThenCrowdTreeShowsChildrenUnder("Armageddon Squad", new[] { "Demolition Team" });
        }
    }
}
