using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.Crowds;

namespace HeroVTT.DomainTests.ManageCrowdRepository
{
    [TestClass]
    public class BrowseAndActivateCrowdFiles : ManageCrowdRepositoryDomainHelper
    {
        [TestMethod]
        public void ActivatingSingleCrowdFileShowsCrowdInTree()
        {
            // Given: armageddons.json exists on disk containing top-level Crowd Armageddon Squad
            given_crowd_file_on_disk("armageddons.json", f => f
                .TopLevel("Armageddon Squad")
                    .WithCharacter("Battle Maiden")
                    .WithCharacter("Manticore")
                    .WithNested("Demolition Team", n => n.WithCharacter("Demo Lead")));
            given_active_crowd_list_contains("armageddons.json");
            // When: the GM activates the crowd file (workspace opens loading active list)
            List<CrowdModel> loaded = when_workspace_opens();
            // Then: the Crowd Tree shows Crowd Armageddon Squad with nested Demolition Team
            then_crowd_in_result(loaded, "Armageddon Squad");
            then_nested_crowd_under(loaded, "Armageddon Squad", "Demolition Team");
        }

        [TestMethod]
        public void ActivatingTwoCrowdFilesShowsBothInActivationSequence()
        {
            // Given: heroes.json and villains.json exist on disk
            given_crowd_file_on_disk("heroes.json", f => f
                .TopLevel("Freedom Phalanx").WithCharacter("Statesman").WithCharacter("Positron"));
            given_crowd_file_on_disk("villains.json", f => f
                .TopLevel("Council Empire").WithCharacter("Marcus Valerius"));
            given_active_crowd_list_contains("heroes.json", "villains.json");
            // When: the GM activates both crowd files
            List<CrowdModel> loaded = when_workspace_opens();
            // Then: the Crowd Tree shows both Freedom Phalanx and Council Empire in activation sequence
            then_crowd_in_result(loaded, "Freedom Phalanx");
            then_crowd_in_result(loaded, "Council Empire");
            then_crowd_before(loaded, "Freedom Phalanx", "Council Empire");
        }

        [TestMethod]
        public void MalformedCrowdFileIsSkippedOthersActivateSuccessfully()
        {
            // Given: broken.json contains malformed JSON; heroes.json contains Freedom Phalanx (Statesman)
            given_malformed_crowd_file_on_disk("broken.json");
            given_crowd_file_on_disk("heroes.json", f => f
                .TopLevel("Freedom Phalanx").WithCharacter("Statesman"));
            given_active_crowd_list_contains("broken.json", "heroes.json");
            // When: the GM activates both files
            List<CrowdModel> loaded = when_workspace_opens();
            // Then: an error notification names broken.json; active list contains heroes.json; Freedom Phalanx loads
            then_crowd_in_result(loaded, "Freedom Phalanx");
            then_active_list_contains("heroes.json");
        }

        [TestMethod]
        public void ActivatingCrowdFilesPersistsThemInActiveCrowdList()
        {
            // Given: armageddons.json exists on disk; the persisted Active Crowd List is empty
            given_crowd_file_on_disk("armageddons.json", f => f
                .TopLevel("Armageddon Squad").WithCharacter("Battle Maiden"));
            given_active_crowd_list_contains("armageddons.json");
            // When: the GM activates armageddons.json
            when_workspace_opens();
            // Then: the persisted Active Crowd List contains armageddons.json
            then_active_list_contains("armageddons.json");
        }
    }
}
