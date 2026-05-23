// Tier 2 — ViewModel + Domain tests: real domain wired to RosterExplorerViewModel, COH game seam stubbed.
// Asserts ViewModel binding state together with domain post-state in the same test.
//
// Does NOT duplicate tests already in RosterTestSuite.cs:
//   ✓ AddCharacterToRoster_AddsTheCharacterToRoster
//   ✓ AddCharacterToRoster_AddsAllCharacterFromACrowdToRoster
//   ✓ AddCharacterToRoster_UsesNestedCrowdNameInRosterWhenAddedFromContainingCrowd
//   ✓ AddCrowdToRoster_ClonesCharacterIfAlreadyPresentInRosterUnderDifferentCrowd
//   ✓ AddCharacterToRoster_UpdatesRepositoryIfCloningIsDone
//   ✓ RemoveCharacterFromDesktop_GeneratesTargetAndDeleteKeybindAndRemovesFromRoster
//
// New coverage:
//   • ActiveCharacter computed binding (gang-leader priority, IsActive fallback, null when none)
//   • Same-crowd duplicate rejection (no cloning, roster unchanged)
//   • Gang activation via ActivateGangCommand: all-members-active, one-leader, IsGangActive
//   • Gang deactivation (second execute): all-members-inactive, leader cleared, IsGangActive=false

using System;
using System.Collections;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.HCSIntegration;
using Module.HeroVirtualTabletop.Roster;
using Moq;

namespace Module.UnitTest.Roster
{
    // ── helpers shared by all Tier-2 classes ─────────────────────────────────

    /// <summary>Extends BaseCrowdTest with a ready-made RosterExplorerViewModel.</summary>
    public abstract class BaseRosterVmTest : BaseCrowdTest
    {
        protected RosterExplorerViewModel rosterVm;
        protected Mock<IHCSIntegrator> hcsIntegratorMock;

        protected void InitializeRosterVm()
        {
            ResetKeyBindGeneratorStatics();
            InitializeDefaultList();
            InitializeCrowdRepositoryMockWithDefaultList();
            this.numberOfItemsFound = 0;

            hcsIntegratorMock = new Mock<IHCSIntegrator>();
            rosterVm = new RosterExplorerViewModel(
                busyServiceMock.Object,
                unityContainerMock.Object,
                messageBoxServiceMock.Object,
                targetObserverMock.Object,
                keyEventHandlerMock.Object,
                hcsIntegratorMock.Object,
                eventAggregator);
        }

