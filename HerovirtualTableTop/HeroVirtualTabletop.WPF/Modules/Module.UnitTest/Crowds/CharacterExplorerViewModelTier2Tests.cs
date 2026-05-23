// ─────────────────────────────────────────────────────────────────────────────
// Tier 2 (ViewModel + Domain) — CharacterExplorerViewModel
//
// Pattern: real CrowdModel / CrowdMemberModel domain objects wired directly to
// CharacterExplorerViewModel.CrowdCollection.  The COH seam (ICrowdRepository)
// is still stubbed with the Moq-backed crowdRepositoryMock from BaseCrowdTest.
//
// What these tests uniquely verify versus the existing CrowdTestSuite:
//   • Domain post-state (IsDirty, OptionGroup count) — not just mock.Verify calls.
//   • The ViewModel binding and the domain object agree after each operation.
//
// Architecture reference: docs/architecture/architecture-reference.md §Testing
// SBE reference:          docs/increment-1/specification-by-example-increment-1.md
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;

namespace Module.UnitTest.Crowds
{
    // ─── Story: Manage Characters in a Crowd ─────────────────────────────────
    //
    // SBE: "When the GM adds a character, then the character has exactly three
    //       option groups: Identities, Powers, Movements."
    //
    // Tier 2 angle: AddCharacterCommand fires on the real ViewModel; we assert
    // domain post-state (OptionGroup count) through the ViewModel's binding.
    [TestClass]
    public class ManageCharacterInCrowd_ViewModelTests : BaseCrowdTest
    {
        [TestInitialize]
        public void Setup()
        {
            InitializeDefaultList();
            InitializeCrowdRepositoryMockWithDefaultList();
        }

        [TestMethod]
        public void WhenGmAddsCharacter_ThenNewCharacterHasThreeOptionGroups()
        {
            // Arrange: select any crowd that can accept a new member
            var gotham = characterExplorerViewModel.CrowdCollection
                .FirstOrDefault(c => c.Name == "Gotham City");
            characterExplorerViewModel.SelectedCrowdModel = gotham;
            characterExplorerViewModel.SelectedCrowdMemberModel = null;

            int membersBefore = gotham.CrowdMemberCollection.Count(m => m is CrowdMemberModel);

            // Act: ViewModel command → real domain CrowdModel.Add() → CrowdMemberModel ctor
            characterExplorerViewModel.AddCharacterCommand.Execute(null);

            // Assert: binding — one more character in the crowd
            int membersAfter = gotham.CrowdMemberCollection.Count(m => m is CrowdMemberModel);
            membersAfter.Should().Be(membersBefore + 1,
                "AddCharacterCommand must create exactly one new character in the selected crowd");

            // Assert: domain post-state — new character carries the three canonical option groups
            var newChar = gotham.CrowdMemberCollection
                .OfType<CrowdMemberModel>()
                .Last();
            var _ = newChar.AvailableIdentities;   // force lazy creation
            var __ = newChar.AnimatedAbilities;
            var ___ = newChar.Movements;

            newChar.OptionGroups.Count().Should().Be(3,
                "every character must have exactly three option groups (Identities, Powers, Movements)");
        }

        [TestMethod]
        public void WhenGmAddsCharacter_ThenIdentitiesPowersAndMovementsGroupsExist()
        {
            var gotham = characterExplorerViewModel.CrowdCollection
                .FirstOrDefault(c => c.Name == "Gotham City");
            characterExplorerViewModel.SelectedCrowdModel = gotham;
            characterExplorerViewModel.SelectedCrowdMemberModel = null;

            characterExplorerViewModel.AddCharacterCommand.Execute(null);

            var newChar = gotham.CrowdMemberCollection.OfType<CrowdMemberModel>().Last();

            newChar.AvailableIdentities.Should().NotBeNull("Identities group must always exist");
            newChar.AnimatedAbilities.Should().NotBeNull("Powers group must always exist");
            newChar.Movements.Should().NotBeNull("Movements group must always exist");

            newChar.AvailableIdentities.Name.Should()
                .Be(Constants.IDENTITY_OPTION_GROUP_NAME, "Identities group must use the canonical name");
            newChar.AnimatedAbilities.Name.Should()
                .Be(Constants.ABILITY_OPTION_GROUP_NAME, "Powers group must use the canonical name");
            newChar.Movements.Name.Should()
                .Be(Constants.MOVEMENT_OPTION_GROUP_NAME, "Movements group must use the canonical name");
        }

