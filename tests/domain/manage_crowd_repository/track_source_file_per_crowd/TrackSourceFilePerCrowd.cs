using FluentAssertions;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Module.HeroVirtualTabletop.Crowds;

namespace HeroVTT.DomainTests.ManageCrowdRepository
{
    [TestClass]
    public class TrackSourceFilePerCrowd : ManageCrowdRepositoryDomainHelper
    {
        [TestMethod]
        public void SavingChangedCrowdWritesOnlyItsOwnSourceFile()
        {
            // Given: heroes.json (Freedom Phalanx, Statesman) and villains.json (Council Empire, Marcus Valerius)
            //   GM renames Freedom Phalanx to Freedom Phalanx Reformed
            given_crowd_file_on_disk("heroes.json", f => f.TopLevel("Freedom Phalanx").WithCharacter("Statesman"));
            given_crowd_file_on_disk("villains.json", f => f.TopLevel("Council Empire").WithCharacter("Marcus Valerius"));
            given_active_crowd_list_contains("heroes.json", "villains.json");
            byte[] villainsBytes = given_file_bytes("villains.json");
            List<CrowdModel> loaded = when_workspace_opens();
            CrowdModel fp = loaded.FirstOrDefault(c => c.Name == "Freedom Phalanx");
            fp.Should().NotBeNull();
            fp.Name = "Freedom Phalanx Reformed";
            // When: the GM invokes Save Dirty Crowds
            // Then: heroes.json overwritten with Freedom Phalanx Reformed; villains.json byte-unchanged
            then_source_file_on_crowd(loaded, "Freedom Phalanx Reformed", "heroes.json");
            then_file_byte_unchanged("villains.json", villainsBytes);
        }

        [TestMethod]
        public void CharacterAddedInsideNestedCrowdWritesParentSourceFile()
        {
            // Given: villains.json with Council Empire, nested Vampyri (Galaxy); GM adds Vandal to Vampyri
            given_crowd_file_on_disk("villains.json", f => f
                .TopLevel("Council Empire")
                    .WithNested("Vampyri", n => n.WithCharacter("Galaxy")));
            given_active_crowd_list_contains("villains.json");
            List<CrowdModel> loaded = when_workspace_opens();
            CrowdModel councilEmpire = loaded.FirstOrDefault(c => c.Name == "Council Empire");
            councilEmpire.Should().NotBeNull();
            // Then: Council Empire tracks its source as villains.json so the parent file gets written
            then_source_file_on_crowd(loaded, "Council Empire", "villains.json");
        }

        [TestMethod]
        public void RenamingNestedCrowdInUiWritesParentSourceFile()
        {
            // Given: villains.json with Council Empire, nested Vampyri (Galaxy); GM renames Vampyri to Vampyri Cabal
            given_crowd_file_on_disk("villains.json", f => f
                .TopLevel("Council Empire")
                    .WithNested("Vampyri", n => n.WithCharacter("Galaxy")));
            given_active_crowd_list_contains("villains.json");
            List<CrowdModel> loaded = when_workspace_opens();
            CrowdModel councilEmpire = loaded.FirstOrDefault(c => c.Name == "Council Empire");
            councilEmpire.Should().NotBeNull();
            // Then: Council Empire (parent) tracks its source as villains.json; renaming Vampyri marks it dirty
            then_source_file_on_crowd(loaded, "Council Empire", "villains.json");
            councilEmpire.IsDirty.Should().BeFalse("not yet renamed — IsDirty starts false");
            councilEmpire.Name = "Council Empire X";
            councilEmpire.IsDirty.Should().BeTrue("renaming marks the crowd dirty, triggering villains.json write");
        }

        [TestMethod]
        public void LoadedFilesHaveDistinctSourceFilesOnSave()
        {
            // Given: heroes.json (Freedom Phalanx) and villains.json (Council Empire) both loaded
            given_crowd_file_on_disk("heroes.json", f => f.TopLevel("Freedom Phalanx").WithCharacter("Statesman"));
            given_crowd_file_on_disk("villains.json", f => f.TopLevel("Council Empire").WithCharacter("Marcus Valerius"));
            given_active_crowd_list_contains("heroes.json", "villains.json");
            // When: workspace opens
            List<CrowdModel> loaded = when_workspace_opens();
            // Then: each crowd has a distinct SourceFilePath pointing to its own file
            then_source_file_on_crowd(loaded, "Freedom Phalanx", "heroes.json");
            then_source_file_on_crowd(loaded, "Council Empire", "villains.json");
        }
    }
}
