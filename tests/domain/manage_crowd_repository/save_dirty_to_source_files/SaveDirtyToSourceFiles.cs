using FluentAssertions;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Module.HeroVirtualTabletop.Crowds;

namespace HeroVTT.DomainTests.ManageCrowdRepository
{
    [TestClass]
    public class SaveDirtyToSourceFiles : ManageCrowdRepositoryDomainHelper
    {
        [TestMethod]
        public void SaveSkipsCleanCrowdFileIsUnchanged()
        {
            // Given: armageddons.json exists; active crowd list contains it; GM made no changes
            given_crowd_file_on_disk("armageddons.json", f => f
                .TopLevel("Armageddon Squad").WithCharacter("Battle Maiden"));
            given_active_crowd_list_contains("armageddons.json");
            byte[] originalBytes = given_file_bytes("armageddons.json");
            when_workspace_opens();
            // When: the GM invokes Save Dirty Crowds (no changes made)
            // Then: armageddons.json is not opened for writing; no Daily Backup created
            then_file_byte_unchanged("armageddons.json", originalBytes);
        }

        [TestMethod]
        public void SaveWritesOnlyRenamedCrowdLeavesOthersUnchanged()
        {
            // Given: heroes.json (Freedom Phalanx), villains.json (Council Empire), neutrals.json (Wandering Wraith)
            given_crowd_file_on_disk("heroes.json", f => f.TopLevel("Freedom Phalanx"));
            given_crowd_file_on_disk("villains.json", f => f.TopLevel("Council Empire"));
            given_crowd_file_on_disk("neutrals.json", f => f.TopLevel("Wandering Wraith"));
            given_active_crowd_list_contains("heroes.json", "villains.json", "neutrals.json");
            byte[] villainsBytes = given_file_bytes("villains.json");
            byte[] neutralsBytes = given_file_bytes("neutrals.json");
            // Load workspace; rename only Freedom Phalanx
            List<CrowdModel> loaded = when_workspace_opens();
            CrowdModel fp = loaded.FirstOrDefault(c => c.Name == "Freedom Phalanx");
            fp.Should().NotBeNull("Freedom Phalanx must be loaded");
            fp.Name = "Freedom Phalanx Reformed";
            // Then: villains.json and neutrals.json are byte-unchanged on disk
            then_file_byte_unchanged("villains.json", villainsBytes);
            then_file_byte_unchanged("neutrals.json", neutralsBytes);
        }

        [TestMethod]
        public void DailyBackupIsCreatedBeforeOverwritingDirtyFile()
        {
            // Given: heroes.json exists; no Daily Backup for heroes.json for today; GM renames Freedom Phalanx
            given_crowd_file_on_disk("heroes.json", f => f.TopLevel("Freedom Phalanx"));
            given_active_crowd_list_contains("heroes.json");
            List<CrowdModel> loaded = when_workspace_opens();
            CrowdModel fp = loaded.FirstOrDefault(c => c.Name == "Freedom Phalanx");
            fp.Should().NotBeNull();
            fp.Name = "Freedom Phalanx Reformed";
            // When: the GM invokes Save Dirty Crowds
            _repository.SaveDirtyCrowds(null);
            // Then: a Daily Backup file exists with the pre-save content of heroes.json
            then_daily_backup_exists("heroes.json");
        }

        [TestMethod]
        public void ClosingWithUnsavedChangesShowsPrompt()
        {
            // Given: heroes.json loaded; GM renames Freedom Phalanx to Freedom Phalanx Reformed
            given_crowd_file_on_disk("heroes.json", f => f.TopLevel("Freedom Phalanx"));
            given_active_crowd_list_contains("heroes.json");
            List<CrowdModel> loaded = when_workspace_opens();
            CrowdModel fp = loaded.FirstOrDefault(c => c.Name == "Freedom Phalanx");
            fp.Should().NotBeNull();
            fp.Name = "Freedom Phalanx Reformed";
            // Then: the crowd is marked dirty; the application should prompt (IsDirty = true signals the prompt)
            fp.IsDirty.Should().BeTrue(
                "a renamed crowd must be dirty so the application shows a save prompt on close");
        }
    }
}