        [TestMethod]
        public void WhenGmAddsCharacterToAllCharacters_ThenCharacterAlsoAppearInAllCharacters()
        {
            // When no crowd selected, the character lands under All Characters crowd
            characterExplorerViewModel.SelectedCrowdModel = null;
            characterExplorerViewModel.SelectedCrowdMemberModel = null;

            characterExplorerViewModel.AddCharacterCommand.Execute(null);

            var allChars = characterExplorerViewModel.CrowdCollection
                .FirstOrDefault(c => c.Name == Constants.ALL_CHARACTER_CROWD_NAME);
            allChars.Should().NotBeNull();
            allChars.CrowdMemberCollection.Should().Contain(
                m => m is CrowdMemberModel,
                "newly added character must appear in All Characters crowd");
        }
    }

    // ─── Story: Rename Character or Crowd ────────────────────────────────────
    //
    // SBE: "When the GM renames a crowd, the crowd is marked dirty."
    //
    // Tier 2 angle: rename flows through SubmitCharacterCrowdRenameCommand;
    // we assert domain post-state (IsDirty on the parent CrowdModel).
    [TestClass]
    public class RenameCrowd_ViewModelTests : BaseCrowdTest
    {
        private CrowdModel _gotham;

        [TestInitialize]
        public void Setup()
        {
            InitializeDefaultList();
            InitializeCrowdRepositoryMockWithDefaultList();
            _gotham = characterExplorerViewModel.CrowdCollection
                .FirstOrDefault(c => c.Name == "Gotham City");
            _gotham.IsDirty = false;
        }

        [TestMethod]
        public void WhenGmRenamesCrowd_ThenCrowdIsDirty()
        {
            RunOnStaThread(() =>
            {
                characterExplorerViewModel.SelectedCrowdModel = _gotham;
                characterExplorerViewModel.SelectedCrowdMemberModel = null;
                characterExplorerViewModel.EnterEditModeCommand.Execute(null);

                var txtBox = new TextBox { Text = "Gotham City Renamed" };
                characterExplorerViewModel.SubmitCharacterCrowdRenameCommand.Execute(txtBox);

                _gotham.IsDirty.Should().BeTrue(
                    "renaming a crowd must mark it dirty so the save cycle picks it up");
            });
        }

        [TestMethod]
        public void WhenGmRenamesCharacter_ThenParentCrowdIsDirty()
        {
            RunOnStaThread(() =>
            {
                var batman = _gotham.CrowdMemberCollection
                    .OfType<CrowdMemberModel>()
                    .FirstOrDefault(m => m.Name == "Batman");

                characterExplorerViewModel.SelectedCrowdModel = _gotham;
                characterExplorerViewModel.SelectedCrowdMemberModel = batman;
                characterExplorerViewModel.EnterEditModeCommand.Execute(null);

                var txtBox = new TextBox { Text = "Dark Knight" };
                characterExplorerViewModel.SubmitCharacterCrowdRenameCommand.Execute(txtBox);

                _gotham.IsDirty.Should().BeTrue(
                    "renaming a character must mark its parent crowd dirty");
            });
        }

        [TestMethod]
        public void WhenGmRenamesCrowdToExistingName_ThenCrowdIsNotDirty()
        {
            RunOnStaThread(() =>
            {
                characterExplorerViewModel.SelectedCrowdModel = _gotham;
                characterExplorerViewModel.SelectedCrowdMemberModel = null;
                characterExplorerViewModel.EnterEditModeCommand.Execute(null);

                // "League of Shadows" already exists — rename must be rejected
                InitializeMessageBoxService(MessageBoxResult.OK);
                var txtBox = new TextBox { Text = "League of Shadows" };
                characterExplorerViewModel.SubmitCharacterCrowdRenameCommand.Execute(txtBox);

                _gotham.IsDirty.Should().BeFalse(
                    "a rejected duplicate rename must not mark the crowd dirty");
            });
        }
    }

