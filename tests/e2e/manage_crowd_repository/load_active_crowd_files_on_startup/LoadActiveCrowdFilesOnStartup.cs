using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CrowdManagement.E2ETests.Support;

namespace CrowdManagement.E2ETests.ManageCrowdRepository
{
    [TestClass]
    public class LoadActiveCrowdFilesOnStartup : ManageCrowdRepositoryHelper
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

        // Scenario: An empty Active Crowd List loads no Crowds and no defaults
        [TestMethod]
        public void EmptyActiveCrowdListLoadsNoCrowdsAndNoDefaults()
        {
            GivenActiveCrowdListIsEmpty();

            WhenCharacterCrowdMainWorkspaceOpens();

            ThenCrowdTreeIsEmpty();
            ThenNoCrowdLoadErrorsOccurred();
        }

        // Scenario Outline (Single nested file): Loading a crowd file with nested structure
        // restores Council Empire → Vampyri (Galaxy, Vandal) + Galaxy Council (Black Swan)
        [TestMethod]
        public void LoadCrowdFileWithTwoNestedCrowdsRestoresFullHierarchy()
        {
            string filePath = GivenCrowdFileOnDiskWithMultipleNested(
                "villains_e2e.json",
                "Council Empire",
                new[] { "Marcus Valerius" },
                new List<System.Collections.Generic.KeyValuePair<string, string[]>>
                {
                    new System.Collections.Generic.KeyValuePair<string, string[]>(
                        "Vampyri", new[] { "Galaxy", "Vandal" }),
                    new System.Collections.Generic.KeyValuePair<string, string[]>(
                        "Galaxy Council", new[] { "Black Swan" })
                });
            GivenActiveCrowdListContains(filePath);

            WhenCharacterCrowdMainWorkspaceOpens(new[] { filePath });

            ThenCrowdTreeShowsCrowd("Council Empire");
            ThenCrowdTreeShowsChildrenUnder("Council Empire", new[] { "Vampyri", "Galaxy Council" });
            ThenNoCrowdLoadErrorsOccurred();
        }

        // Scenario Outline (Two files list order): heroes.json loads before villains.json
        [TestMethod]
        public void TwoCrowdFilesLoadInListOrderFreedomPhalanxBeforeCouncilEmpire()
        {
            string heroes = GivenCrowdFileOnDisk("heroes_e2e.json", "Freedom Phalanx", "Statesman");
            string villains = GivenCrowdFileOnDisk("villains_e2e2.json", "Council Empire", "Marcus Valerius");
            GivenActiveCrowdListContains(heroes, villains);

            WhenCharacterCrowdMainWorkspaceOpens(new[] { heroes, villains });

            ThenCrowdTreeShowsCrowdsInOrder(new[] { "Freedom Phalanx", "Council Empire" });
            ThenNoCrowdLoadErrorsOccurred();
        }

        // Scenario: A missing path on disk is reported and skipped, others still load
        [TestMethod]
        public void MissingPathInActiveCrowdListIsSkippedOthersLoad()
        {
            string heroes = GivenCrowdFileOnDisk("heroes_miss_e2e.json", "Freedom Phalanx", "Statesman");
            string missing = GivenCrowdFileExistsOnDisk("missing_e2e.json");
            GivenActiveCrowdListContains(heroes, missing);

            // missing_e2e.json does not exist on disk — the app skips it.
            // Pass expectedLoadedCrowds=1 so WaitForCrowdsToLoad polls for the right count.
            WhenCharacterCrowdMainWorkspaceOpens(new[] { heroes, missing }, 1);

            ThenCrowdTreeShowsCrowd("Freedom Phalanx");
            ThenCrowdTreeDoesNotShowCrowd("Missing Crowd");
        }

        // Scenario: A malformed active Crowd File is reported and skipped
        [TestMethod]
        public void MalformedActiveCrowdFileIsSkippedOthersLoad()
        {
            string heroes = GivenCrowdFileOnDisk("heroes_mal_e2e.json", "Freedom Phalanx", "Statesman");
            string corrupt = GivenMalformedCrowdFileOnDisk("corrupt_e2e.json");
            GivenActiveCrowdListContains(heroes, corrupt);

            // corrupt_e2e.json has invalid JSON — the app skips it and loads only heroes.
            WhenCharacterCrowdMainWorkspaceOpens(new[] { heroes, corrupt }, 1);

            ThenCrowdTreeShowsCrowd("Freedom Phalanx");
            ThenCrowdTreeDoesNotShowCrowd("Council Empire");
        }
    }
}
