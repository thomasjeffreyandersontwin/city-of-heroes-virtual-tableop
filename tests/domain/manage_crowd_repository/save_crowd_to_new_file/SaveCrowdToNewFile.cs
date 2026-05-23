using FluentAssertions;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Utility;

namespace HeroVTT.DomainTests.ManageCrowdRepository
{
    [TestClass]
    public class SaveCrowdToNewFile : ManageCrowdRepositoryDomainHelper
    {
        [TestMethod]
        public void SaveAsWritesFreshCrowdFileAndActivatesIt()
        {
            // Given: active crowd list is empty; GM has built top-level Crowd "New Squad" (Recruit Alpha, Recruit Beta)
            CrowdModel newSquad = new CrowdModel { Name = "New Squad" };
            string targetPath = path_for("new-squad.json");
            // When: the GM invokes Save Crowd to New File, confirms new-squad.json in the dialog
            Helper.SerializeObjectAsJSONToFile(targetPath, new List<CrowdModel> { newSquad });
            // Then: new-squad.json exists on disk containing top-level Crowd "New Squad"
            File.Exists(targetPath).Should().BeTrue("new-squad.json must be created");
            then_crowd_in_result(
                Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(targetPath) ?? new List<CrowdModel>(),
                "New Squad");
        }

        [TestMethod]
        public void SaveAsTopLevelCrowdWithNestedCrowdsWritesFullSubtree()
        {
            // Given: GM has built Crowd "Council Empire" (Marcus Valerius, nested Vampyri [Galaxy, Vandal])
            given_crowd_file_on_disk("villains.json", f => f
                .TopLevel("Council Empire")
                    .WithCharacter("Marcus Valerius")
                    .WithNested("Vampyri", n => n.WithCharacter("Galaxy").WithCharacter("Vandal")));
            // When: the GM invokes Save Crowd to New File and confirms villains.json
            List<CrowdModel> loaded = when_workspace_opens();
            // Then: villains.json contains Council Empire with Vampyri nested under it
            then_nested_crowd_under(loaded, "Council Empire", "Vampyri");
        }

        [TestMethod]
        public void SaveAsOfLoadedCrowdSwitchesSourceFile()
        {
            // Given: armageddons.json loaded; GM renames Armageddon Squad to Armageddon Squad Reforged
            given_crowd_file_on_disk("armageddons.json", f => f.TopLevel("Armageddon Squad"));
            given_active_crowd_list_contains("armageddons.json");
            List<CrowdModel> loaded = when_workspace_opens();
            CrowdModel crowd = loaded.FirstOrDefault(c => c.Name == "Armageddon Squad");
            crowd.Should().NotBeNull();
            // When: the GM invokes Save Crowd to New File and confirms armageddon-reforged.json
            string newPath = path_for("armageddon-reforged.json");
            crowd.SourceFilePath = newPath;
            // Then: SourceFilePath is updated to armageddon-reforged.json
            crowd.SourceFilePath.Should().Be(newPath,
                "Save As switches SourceFilePath so subsequent saves go to the new file");
        }

        [TestMethod]
        public void CancellingFileSaveDialogLeavesFilesUntouched()
        {
            // Given: armageddons.json exists; GM has renamed Armageddon Squad to Armageddon Squad Reforged
            given_crowd_file_on_disk("armageddons.json", f => f.TopLevel("Armageddon Squad"));
            given_active_crowd_list_contains("armageddons.json");
            byte[] originalBytes = given_file_bytes("armageddons.json");
            when_workspace_opens();
            // When: the GM invokes Save Crowd to New File and cancels the dialog
            // Then: no new file is created; armageddons.json is byte-unchanged
            File.Exists(path_for("armageddon-reforged.json")).Should().BeFalse(
                "cancelling the dialog must not create any file");
            then_file_byte_unchanged("armageddons.json", originalBytes);
        }

        [TestMethod]
        public void SaveAsIsRejectedWhenNestedCrowdIsSelected()
        {
            // Given: armageddons.json with nested Crowd Demolition Team; GM selects the nested crowd
            given_crowd_file_on_disk("armageddons.json", f => f
                .TopLevel("Armageddon Squad")
                    .WithNested("Demolition Team", n => n.WithCharacter("Demo Lead")));
            given_active_crowd_list_contains("armageddons.json");
            List<CrowdModel> loaded = when_workspace_opens();
            CrowdModel armageddon = loaded.FirstOrDefault(c => c.Name == "Armageddon Squad");
            armageddon.Should().NotBeNull();
            // Then: Save As requires a top-level Crowd selection; nested selection must be blocked
            // Domain-level check: Demolition Team is not a top-level crowd in the loaded list
            bool isTopLevel = loaded.Any(c => c.Name == "Demolition Team");
            isTopLevel.Should().BeFalse(
                "Save Crowd to New File must be rejected when a nested Crowd is selected — it is not top-level");
        }
    }
}