    // ─── Story: Delete Character from Crowd ──────────────────────────────────
    //
    // SBE: "When the GM deletes a character, the crowd is marked dirty and the
    //       character is no longer in the crowd."
    //
    // Tier 2 angle: DeleteCharacterCrowdCommand fires; we assert both ViewModel
    // binding state and domain post-state (IsDirty, member absence).
    [TestClass]
    public class DeleteCharacterFromCrowd_ViewModelTests : BaseCrowdTest
    {
        private CrowdModel _gotham;
        private CrowdMemberModel _batman;

        [TestInitialize]
        public void Setup()
        {
            InitializeDefaultList();
            InitializeCrowdRepositoryMockWithDefaultList();
            _gotham = characterExplorerViewModel.CrowdCollection
                .FirstOrDefault(c => c.Name == "Gotham City");
            _batman = _gotham.CrowdMemberCollection
                .OfType<CrowdMemberModel>()
                .FirstOrDefault(m => m.Name == "Batman");
            _gotham.IsDirty = false;
        }

        [TestMethod]
        public void WhenGmDeletesCharacter_ThenCharacterRemovedFromCrowdBinding()
        {
            InitializeMessageBoxService(MessageBoxResult.Yes);

            characterExplorerViewModel.SelectedCrowdModel = _gotham;
            characterExplorerViewModel.SelectedCrowdMemberModel = _batman;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);

            characterExplorerViewModel.CrowdCollection
                .FirstOrDefault(c => c.Name == "Gotham City")
                .CrowdMemberCollection.OfType<CrowdMemberModel>()
                .Should().NotContain(m => m.Name == "Batman",
                    "the deleted character must no longer appear in the crowd's member collection");
        }

        [TestMethod]
        public void WhenGmDeletesCharacter_ThenParentCrowdIsDirty()
        {
            InitializeMessageBoxService(MessageBoxResult.Yes);

            characterExplorerViewModel.SelectedCrowdModel = _gotham;
            characterExplorerViewModel.SelectedCrowdMemberModel = _batman;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);

            _gotham.IsDirty.Should().BeTrue(
                "removing a character from a crowd must mark the crowd dirty");
        }
    }

    // ─── Story: Clone Character ───────────────────────────────────────────────
    //
    // SBE: "When the GM clones a character, the clone appears in the target crowd
    //       with a numbered suffix."
    //
    // Tier 2 angle: CloneCharacterCrowdCommand + PasteCharacterCrowdCommand;
    // assert the clone's OptionGroups are also initialised (invariant survives).
    [TestClass]
    public class CloneCharacter_ViewModelTests : BaseCrowdTest
    {
        [TestInitialize]
        public void Setup()
        {
            InitializeDefaultList();
            InitializeCrowdRepositoryMockWithDefaultList();
        }

        [TestMethod]
        public void WhenGmClonesAndPastesCharacter_ThenCloneHasThreeOptionGroups()
        {
            var gotham = characterExplorerViewModel.CrowdCollection
                .FirstOrDefault(c => c.Name == "Gotham City");
            var batman = gotham.CrowdMemberCollection
                .OfType<CrowdMemberModel>()
                .FirstOrDefault(m => m.Name == "Batman");

            var league = characterExplorerViewModel.CrowdCollection
                .FirstOrDefault(c => c.Name == "League of Shadows");

            // Clone Batman
            characterExplorerViewModel.SelectedCrowdModel = gotham;
            characterExplorerViewModel.SelectedCrowdMemberModel = batman;
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);

            // Paste into League of Shadows
            characterExplorerViewModel.SelectedCrowdModel = league;
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);

            var clone = league.CrowdMemberCollection
                .OfType<CrowdMemberModel>()
                .FirstOrDefault(m => m.Name == "Batman (1)");
            clone.Should().NotBeNull("cloned character must appear with a numbered suffix in the target crowd");

            // Force OptionGroup lazy creation and assert invariant
            var _ = clone.AvailableIdentities;
            var __ = clone.AnimatedAbilities;
            var ___ = clone.Movements;

            clone.OptionGroups.Count().Should().Be(3,
                "a cloned character must carry all three canonical option groups");
        }
    }
}
