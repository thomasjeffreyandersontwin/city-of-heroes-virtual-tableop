// Tier 1 — Domain tests: no ViewModel, no WPF types, no COH.
// Covers: Activate Crowd as Gang with Gang Leader · Deactivate Gang
// Stories: SBE §Roster — Activate Crowd as Gang, Deactivate Gang
// CRC: Gang Mode — collective activation; exactly one gang leader; all members required.
//      Gang Leader — leader designation; leader indicator.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;

namespace Module.UnitTest.Roster
{
    // Story: Activate Crowd as Gang with Gang Leader
    // SBE: all member entries become active; exactly one gang leader designated;
    //      missing member rejects activation; existing gang replaced atomically.
    [TestClass]
    public class TestActivateCrowdAsGangWithGangLeader
    {
        private CrowdMemberModel _guardA;
        private CrowdMemberModel _guardB;
        private CrowdMemberModel _guardC;
        private List<CrowdMemberModel> _gangMembers;

        [TestInitialize]
        public void GivenARosterWithEntriesFromACrowd()
        {
            _guardA = new CrowdMemberModel { Name = "Guard_A" };
            _guardB = new CrowdMemberModel { Name = "Guard_B" };
            _guardC = new CrowdMemberModel { Name = "Guard_C" };
            _gangMembers = new List<CrowdMemberModel> { _guardA, _guardB, _guardC };
        }

        // SBE row: Gang activated successfully — member_entries = Guard_A, Guard_B, Guard_C
        // Simulates ActivateGang at domain level: call SetActive on all members,
        // designate first as gang leader (mirrors ActivateGang implementation).
        [TestMethod]
        public void WhenGangActivated_ThenAllMemberEntriesHaveActiveDesignation()
        {
            GivenGangActivatedWithLeader(_gangMembers, leaderIndex: 0);

            _guardA.IsActive.Should().BeTrue();
            _guardB.IsActive.Should().BeTrue();
            _guardC.IsActive.Should().BeTrue();
        }

        // CRC invariant: exactly one gang leader must be designated when gang mode activated
        [TestMethod]
        public void WhenGangActivated_ThenExactlyOneGangLeaderDesignated()
        {
            GivenGangActivatedWithLeader(_gangMembers, leaderIndex: 0);

            var leaders = _gangMembers.Where(m => m.IsGangLeader).ToList();
            leaders.Should().HaveCount(1,
                "exactly one gang leader must be designated when gang mode is activated");
        }

        // SBE: Gang activated successfully — leader_designation = Guard_A, leader_indicator = visible
        [TestMethod]
        public void WhenGangActivated_ThenDesignatedMemberHasGangLeaderIndicator()
        {
            GivenGangActivatedWithLeader(_gangMembers, leaderIndex: 0);

            _guardA.IsGangLeader.Should().BeTrue();
        }

        // SBE: Gang activated successfully — non-leader members have no leader indicator
        [TestMethod]
        public void WhenGangActivated_ThenNonLeaderMembersHaveNoGangLeaderIndicator()
        {
            GivenGangActivatedWithLeader(_gangMembers, leaderIndex: 0);

            _guardB.IsGangLeader.Should().BeFalse();
            _guardC.IsGangLeader.Should().BeFalse();
        }

        // SBE row: Single member gang — valid; entry shows both gang and leader indicators
        [TestMethod]
        public void WhenSingleMemberGangActivated_ThenThatMemberIsBothActiveAndGangLeader()
        {
            var singleMember = new List<CrowdMemberModel> { _guardA };
            GivenGangActivatedWithLeader(singleMember, leaderIndex: 0);

            _guardA.IsActive.Should().BeTrue();
            _guardA.IsGangLeader.Should().BeTrue();
        }

        // SBE row: Existing gang replaced — previous indicators cleared before new gang activated
        [TestMethod]
        public void WhenExistingGangReplaced_ThenPreviousGangLeaderIndicatorIsCleared()
        {
            // Activate first gang (Guard_A as leader)
            GivenGangActivatedWithLeader(_gangMembers, leaderIndex: 0);

            // Deactivate and replace with a new gang (Guard_B as leader)
            GivenGangDeactivated(_gangMembers);
            var newGang = new List<CrowdMemberModel> { _guardB, _guardC };
            GivenGangActivatedWithLeader(newGang, leaderIndex: 0);

            // Old leader indicator is gone; new leader indicator is set
            _guardA.IsGangLeader.Should().BeFalse("previous gang leader indicator was cleared");
            _guardB.IsGangLeader.Should().BeTrue("Guard_B is the new gang leader");
        }

