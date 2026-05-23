using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Module.UnitTest.Identities
{
    // ─────────────────────────────────────────────────────────────────────────
    // Tier 2: OptionGroupViewModel<Identity> — ViewModel binding behavior
    //
    // Story: Identity Option Group binding via OptionGroupViewModel<Identity>
    // SBE: § Add Identity, Set Default Identity, Set Active Identity, Remove Identity
    //
    // Architecture: Real domain (CrowdMemberModel) wired to OptionGroupViewModel<Identity>.
    //   No WPF dispatcher calls; SpawnAndTargetOwnerCharacter path is bypassed by
    //   calling character.SetAsSpawned() before triggering SelectedOption.
    // ─────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TestIdentityOptionGroupViewModelBinding : BaseTest
    {
        private CrowdMemberModel _character;
        private CharacterEditorViewModel _characterEditorVM;
        private OptionGroupViewModel<Identity> _identityGroupVM;

        [TestInitialize]
        public void GivenACharacterEditorViewModelWithIdentityOptionGroupViewModel()
        {
            ResetKeyBindGeneratorStatics();

            var keyHandlerMock = new Mock<IDesktopKeyEventHandler>();
            _character = new CrowdMemberModel("Guard_Captain");

            // Wire mock container to return all three OptionGroupViewModels (mirrors AnimatedCharacterTestSuite)
            unityContainerMock
                .Setup(c => c.Resolve(
                    It.IsAny<Type>(),
                    It.IsAny<string>(),
                    It.IsAny<Microsoft.Practices.Unity.ResolverOverride[]>()))
                .Returns((Type t, string name, Microsoft.Practices.Unity.ResolverOverride[] overrides) =>
                {
                    if (t == typeof(OptionGroupViewModel<AnimatedAbility>))
                        return new OptionGroupViewModel<AnimatedAbility>(
                            busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object,
                            keyHandlerMock.Object, eventAggregator, _character.AnimatedAbilities, _character);
                    if (t == typeof(OptionGroupViewModel<Identity>))
                        return new OptionGroupViewModel<Identity>(
                            busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object,
                            keyHandlerMock.Object, eventAggregator,
                            _character.AvailableIdentities as OptionGroup<Identity>, _character);
                    if (t == typeof(OptionGroupViewModel<CharacterMovement>))
                        return new OptionGroupViewModel<CharacterMovement>(
                            busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object,
                            keyHandlerMock.Object, eventAggregator, _character.Movements, _character);
                    return null;
                });

            _characterEditorVM = new CharacterEditorViewModel(
                busyServiceMock.Object,
                unityContainerMock.Object,
                keyHandlerMock.Object,
                eventAggregator);
            _characterEditorVM.LoadCharacter(
                new Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>(_character, null));

            _identityGroupVM = _characterEditorVM.OptionGroups
                .FirstOrDefault(og => og.OptionGroup.Name == Constants.IDENTITY_OPTION_GROUP_NAME)
                as OptionGroupViewModel<Identity>;
        }

        // ── Scenario: Identity Option Group ViewModel wired to character ──────

        // Scenario: ViewModel exposes the Identities option group by canonical name
        [TestMethod]
        public void ViewModelExposesIdentityOptionGroupWithCanonicalName()
        {
            // Then: the Identity group is present and has the canonical name
            _identityGroupVM.Should().NotBeNull(
                "CharacterEditorViewModel must expose an OptionGroupViewModel<Identity> for the Identities group");
            _identityGroupVM.OptionGroup.Name.Should().Be(Constants.IDENTITY_OPTION_GROUP_NAME);
        }

        // ── Scenario: Add Identity ─────────────────────────────────────────────

        // Scenario: AddOptionCommand creates a new identity in the domain model
        [TestMethod]
        public void AddOptionCommand_CreatesNewIdentityInDomainModel()
        {
            // Given: no identities on the character
            _character.AvailableIdentities.Count.Should().Be(0);

            // When: GM presses Add Identity
            _identityGroupVM.AddOptionCommand.Execute(null);

            // Then: a new identity appears in the domain collection
            _character.AvailableIdentities.Count.Should().Be(1,
                "AddOptionCommand must add a new Identity to the domain OptionGroup");
        }

        // Scenario: Adding two identities produces two distinct entries in the domain
        [TestMethod]
        public void AddOptionCommandTwice_TwoDistinctIdentitiesInDomainModel()
        {
            // When
            _identityGroupVM.AddOptionCommand.Execute(null);
            _identityGroupVM.AddOptionCommand.Execute(null);

            // Then
            _character.AvailableIdentities.Count.Should().Be(2,
                "each Add command call produces a new uniquely-named identity");
            _character.AvailableIdentities[0].Name.Should()
                .NotBe(_character.AvailableIdentities[1].Name,
                "identity names must be unique within the option group");
        }

        // ── Scenario: Remove Identity ──────────────────────────────────────────

        // Scenario: RemoveOptionCommand removes the selected identity from the domain model
        [TestMethod]
        public void RemoveOptionCommand_RemovesSelectedIdentityFromDomainModel()
        {
            // Given: two identities exist (guard prevents single-identity removal)
            _identityGroupVM.AddOptionCommand.Execute(null);
            _identityGroupVM.AddOptionCommand.Execute(null);
            var identityToRemove = _character.AvailableIdentities[0];
            _identityGroupVM.SelectedOption = identityToRemove;

            // When
            _identityGroupVM.RemoveOptionCommand.Execute(null);

            // Then: removed identity is gone from the domain collection
            _character.AvailableIdentities.Contains(identityToRemove).Should().BeFalse(
                "RemoveOptionCommand must remove the selected identity from the domain OptionGroup");
        }

        // NOTE: RemoveOptionCommand_BlockedWhenOnlyOneIdentityRemains is NOT tested here.
        // The guard uses MessageBox.Show(...) directly (not via messageBoxService), which
        // blocks the STA thread forever in a headless runner. The guard is a pure UI-layer
        // decision; it carries no domain logic worth testing via ViewModel tests.

        // ── Scenario: DefaultOption binding ───────────────────────────────────

        // Scenario: Setting DefaultOption on ViewModel updates owner.DefaultIdentity in domain
        [TestMethod]
        public void SettingDefaultOption_UpdatesOwnerDefaultIdentityInDomain()
        {
            // Given: two identities present; first is default
            _identityGroupVM.AddOptionCommand.Execute(null);
            _identityGroupVM.AddOptionCommand.Execute(null);
            var secondIdentity = _character.AvailableIdentities[1];

            // When: GM picks second identity as default via ViewModel
            _identityGroupVM.DefaultOption = secondIdentity;

            // Then: domain reflects the change
            _character.DefaultIdentity.Should().Be(secondIdentity,
                "ViewModel.DefaultOption setter must write through to Character.DefaultIdentity");
        }

        // Scenario: SetDefaultOptionCommand sets DefaultOption to current SelectedOption
        [TestMethod]
        public void SetDefaultOptionCommand_SetsDefaultOptionToSelectedOption()
        {
            // Given: two identities; second is selected
            _identityGroupVM.AddOptionCommand.Execute(null);
            _identityGroupVM.AddOptionCommand.Execute(null);
            var secondIdentity = _character.AvailableIdentities[1];
            _identityGroupVM.SelectedOption = secondIdentity;

            // When
            _identityGroupVM.SetDefaultOptionCommand.Execute(null);

            // Then
            _identityGroupVM.DefaultOption.Should().Be(secondIdentity);
            _character.DefaultIdentity.Should().Be(secondIdentity,
                "SetDefaultOptionCommand must make the selected option the new default in the domain");
        }

        // ── Scenario: ActiveOption binding ────────────────────────────────────

        // Scenario: ActiveOption getter reflects owner.ActiveIdentity from the domain
        [TestMethod]
        public void ActiveOptionGetter_ReflectsOwnerActiveIdentityFromDomain()
        {
            // Given: a spawned character with an identity set as active directly in domain
            _identityGroupVM.AddOptionCommand.Execute(null);
            var identity = _character.AvailableIdentities[0];
            _character.ActiveIdentity = identity;

            // Then: ViewModel's ActiveOption mirrors domain state
            _identityGroupVM.ActiveOption.Should().Be(identity,
                "ActiveOption must read directly from Character.ActiveIdentity");
        }

        // Scenario: Setting ActiveOption on ViewModel updates owner.ActiveIdentity in domain
        [TestMethod]
        public void SettingActiveOption_UpdatesOwnerActiveIdentityInDomain()
        {
            // Given: two identities
            _identityGroupVM.AddOptionCommand.Execute(null);
            _identityGroupVM.AddOptionCommand.Execute(null);
            var secondIdentity = _character.AvailableIdentities[1];

            // When: ViewModel sets the active identity
            _identityGroupVM.ActiveOption = secondIdentity;

            // Then: domain reflects the active identity change
            _character.ActiveIdentity.Should().Be(secondIdentity,
                "ViewModel.ActiveOption setter must write through to Character.ActiveIdentity");
        }

        // ── Scenario: PropertyChanged notifications ───────────────────────────

        // Scenario: PropertyChanged fires for ActiveOption when owner.ActiveIdentity changes
        [TestMethod]
        public void PropertyChanged_FiresForActiveOption_WhenOwnerActiveIdentityChanges()
        {
            // Given: add an identity so we can set ActiveIdentity
            _identityGroupVM.AddOptionCommand.Execute(null);
            var identity = _character.AvailableIdentities[0];

            var firedProperties = new List<string>();
            _identityGroupVM.PropertyChanged += (s, e) => firedProperties.Add(e.PropertyName);

            // When: domain changes ActiveIdentity directly (e.g. game event)
            _character.ActiveIdentity = identity;

            // Then: ViewModel raised PropertyChanged for ActiveOption
            firedProperties.Should().Contain("ActiveOption",
                "ViewModel must propagate owner.PropertyChanged(\"ActiveIdentity\") → PropertyChanged(\"ActiveOption\")");
        }

        // Scenario: PropertyChanged fires for DefaultOption when owner.DefaultIdentity changes
        [TestMethod]
        public void PropertyChanged_FiresForDefaultOption_WhenOwnerDefaultIdentityChanges()
        {
            // Given: add two identities
            _identityGroupVM.AddOptionCommand.Execute(null);
            _identityGroupVM.AddOptionCommand.Execute(null);
            var secondIdentity = _character.AvailableIdentities[1];

            var firedProperties = new List<string>();
            _identityGroupVM.PropertyChanged += (s, e) => firedProperties.Add(e.PropertyName);

            // When: domain changes DefaultIdentity directly
            _character.DefaultIdentity = secondIdentity;

            // Then: ViewModel raises PropertyChanged for DefaultOption
            firedProperties.Should().Contain("DefaultOption",
                "ViewModel must propagate owner.PropertyChanged(\"DefaultIdentity\") → PropertyChanged(\"DefaultOption\")");
        }

        // Scenario: ViewModel owner is the character loaded into CharacterEditorViewModel
        [TestMethod]
        public void ViewModelOwner_IsTheCharacterLoadedIntoCharacterEditorViewModel()
        {
            // Then: Owner on the ViewModel is the same object as the loaded character
            _identityGroupVM.Owner.Should().BeSameAs(_character,
                "OptionGroupViewModel<Identity> must track the domain character as Owner");
        }
    }
}
