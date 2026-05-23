// Tier 1 — Domain tests: no ViewModel, no WPF types, no COH.
// Covers: Activate Character · Deactivate Character
// Stories: SBE §Roster — Activate Character, Deactivate Character
// CRC: Active Character — active designation; at most one active unless gang mode

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;

namespace Module.UnitTest.Roster
{
    // Story: Activate Character (mark as active turn)
    // SBE: activate sets active designation; replacing previous active deactivates it
    //      (single-active-at-a-time invariant is enforced by the Roster/ViewModel layer,
    //      not by Character.SetActive() itself — documented here at the domain tier).
    [TestClass]
    public class TestActivateCharacter
    {
        private CrowdMemberModel _guardCaptain;
        private CrowdMemberModel _villainBoss;

        [TestInitialize]
        public void GivenARosterWithTwoEntries()
        {
            _guardCaptain = new CrowdMemberModel { Name = "Guard_Captain_01" };
            _villainBoss = new CrowdMemberModel { Name = "Villain_Boss_03" };
        }

        // SBE: new roster entry starts with active indicator hidden
        [TestMethod]
        public void WhenCharacterFirstAddedToRoster_ThenIsActiveFalse()
        {
            _guardCaptain.IsActive.Should().BeFalse();
        }

        // SBE row: Activate new entry — active_designation = Guard_Captain_01
        [TestMethod]
        public void WhenActivateTriggered_ThenActiveDesignationIsSet()
        {
            _guardCaptain.SetActive();

            _guardCaptain.IsActive.Should().BeTrue();
        }

        // SBE row: Already active — no-op; active designation remains on same entry
        [TestMethod]
        public void WhenAlreadyActiveAndActivateTriggeredAgain_ThenActiveDesignationRemainsSet()
        {
            _guardCaptain.SetActive();
            _guardCaptain.SetActive(); // idempotent at domain level

            _guardCaptain.IsActive.Should().BeTrue();
        }

        // SBE: when a different entry was active it loses its indicator atomically.
        // At domain tier: SetActive() does NOT auto-deactivate others — the Roster
        // (ViewModel) enforces single-active-at-a-time by calling ResetActive() on the
        // previous holder before calling SetActive() on the new one.
        [TestMethod]
        public void WhenSecondCharacterActivated_ThenDomainDoesNotAutoDeactivateFirst()
        {
            _guardCaptain.SetActive();
            _villainBoss.SetActive();

            // Both show active at the pure domain tier — the VM layer clears the old one.
            _guardCaptain.IsActive.Should().BeTrue(
                "the domain does not enforce single-active; the Roster ViewModel does");
            _villainBoss.IsActive.Should().BeTrue();
        }

        // SBE row: Activate unspawned entry — active_designation applied; no overlay indicator
        [TestMethod]
        public void WhenUnspawnedCharacterActivated_ThenActiveDesignationIsApplied()
        {
            // Guard_Captain_01 is not spawned (HasBeenSpawned = false)
            _guardCaptain.SetActive();

            _guardCaptain.IsActive.Should().BeTrue(
                "active designation applies even without a spawned NPC in the game world");
            _guardCaptain.HasBeenSpawned.Should().BeFalse();
        }

        // CRC invariant: IsGangLeader must be false for non-gang-leader entries
        [TestMethod]
        public void WhenCharacterActivatedWithoutGang_ThenGangLeaderIndicatorRemainsHidden()
        {
            _guardCaptain.SetActive();

            _guardCaptain.IsGangLeader.Should().BeFalse(
                "SetActive alone does not make a character the gang leader");
        }
    }

    // Story: Deactivate Character
    // SBE: deactivate removes active designation; not active is a no-op;
    //      gang member deactivated individually does not end gang mode.
    [TestClass]
    public class TestDeactivateCharacter
    {
        private CrowdMemberModel _guardCaptain;
        private CrowdMemberModel _villainBoss;

        [TestInitialize]
        public void GivenARosterWithActiveAndInactiveEntries()
        {
            _guardCaptain = new CrowdMemberModel { Name = "Guard_Captain_01" };
            _villainBoss = new CrowdMemberModel { Name = "Villain_Boss_03" };
        }

        // SBE row: Active entry deactivated — active_designation = none
        [TestMethod]
        public void WhenActiveCharacterDeactivated_ThenActiveDesignationIsRemoved()
        {
            _guardCaptain.SetActive();
            _guardCaptain.ResetActive();

            _guardCaptain.IsActive.Should().BeFalse();
        }

        // SBE row: Not active — no-op; no error raised
        [TestMethod]
        public void WhenAlreadyInactiveAndDeactivateCalled_ThenIsNoOpWithNoError()
        {
            // Guard has never been activated
            _guardCaptain.ResetActive(); // must not throw

            _guardCaptain.IsActive.Should().BeFalse();
        }

        // SBE: when deactivated, no other entry is automatically activated
        [TestMethod]
        public void WhenCharacterDeactivated_ThenNoOtherEntryIsAutoActivated()
        {
            _guardCaptain.SetActive();
            _guardCaptain.ResetActive();

            _villainBoss.IsActive.Should().BeFalse(
                "deactivating one character must not auto-activate another");
        }

        // SBE row: Gang member deactivated individually — that entry's active_designation cleared;
        // CRC: ResetActive() clears IsGangLeader at the same time as IsActive.
        [TestMethod]
        public void WhenGangLeaderDeactivatedIndividually_ThenGangLeaderIndicatorAlsoClears()
        {
            _guardCaptain.SetActive();
            _guardCaptain.IsGangLeader = true;

            _guardCaptain.ResetActive();

            _guardCaptain.IsActive.Should().BeFalse();
            _guardCaptain.IsGangLeader.Should().BeFalse(
                "ResetActive clears both the active designation and the gang leader indicator");
        }
    }
}
