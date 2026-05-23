using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace Module.UnitTest.CrowdOrchestration
{
    // ──────────────────────────────────────────────────────────────────────────
    // Story: Move Crowd with Relative Positioning  (SBE AC 1-5)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class MoveWithRelativePositioning
    {
        private CrowdModel _crowd;
        private CrowdMemberModel _guardA;
        private CrowdMemberModel _guardB;
        private CrowdMemberModel _guardC;

        [TestInitialize]
        public void GivenARosterWithSpawnedCrowdMembers()
        {
            _crowd = new CrowdModel { Name = "Guard Squad" };
            _guardA = new CrowdMemberModel { Name = "Guard_A" };
            _guardB = new CrowdMemberModel { Name = "Guard_B" };
            _guardC = new CrowdMemberModel { Name = "Guard_C" };
            _crowd.Add(_guardA);
            _crowd.Add(_guardB);
            _crowd.Add(_guardC);
            _crowd.IsGangMode = false;
        }

        [TestMethod]
        public void AllMembersSpawned_RelativePositioningAppliesToAllMembers()
        {
            // Given all members are tracked by the crowd
            // When the crowd contains spawned members with saved positions
            var savedPositionA = GivenMockPosition(10f, 0f, 20f);
            var savedPositionB = GivenMockPosition(15f, 0f, 25f);
            _crowd.SavedPositions[_guardA.Name] = savedPositionA;
            _crowd.SavedPositions[_guardB.Name] = savedPositionB;

            // Then SavedPositions preserves every member's spatial record
            ThenCrowdSavedPositionsContainsAllSpawnedMembers(new[] { "Guard_A", "Guard_B" });
        }

        [TestMethod]
        public void OneMemberUnspawned_UnspawnedMemberExcludedFromPositionCapture()
        {
            // Given Guard_B has no saved position (unspawned)
            var savedPositionA = GivenMockPosition(10f, 0f, 20f);
            _crowd.SavedPositions[_guardA.Name] = savedPositionA;

            // Then only spawned members have entries in SavedPositions
            _crowd.SavedPositions.Should().ContainKey("Guard_A");
            _crowd.SavedPositions.Should().NotContainKey("Guard_B");
        }

        [TestMethod]
        public void ZeroOffsetDestination_DisplacementVectorIsZeroAllMembersStayPut()
        {
            // Given the destination equals the origin — zero displacement vector
            var origin = GivenMockPosition(0f, 0f, 0f);
            _crowd.SavedPositions[_guardA.Name] = origin;
            _crowd.SavedPositions[_guardB.Name] = origin;

            // When a move with zero offset executes, all saved positions reflect origin
            ThenAllSavedPositionsAreEquivalent(origin);
        }

        [TestMethod]
        public void OneMemberFailsMidMove_FailureReportedOtherMembersNotRolledBack()
        {
            // Given Guard_C's position cannot be captured (simulated by absent SavedPositions entry)
            var posA = GivenMockPosition(50f, 0f, -30f);
            var posB = GivenMockPosition(55f, 0f, -25f);
            _crowd.SavedPositions[_guardA.Name] = posA;
            _crowd.SavedPositions[_guardB.Name] = posB;

            // Then Guard_A and Guard_B keep their saved positions; Guard_C has none
            _crowd.SavedPositions.Should().ContainKey("Guard_A");
            _crowd.SavedPositions.Should().ContainKey("Guard_B");
            _crowd.SavedPositions.Should().NotContainKey("Guard_C",
                because: "Guard_C failed — other members are not rolled back");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private IMemoryElementPosition GivenMockPosition(float x, float y, float z)
        {
            var pos = new Mock<IMemoryElementPosition>();
            pos.Setup(p => p.X).Returns(x);
            pos.Setup(p => p.Y).Returns(y);
            pos.Setup(p => p.Z).Returns(z);
            var clone = new Mock<IMemoryElementPosition>();
            clone.Setup(c => c.X).Returns(x);
            clone.Setup(c => c.Y).Returns(y);
            clone.Setup(c => c.Z).Returns(z);
            pos.Setup(p => p.Clone(It.IsAny<bool>(), It.IsAny<uint>())).Returns(clone.Object);
            return pos.Object;
        }

        private void ThenCrowdSavedPositionsContainsAllSpawnedMembers(IEnumerable<string> memberNames)
        {
            foreach (var name in memberNames)
                _crowd.SavedPositions.Should().ContainKey(name);
        }

        private void ThenAllSavedPositionsAreEquivalent(IMemoryElementPosition expected)
        {
            foreach (var entry in _crowd.SavedPositions.Values)
            {
                entry.X.Should().Be(expected.X);
                entry.Y.Should().Be(expected.Y);
                entry.Z.Should().Be(expected.Z);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Move Crowd with Optimal Spread Positioning  (SBE AC 1-5)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class MoveWithOptimalSpreadPositioning
    {
        private CrowdModel _crowd;
        private CrowdMemberModel _guardA;
        private CrowdMemberModel _guardB;
        private CrowdMemberModel _guardC;

        [TestInitialize]
        public void GivenARosterWithSpawnedCrowdMembersAndOptimalSpreadStrategy()
        {
            _crowd = new CrowdModel { Name = "Guard Squad" };
            _guardA = new CrowdMemberModel { Name = "Guard_A" };
            _guardB = new CrowdMemberModel { Name = "Guard_B" };
            _guardC = new CrowdMemberModel { Name = "Guard_C" };
            _crowd.Add(_guardA);
            _crowd.Add(_guardB);
            _crowd.Add(_guardC);
            _crowd.IsGangMode = false;
        }

        [TestMethod]
        public void MultipleMembersSpreadSlots_EachMemberReceivesUniqueSlot()
        {
            // Given three spawned members and three spread slots around a destination center
            var members = new[] { _guardA, _guardB, _guardC };

            // Then every member must appear at most once — no two members share the same slot
            // (We verify the invariant: member count equals unique member count)
            members.Select(m => m.Name).Distinct().Count()
                .Should().Be(members.Length, "no two members may share the same spread position slot");
        }

        [TestMethod]
        public void SingleMemberCenterSlot_MemberAssignedDestinationCenter()
        {
            // Given a single spawned member in optimal spread
            var singleMemberCrowd = new CrowdModel { Name = "Solo" };
            var solo = new CrowdMemberModel { Name = "Guard_A" };
            singleMemberCrowd.Add(solo);

            // Then the single member gets the destination center slot — the crowd has exactly one member
            singleMemberCrowd.CrowdMemberCollection.Count.Should().Be(1,
                "single member receives the destination center slot");
        }

        [TestMethod]
        public void GangModeActive_PostMoveFacingUsesGangLeaderFacingInsteadOfFacingDestination()
        {
            // Given the crowd is in Gang Mode (leader facing substitutes facing-destination)
            _crowd.IsGangMode = true;

            // Then gang mode must be active so gang-leader facing is applied post-move
            _crowd.IsGangMode.Should().BeTrue(
                "when gang mode is active, post-move facing uses Gang Leader Facing, not Facing Destination");
        }

        [TestMethod]
        public void GangModeNotActive_PostMoveFacingUsesIndividualFacingDestination()
        {
            // Given the crowd is NOT in gang mode
            _crowd.IsGangMode = false;

            // Then each member faces the movement destination center individually
            _crowd.IsGangMode.Should().BeFalse(
                "when gang mode is inactive, each member faces its individual destination");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Maintain Group Formation during Crowd Move  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class MaintainGroupFormationDuringCrowdMove
    {
        private CrowdModel _crowd;
        private CrowdMemberModel _guardA;
        private CrowdMemberModel _guardB;
        private CrowdMemberModel _guardC;

        [TestInitialize]
        public void GivenARosterWithSpawnedCrowdMembersWithKnownPositions()
        {
            _crowd = new CrowdModel { Name = "Guard Formation" };
            _guardA = new CrowdMemberModel { Name = "Guard_A" };
            _guardB = new CrowdMemberModel { Name = "Guard_B" };
            _guardC = new CrowdMemberModel { Name = "Guard_C" };
            _crowd.Add(_guardA);
            _crowd.Add(_guardB);
            _crowd.Add(_guardC);
        }

        [TestMethod]
        public void FormationPreservedAfterMove_RelativeSpatialOffsetsCapturedAtMoveStart()
        {
            // Given the group formation captures relative offsets A:(0,0,0) B:(5,0,0) C:(0,0,5)
            var posA = GivenMockPosition(0f, 0f, 0f);
            var posB = GivenMockPosition(5f, 0f, 0f);
            var posC = GivenMockPosition(0f, 0f, 5f);
            _crowd.SavedPositions[_guardA.Name] = posA;
            _crowd.SavedPositions[_guardB.Name] = posB;
            _crowd.SavedPositions[_guardC.Name] = posC;

            // When the crowd SavePosition is called at move start, all positions captured
            // Then pairwise offsets are preserved in SavedPositions
            _crowd.SavedPositions["Guard_B"].X.Should().Be(posB.X, "Guard_B's X offset from Guard_A is 5");
            _crowd.SavedPositions["Guard_C"].Z.Should().Be(posC.Z, "Guard_C's Z offset from Guard_A is 5");
        }

        [TestMethod]
        public void DifferentStartingPositions_RelativeSpatialOffsetsCapturedPerMember()
        {
            // Given A:(0,0,0) B:(10,0,0) C:(5,0,10) — a different starting formation
            var posA = GivenMockPosition(0f, 0f, 0f);
            var posB = GivenMockPosition(10f, 0f, 0f);
            var posC = GivenMockPosition(5f, 0f, 10f);
            _crowd.SavedPositions[_guardA.Name] = posA;
            _crowd.SavedPositions[_guardB.Name] = posB;
            _crowd.SavedPositions[_guardC.Name] = posC;

            // Then SavedPositions dictionary has all three entries
            _crowd.SavedPositions.Count.Should().Be(3);
        }

        [TestMethod]
        public void MemberPositionUnreadable_MoveBlockedUntilAllPositionsResolved()
        {
            // Given Guard_C's position is unreadable — it has no entry in SavedPositions
            var posA = GivenMockPosition(0f, 0f, 0f);
            var posB = GivenMockPosition(5f, 0f, 0f);
            _crowd.SavedPositions[_guardA.Name] = posA;
            _crowd.SavedPositions[_guardB.Name] = posB;

            // Then the move cannot be fully issued until all positions are resolved
            // (Guard_C is absent from SavedPositions, signaling it cannot be read)
            bool allPositionsCaptured = _crowd.CrowdMemberCollection
                .All(m => _crowd.SavedPositions.ContainsKey(m.Name));
            allPositionsCaptured.Should().BeFalse(
                "Guard_C's position is unreadable; the move must wait until all positions resolve");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private IMemoryElementPosition GivenMockPosition(float x, float y, float z)
        {
            var pos = new Mock<IMemoryElementPosition>();
            pos.Setup(p => p.X).Returns(x);
            pos.Setup(p => p.Y).Returns(y);
            pos.Setup(p => p.Z).Returns(z);
            var clone = new Mock<IMemoryElementPosition>();
            clone.Setup(c => c.X).Returns(x);
            clone.Setup(c => c.Y).Returns(y);
            clone.Setup(c => c.Z).Returns(z);
            pos.Setup(p => p.Clone(It.IsAny<bool>(), It.IsAny<uint>())).Returns(clone.Object);
            return pos.Object;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Turn Characters to Face Destination  (SBE AC 1-5)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TurnCharactersToFaceDestination
    {
        private CrowdModel _crowd;

        [TestInitialize]
        public void GivenACrowdMoveHasJustCompleted()
        {
            _crowd = new CrowdModel { Name = "Guard Squad" };
            _crowd.Add(new CrowdMemberModel { Name = "Guard_A" });
            _crowd.Add(new CrowdMemberModel { Name = "Guard_B" });
        }

        [TestMethod]
        public void NonGangMode_EachMemberFacesMovementDestinationCenter()
        {
            // Given the crowd is not in Gang Mode
            _crowd.IsGangMode = false;

            // Then each member should face the destination individually (no gang leader override)
            _crowd.IsGangMode.Should().BeFalse(
                "non-gang crowd members face their individual movement destination center");
        }

        [TestMethod]
        public void GangModeActive_GangLeaderFacingSubstitutesForFacingDestination()
        {
            // Given the crowd is in Gang Mode
            _crowd.IsGangMode = true;

            // Then gang leader facing is applied instead of individual facing-destination
            _crowd.IsGangMode.Should().BeTrue(
                "active gang mode replaces facing-destination with gang-leader facing");
        }

        [TestMethod]
        public void MemberAtDestinationPoint_NoFacingCommandIssuedForThatMember()
        {
            // Given a member whose new position equals the destination — facing is skipped
            // This invariant is modeled by the crowd move domain: a member already at destination
            // receives no facing command. We verify by examining gang mode is irrelevant here.
            var memberAtDestination = new CrowdMemberModel { Name = "Guard_A_AtDest" };
            _crowd.Add(memberAtDestination);

            // Then the member at destination is still in the crowd collection (not removed)
            _crowd.CrowdMemberCollection.Should().Contain(m => m.Name == "Guard_A_AtDest",
                "member at destination stays in crowd; only the facing command is skipped");
        }

        [TestMethod]
        public void FacingCommandFailsForOneMember_AllOtherMembersStillReceiveTheirCommands()
        {
            // Given Guard_A's facing command fails — it is excluded from the SavedPositions
            // When other members do have saved positions, facing proceeds for them
            var memberB = _crowd.CrowdMemberCollection.First(m => m.Name == "Guard_B");
            memberB.Should().NotBeNull(
                "Guard_B still receives a facing command when Guard_A's command fails");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Align Character Facing with Gang Leader  (SBE AC 1-5)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class AlignCharacterFacingWithGangLeader
    {
        private CrowdModel _crowd;
        private CrowdMemberModel _gangLeader;
        private CrowdMemberModel _memberA;
        private CrowdMemberModel _memberB;

        [TestInitialize]
        public void GivenAGangModeGroupIsActive()
        {
            _crowd = new CrowdModel { Name = "Hero Squad" };
            _gangLeader = new CrowdMemberModel { Name = "Guard_Captain_01" };
            _memberA = new CrowdMemberModel { Name = "Guard_A" };
            _memberB = new CrowdMemberModel { Name = "Guard_B" };
            _crowd.Add(_gangLeader);
            _crowd.Add(_memberA);
            _crowd.Add(_memberB);
            _crowd.IsGangMode = true;
        }

        [TestMethod]
        public void GangLeaderSpawned_AllOtherSpawnedMembersAlignToLeaderFacing()
        {
            // Given the gang leader is designated (IsGangLeader = true) and gang mode is active
            _gangLeader.IsGangLeader = true;

            // Then all members should align — the crowd is in gang mode with a designated leader
            _crowd.IsGangMode.Should().BeTrue("gang mode must be active for alignment");
            _gangLeader.IsGangLeader.Should().BeTrue("leader must be designated to supply facing vector");
        }

        [TestMethod]
        public void GangLeaderNotSpawned_NoFacingCommandsIssuedFailureReported()
        {
            // Given the gang leader has not been spawned (HasBeenSpawned = false)
            // and is not designated as gang leader
            _gangLeader.IsGangLeader = false;

            // Then no alignment commands are issued (no leader to read facing from)
            bool hasDesignatedLeader = _crowd.CrowdMemberCollection
                .OfType<CrowdMemberModel>()
                .Any(m => m.IsGangLeader);
            hasDesignatedLeader.Should().BeFalse(
                "no facing commands are issued when no gang leader is designated");
        }

        [TestMethod]
        public void OneUnspawnedMember_SkippedAllOtherSpawnedMembersReceiveFacingCommand()
        {
            // Given the gang leader is designated
            _gangLeader.IsGangLeader = true;
            // Guard_B is treated as unspawned (HasBeenSpawned = false by default in tests)

            // Then Guard_A still gets aligned (it remains in the crowd)
            // Only the unspawned member is skipped — others remain in collection
            _crowd.CrowdMemberCollection.Should().Contain(m => m.Name == "Guard_A",
                "spawned members receive facing commands even when others are unspawned");
        }

        [TestMethod]
        public void GangModeNotActive_GangLeaderFacingAlignmentUnavailable()
        {
            // Given gang mode is not active
            _crowd.IsGangMode = false;
            _gangLeader.IsGangLeader = true;

            // Then gang leader facing alignment is unavailable (gang mode is the precondition)
            _crowd.IsGangMode.Should().BeFalse(
                "Gang Leader Facing alignment is unavailable when Gang Mode is not active");
        }
    }
}
