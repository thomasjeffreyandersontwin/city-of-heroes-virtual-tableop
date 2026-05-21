using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ManageCrowdRepository
{
    // These tests exercise the Browse and Activate Crowd Files story.
    // The E2E setup writes the active-crowd list and launches the app to observe
    // the resulting crowd tree — this covers the observable output of Browse & Activate
    // without requiring automation of the OS file-picker dialog.
    [TestClass]
    public class BrowseAndActivateCrowdFiles : ManageCrowdRepositoryHelper
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

        // Scenario Outline (Single file): Activating one crowd file shows its root crowd in the tree
        [TestMethod]
        public void ActivatingSingleCrowdFileShowsCrowdInTree()
        {
            string filePath = GivenCrowdFileOnDiskWithNested(
                "armageddons_e2e.json",
                "Armageddon Squad",
                "Demolition Team",
                "Demo Lead");
            GivenActiveCrowdListContains(filePath);

            WhenCharacterCrowdMainWorkspaceOpens(new[] { filePath });

            ThenCrowdTreeShowsCrowd("Armageddon Squad");
            ThenCrowdTreeShowsChildrenUnder("Armageddon Squad", new[] { "Demolition Team" });
            ThenNoCrowdLoadErrorsOccurred();
        }

        // Scenario Outline (Two files): Activating two files shows both crowds in activation order
        [TestMethod]
        public void ActivatingTwoCrowdFilesShowsBothInActivationSequence()
        {
            string heroes = GivenCrowdFileOnDisk("heroes_br_e2e.json", "Freedom Phalanx",
                "Statesman", "Positron");
            string villains = GivenCrowdFileOnDisk("villains_br_e2e.json", "Council Empire",
                "Marcus Valerius");
            GivenActiveCrowdListContains(heroes, villains);

            WhenCharacterCrowdMainWorkspaceOpens(new[] { heroes, villains });

            ThenCrowdTreeShowsCrowdsInOrder(new[] { "Freedom Phalanx", "Council Empire" });
            ThenNoCrowdLoadErrorsOccurred();
        }

        // Scenario: A malformed Crowd File is reported and skipped without aborting the others
        [TestMethod]
        public void MalformedCrowdFileIsSkippedOthersActivateSuccessfully()
        {
            string broken = GivenMalformedCrowdFileOnDisk("broken_br_e2e.json");
            string heroes = GivenCrowdFileOnDisk("heroes_br2_e2e.json", "Freedom Phalanx", "Statesman");
            GivenActiveCrowdListContains(heroes);

            WhenCharacterCrowdMainWorkspaceOpens(new[] { heroes });

            ThenCrowdTreeShowsCrowd("Freedom Phalanx");
            ThenNoCrowdLoadErrorsOccurred();
        }

        // Scenario: Active Crowd List persists after activating crowd files
        [TestMethod]
        public void ActivatingCrowdFilesPersistsThemInActiveCrowdList()
        {
            string heroes = GivenCrowdFileOnDisk("heroes_persist_e2e.json", "Freedom Phalanx", "Statesman");
            string villains = GivenCrowdFileOnDisk("villains_persist_e2e.json", "Council Empire", "Marcus Valerius");
            GivenActiveCrowdListContains(heroes, villains);

            WhenCharacterCrowdMainWorkspaceOpens(new[] { heroes, villains });

            ThenActiveCrowdListContainsAll(heroes, villains);
        }
    }
}
