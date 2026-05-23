using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.Crowds;

namespace HeroVTT.DomainTests.ManageCrowdRepository
{
    [TestClass]
    public class LoadActiveCrowdFilesOnStartup : ManageCrowdRepositoryDomainHelper
    {
        [TestMethod]
        public void EmptyActiveCrowdListLoadsNoCrowdsAndNoDefaults()
        {
            // Given: the Active Crowd List file "C:\COH\data\active-crowds.json" does not exist
            // When: the Character Crowd Main Workspace opens
            List<CrowdModel> loaded = when_workspace_opens();
            // Then: the Crowd Tree shows only the protected All Characters Crowd with no Characters
            then_no_crowds_loaded(loaded);
        }

        [TestMethod]
        public void LoadCrowdFileWithTwoNestedCrowdsRestoresFullHierarchy()
        {
            // Given: villains.json exists with Council Empire, nested Vampyri (Galaxy, Vandal), Galaxy Council (Black Swan)
            given_crowd_file_on_disk("villains.json", f => f
                .TopLevel("Council Empire")
                    .WithCharacter("Marcus Valerius")
                    .WithNested("Vampyri", n => n.WithCharacter("Galaxy").WithCharacter("Vandal"))
                    .WithNested("Galaxy Council", n => n.WithCharacter("Black Swan")));
            given_active_crowd_list_contains("villains.json");
            // When: the Character Crowd Main Workspace opens
            List<CrowdModel> loaded = when_workspace_opens();
            // Then: the Crowd Tree shows Council Empire with nested Vampyri and Galaxy Council
            then_crowd_in_result(loaded, "Council Empire");
            then_nested_crowd_under(loaded, "Council Empire", "Vampyri");
        }

        [TestMethod]
        public void TwoCrowdFilesLoadInListOrderFreedomPhalanxBeforeCouncilEmpire()
        {
            // Given: heroes.json listed before villains.json in the active crowd list
            given_crowd_file_on_disk("heroes.json", f => f.TopLevel("Freedom Phalanx").WithCharacter("Statesman"));
            given_crowd_file_on_disk("villains.json", f => f.TopLevel("Council Empire").WithCharacter("Marcus Valerius"));
            given_active_crowd_list_contains("heroes.json", "villains.json");
            // When: the Character Crowd Main Workspace opens
            List<CrowdModel> loaded = when_workspace_opens();
            // Then: the Crowd Tree shows Freedom Phalanx before Council Empire (list order)
            then_crowd_before(loaded, "Freedom Phalanx", "Council Empire");
        }

        [TestMethod]
        public void MissingPathInActiveCrowdListIsSkippedOthersLoad()
        {
            // Given: heroes.json exists; missing.json does not exist on disk
            given_crowd_file_on_disk("heroes.json", f => f.TopLevel("Freedom Phalanx").WithCharacter("Statesman"));
            given_active_crowd_list_contains("heroes.json", "missing.json");
            // When: the Character Crowd Main Workspace opens
            List<CrowdModel> loaded = when_workspace_opens();
            // Then: a warning names missing.json; Freedom Phalanx still loads; missing path stays in active list
            then_crowd_in_result(loaded, "Freedom Phalanx");
            then_active_list_contains("missing.json");
        }

        [TestMethod]
        public void MalformedActiveCrowdFileIsSkippedOthersLoad()
        {
            // Given: heroes.json valid; corrupt.json contains malformed JSON
            given_crowd_file_on_disk("heroes.json", f => f.TopLevel("Freedom Phalanx"));
            given_malformed_crowd_file_on_disk("corrupt.json");
            given_active_crowd_list_contains("heroes.json", "corrupt.json");
            // When: the Character Crowd Main Workspace opens
            List<CrowdModel> loaded = when_workspace_opens();
            // Then: an error notification names corrupt.json; Freedom Phalanx still loads
            then_crowd_in_result(loaded, "Freedom Phalanx");
        }
    }
}