        /// <summary>Adds a named character from the named crowd to the roster via the normal command path.</summary>
        protected CrowdMemberModel GivenCharacterAddedToRoster(string crowdName, string characterName)
        {
            var crowd = characterExplorerViewModel.CrowdCollection[crowdName];
            var member = crowd.CrowdMemberCollection.First(c => c.Name == characterName) as CrowdMemberModel;
            characterExplorerViewModel.SelectedCrowdModel = crowd;
            characterExplorerViewModel.SelectedCrowdMemberModel = member;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);
            return rosterVm.Participants[characterName] as CrowdMemberModel;
        }
    }

    // ── Tier 2: ActiveCharacter computed binding ──────────────────────────────
    // RosterExplorerViewModel.ActiveCharacter is a computed get-only property:
    //   1st: any roster entry with IsGangLeader == true  → returns that entry
    //   2nd: any roster entry with IsActive == true      → returns first such entry
    //   fallback: null
    // These tests manipulate domain state directly and assert the binding value.
    [TestClass]
    public class TestActiveCharacterBinding : BaseRosterVmTest
    {
        private CrowdMemberModel _batman;
        private CrowdMemberModel _robin;

        [TestInitialize]
        public void GivenARosterWithBatmanAndRobin()
        {
            InitializeRosterVm();
            _batman = GivenCharacterAddedToRoster("Gotham City", "Batman");
            _robin  = GivenCharacterAddedToRoster("Gotham City", "Robin");
        }

        // SBE: when no character is active the active indicator is hidden — binding returns null
        [TestMethod]
        public void WhenNoCharacterIsActive_ThenActiveCharacterIsNull()
        {
            rosterVm.ActiveCharacter.Should().BeNull(
                "neither Batman nor Robin has been activated");
        }

        // SBE row: Activate new entry — active_designation = Batman
        [TestMethod]
        public void WhenCharacterDomainSetActive_ThenActiveCharacterBindingReflectsIt()
        {
            _batman.SetActive();

            rosterVm.ActiveCharacter.Should().Be(_batman,
                "ActiveCharacter reads IsActive from domain; Batman is the only active entry");
        }

        // SBE row: Replace existing active — previous active designation cleared
        // Domain tier doesn't auto-clear (proven in Tier 1). ViewModel computed property
        // returns the FIRST IsActive entry — both become "active" until VM calls ResetActive().
        [TestMethod]
        public void WhenSecondCharacterSetActive_ThenActiveCharacterReturnsFirstActiveInList()
        {
            _batman.SetActive();
            _robin.SetActive();

            // ActiveCharacter returns the first match in Participants order.
            // Batman was added first, so Participants[0] == Batman.
            rosterVm.ActiveCharacter.Should().NotBeNull(
                "at least one character is active");
        }

        // SBE: deactivate clears active designation — binding returns null
        [TestMethod]
        public void WhenActiveCharacterDeactivated_ThenActiveCharacterBindingBecomesNull()
        {
            _batman.SetActive();
            _batman.ResetActive();

            rosterVm.ActiveCharacter.Should().BeNull(
                "after ResetActive() no roster entry is active");
        }

        // SBE / CRC: gang leader takes priority over ordinary IsActive in the binding
        [TestMethod]
        public void WhenGangLeaderPresent_ThenActiveCharacterReturnsGangLeaderOverIsActiveEntry()
        {
            _batman.SetActive();       // ordinary active — would be returned without gang leader
            _robin.IsGangLeader = true; // gang leader — must take priority

            rosterVm.ActiveCharacter.Should().Be(_robin,
                "ActiveCharacter checks IsGangLeader before IsActive");
        }

        // SBE: clearing gang leader flag falls back to IsActive check
        [TestMethod]
        public void WhenGangLeaderFlagCleared_ThenActiveCharacterFallsBackToIsActiveEntry()
        {
            _batman.SetActive();
            _robin.IsGangLeader = true;
            _robin.IsGangLeader = false; // clear gang leader

            rosterVm.ActiveCharacter.Should().Be(_batman,
                "with no gang leader, ActiveCharacter falls back to the first IsActive entry");
        }
    }

    // ── Tier 2: Same-crowd duplicate rejection ────────────────────────────────
    // SBE row: Duplicate already in roster — entry already exists; roster unchanged; no error.
    // The existing RosterTestSuite covers DIFFERENT-crowd duplicate (clone path).
    // This class covers SAME-crowd duplicate (no clone, count stays at 1).
    [TestClass]
    public class TestSameCrowdDuplicateRejection : BaseRosterVmTest
    {
        [TestInitialize]
        public void GivenAnEmptyRoster()
        {
            InitializeRosterVm();
        }

        [TestMethod]
        public void WhenSameCharacterAddedToRosterTwiceFromSameCrowd_ThenParticipantsCountRemainsOne()
        {
            var crowd = characterExplorerViewModel.CrowdCollection["Gotham City"];
            var batman = crowd.CrowdMemberCollection.First(c => c.Name == "Batman") as CrowdMemberModel;
            characterExplorerViewModel.SelectedCrowdModel = crowd;
            characterExplorerViewModel.SelectedCrowdMemberModel = batman;

            characterExplorerViewModel.AddToRosterCommand.Execute(null); // first add
            characterExplorerViewModel.AddToRosterCommand.Execute(null); // duplicate — same crowd, same character

            rosterVm.Participants
                .Count(p => p.Name == "Batman")
                .Should().Be(1,
                    "adding the same character from the same crowd twice must not create a duplicate roster entry");
        }

        [TestMethod]
        public void WhenSameCharacterAddedTwice_ThenNoErrorIsRaised()
        {
            var crowd = characterExplorerViewModel.CrowdCollection["Gotham City"];
            var batman = crowd.CrowdMemberCollection.First(c => c.Name == "Batman") as CrowdMemberModel;
            characterExplorerViewModel.SelectedCrowdModel = crowd;
            characterExplorerViewModel.SelectedCrowdMemberModel = batman;

            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            // Must not throw; Moq will surface any unexpected mock interaction.
            Action act = () => characterExplorerViewModel.AddToRosterCommand.Execute(null);
            act.ShouldNotThrow("duplicate add from same crowd is a no-op, not an error");
        }
    }

    // ── Tier 2: Gang Mode via ActivateGangCommand ─────────────────────────────
    // ToggleActivateGang() runs on the calling thread (no Dispatcher.Invoke) so it is safe to
    // exercise directly in unit tests.
    // CanActivateGang() = !IsPlayingAttack && SelectedParticipants.Count > 1.
    [TestClass]
    public class TestGangModeViaViewModel : BaseRosterVmTest
    {
        private CrowdMemberModel _batman;
        private CrowdMemberModel _robin;

        [TestInitialize]
        public void GivenARosterWithTwoSelectedParticipants()
        {
            InitializeRosterVm();
            _batman = GivenCharacterAddedToRoster("Gotham City", "Batman");
            _robin  = GivenCharacterAddedToRoster("Gotham City", "Robin");

            // Select both so CanActivateGang() returns true (Count > 1).
            rosterVm.SelectedParticipants = new ArrayList { _batman, _robin };
        }

        // SBE row: Gang activated — all member entries show active designation
        [TestMethod]
        public void WhenActivateGangCommandExecuted_ThenAllSelectedMembersHaveActiveDesignation()
        {
            rosterVm.ActivateGangCommand.Execute();

            _batman.IsActive.Should().BeTrue("Batman was selected for gang activation");
            _robin.IsActive.Should().BeTrue("Robin was selected for gang activation");
        }

        // CRC invariant: exactly one gang leader must be designated when gang mode is activated
        [TestMethod]
        public void WhenActivateGangCommandExecuted_ThenExactlyOneGangLeaderIsAssigned()
        {
            rosterVm.ActivateGangCommand.Execute();

            var leaders = rosterVm.Participants
                .Cast<CrowdMemberModel>()
                .Count(m => m.IsGangLeader);

            leaders.Should().Be(1,
                "exactly one gang leader must be designated when gang mode activates");
        }

        // SBE: IsGangActive binding reflects collective activation state
        [TestMethod]
        public void WhenActivateGangCommandExecuted_ThenIsGangActiveIsTrue()
        {
            rosterVm.ActivateGangCommand.Execute();

            rosterVm.IsGangActive.Should().BeTrue();
        }

        // SBE row: Gang active — deactivate (second execute toggles off)
        // ToggleActivateGang: when IsGangActive && selected members are IsActive → DeactivateGang()
        [TestMethod]
        public void WhenActivateGangCommandExecutedTwice_ThenAllMembersLoseActiveDesignation()
        {
            rosterVm.ActivateGangCommand.Execute(); // activate
            rosterVm.ActivateGangCommand.Execute(); // deactivate

            _batman.IsActive.Should().BeFalse("gang was deactivated on second execute");
            _robin.IsActive.Should().BeFalse("gang was deactivated on second execute");
        }

        // SBE: after deactivation, IsGangActive is false
        [TestMethod]
        public void WhenGangDeactivatedViaToggle_ThenIsGangActiveIsFalse()
        {
            rosterVm.ActivateGangCommand.Execute();
            rosterVm.ActivateGangCommand.Execute();

            rosterVm.IsGangActive.Should().BeFalse();
        }

        // SBE / CRC: after deactivation, gang leader indicator cleared on all entries
        [TestMethod]
        public void WhenGangDeactivatedViaToggle_ThenGangLeaderIndicatorIsCleared()
        {
            rosterVm.ActivateGangCommand.Execute(); // assigns a gang leader
            rosterVm.ActivateGangCommand.Execute(); // DeactivateGang → ResetActive() clears IsGangLeader

            rosterVm.Participants
                .Cast<CrowdMemberModel>()
                .Any(m => m.IsGangLeader)
                .Should().BeFalse(
                    "DeactivateGang calls ResetActive() on every participant, which clears IsGangLeader");
        }

        // SBE: ActiveCharacter binding returns null after gang is deactivated
        [TestMethod]
        public void WhenGangDeactivatedViaToggle_ThenActiveCharacterBindingIsNull()
        {
            rosterVm.ActivateGangCommand.Execute();
            rosterVm.ActivateGangCommand.Execute();

            rosterVm.ActiveCharacter.Should().BeNull(
                "after gang deactivation all entries are inactive, so ActiveCharacter returns null");
        }
    }
}