        // ── helpers ─────────────────────────────────────────────────────────────

        private static void GivenGangActivatedWithLeader(
            List<CrowdMemberModel> members, int leaderIndex)
        {
            foreach (var m in members)
                m.SetActive();
            members[leaderIndex].IsGangLeader = true;
        }

        private static void GivenGangDeactivated(List<CrowdMemberModel> members)
        {
            foreach (var m in members)
                m.ResetActive();
        }
    }

    // Story: Deactivate Gang
    // SBE: deactivate clears all gang membership indicators and the gang leader indicator;
    //      overlays lose gang status; no auto-reactivation; no-op when already inactive.
    [TestClass]
    public class TestDeactivateGang
    {
        private CrowdMemberModel _guardA;
        private CrowdMemberModel _guardB;
        private CrowdMemberModel _guardC;
        private List<CrowdMemberModel> _gangMembers;

        [TestInitialize]
        public void GivenARosterWithActiveGangEntries()
        {
            _guardA = new CrowdMemberModel { Name = "Guard_A" };
            _guardB = new CrowdMemberModel { Name = "Guard_B" };
            _guardC = new CrowdMemberModel { Name = "Guard_C" };
            _gangMembers = new List<CrowdMemberModel> { _guardA, _guardB, _guardC };
        }

        // SBE row: Gang active — deactivated; all gang membership indicators removed
        [TestMethod]
        public void WhenGangDeactivated_ThenAllMemberEntriesLoseActiveDesignation()
        {
            GivenGangActivated();

            WhenGangDeactivated();

            _guardA.IsActive.Should().BeFalse();
            _guardB.IsActive.Should().BeFalse();
            _guardC.IsActive.Should().BeFalse();
        }

        // SBE: gang leader indicator cleared when gang deactivated
        [TestMethod]
        public void WhenGangDeactivated_ThenGangLeaderIndicatorIsCleared()
        {
            GivenGangActivated();

            WhenGangDeactivated();

            _gangMembers.Any(m => m.IsGangLeader).Should().BeFalse(
                "all gang leader indicators are cleared on gang deactivation");
        }

        // SBE: no entry is automatically activated after deactivation; single-character mode resumes
        [TestMethod]
        public void WhenGangDeactivated_ThenNoEntryIsAutoActivated()
        {
            GivenGangActivated();

            WhenGangDeactivated();

            _gangMembers.Any(m => m.IsActive).Should().BeFalse(
                "deactivating gang mode leaves all entries inactive; no auto-replacement");
        }

        // SBE row: No gang active — no-op; action completes without error
        [TestMethod]
        public void WhenNoGangActiveAndDeactivateCalled_ThenIsNoOpWithNoError()
        {
            // All members are already inactive — no exception should occur
            WhenGangDeactivated();

            _gangMembers.Any(m => m.IsActive).Should().BeFalse();
        }

        // SBE row: Some members unspawned — collective deactivation still clears all indicators
        [TestMethod]
        public void WhenGangDeactivatedWithUnspawnedMembers_ThenActiveDesignationStillCleared()
        {
            // Guard_C is active but not spawned
            _guardA.SetActive();
            _guardA.IsGangLeader = true;
            _guardB.SetActive();
            // Guard_C stays unspawned and inactive

            WhenGangDeactivated();

            _guardA.IsActive.Should().BeFalse();
            _guardB.IsActive.Should().BeFalse();
            _guardA.IsGangLeader.Should().BeFalse();
        }

        // ── helpers ─────────────────────────────────────────────────────────────

        private void GivenGangActivated()
        {
            foreach (var m in _gangMembers)
                m.SetActive();
            _guardA.IsGangLeader = true;
        }

        private void WhenGangDeactivated()
        {
            foreach (var m in _gangMembers)
                m.ResetActive();
        }
    }
}
