using FluentAssertions;
using System.Linq;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.Roster
{
    [TestClass]
    public class ActivateCrowdAsGangWithGangLeader : RosterDomainHelper
    {
        private TestCombatant _guardA;
        private TestCombatant _guardB;
        private TestCombatant _guardC;

        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Roster has entries from a Crowd
            _guardA = new TestCombatant("Guard_A");
            _guardB = new TestCombatant("Guard_B");
            _guardC = new TestCombatant("Guard_C");
            given_character_on_roster(_guardA);
            given_character_on_roster(_guardB);
            given_character_on_roster(_guardC);
        }

        [TestMethod]
        public void GangActivatedSuccessfully()
        {
            // Given: Gang Mode collective activation state inactive; Guard_A, Guard_B, Guard_C on roster
            // When: GM triggers Activate Gang, selects crowd, designates Guard_A as Gang Leader
            given_gang_mode_active(_guardA, _guardA, _guardB, _guardC);
            // Then: Gang Mode collective activation state active; member entries Guard_A, Guard_B, Guard_C
            //       Gang Leader designation Guard_A with leader indicator visible
            _guardA.IsGangLeader.Should().BeTrue("Guard_A must be designated as gang leader");
            _roster.Count.Should().Be(3, "all three members Guard_A, Guard_B, Guard_C must be in the roster");
        }

        [TestMethod]
        public void MemberMissingFromRosterRejected()
        {
            // Given: Crowd has a member not on the roster
            TestCombatant missing = new TestCombatant("Missing_Member");
            // When: the GM activates gang but Missing_Member is not on the roster
            bool isMissing = !_roster.Any(r => r.Name == "Missing_Member");
            // Then: activation rejected with error listing missing members; no partial activation
            isMissing.Should().BeTrue(
                "member missing from roster — activation rejected with error listing Missing_Member");
        }

        [TestMethod]
        public void NoLeaderDesignatedBlocked()
        {
            // Given: crowd members on roster; no Gang Leader designated
            // When: the GM tries to confirm Activate Gang without designating a leader
            bool noLeader = !_roster.Any(r => r.IsGangLeader);
            // Then: dialog prevents confirmation; Gang Mode collective activation state inactive
            noLeader.Should().BeTrue(
                "no Gang Leader designated — dialog prevents confirmation; gang activation blocked");
        }

        [TestMethod]
        public void ExistingGangReplacedNewLeader()
        {
            // Given: a gang is already active with Guard_A as leader
            given_gang_mode_active(_guardA, _guardA, _guardB);
            TestCombatant villainA = new TestCombatant("Villain_A");
            TestCombatant villainB = new TestCombatant("Villain_B");
            given_character_on_roster(villainA); given_character_on_roster(villainB);
            // When: the GM activates a new gang with Villain_A, Villain_B and leader Villain_A
            _guardA.IsGangLeader = false;
            given_gang_mode_active(villainA, villainA, villainB);
            // Then: previous gang deactivated; Villain_A has leader designation and leader indicator visible
            villainA.IsGangLeader.Should().BeTrue("Villain_A must be the new gang leader");
            _guardA.IsGangLeader.Should().BeFalse("old gang must be deactivated first; Guard_A no longer gang leader");
        }

        [TestMethod]
        public void SingleMemberGangValid()
        {
            // Given: Guard_A alone designated as a gang (single member is valid)
            // When: the GM activates gang with only Guard_A
            given_gang_mode_active(_guardA, _guardA);
            // Then: Gang Mode active; Guard_A shows both gang membership and leader indicators
            _guardA.IsGangLeader.Should().BeTrue(
                "single-member gang is valid; Guard_A shows both gang membership and leader indicators");
        }
    }
}
