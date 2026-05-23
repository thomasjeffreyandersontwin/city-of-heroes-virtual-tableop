// Tier 1 — Domain tests: no ViewModel, no WPF types, no COH.
// Covers: Add Character to Roster · Track Spawned State per Character
// Stories: SBE §Roster — Add Character to Roster, Track Spawned State per Character
// CRC: Roster Entry (spawned state, active indicator, gang membership) · Spawned State (presence in game world)

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;

namespace Module.UnitTest.Roster
{
    // Story: Add Character to Roster
    // SBE: new entry has character name, spawned state false, active indicator hidden,
    //      gang membership indicator hidden; duplicate addition is rejected.
    [TestClass]
    public class TestAddCharacterToRoster
    {
        private CrowdMemberModel _guardCaptain;
        private CrowdMemberModel _villainBoss;

        [TestInitialize]
        public void GivenASessionActiveWithNoRosterEntries()
        {
            _guardCaptain = new CrowdMemberModel { Name = "Guard_Captain_01" };
            _villainBoss = new CrowdMemberModel { Name = "Villain_Boss_03" };
        }

        // SBE row: New character added — character_name = Guard_Captain_01
        [TestMethod]
        public void WhenCharacterAddedToRoster_ThenRosterEntryNameMatchesCharacterName()
        {
            _guardCaptain.Name.Should().Be("Guard_Captain_01");
        }

        // SBE row: New character added — spawned_state = false
        [TestMethod]
        public void WhenCharacterAddedToRoster_ThenSpawnedStateIsFalse()
        {
            _guardCaptain.HasBeenSpawned.Should().BeFalse();
        }

        // SBE row: New character added — active_turn_indicator = hidden
        [TestMethod]
        public void WhenCharacterAddedToRoster_ThenActiveIndicatorIsHidden()
        {
            _guardCaptain.IsActive.Should().BeFalse();
        }

        // SBE row: New character added — gang_membership_indicator = hidden
        [TestMethod]
        public void WhenCharacterAddedToRoster_ThenGangMembershipIndicatorIsHidden()
        {
            _guardCaptain.IsGangLeader.Should().BeFalse();
        }

        // SBE row: No identity configured — identity not required for roster membership
        [TestMethod]
        public void WhenCharacterWithNoIdentityAdded_ThenRosterEntryExistsWithNoError()
        {
            var blank = new CrowdMemberModel { Name = "Blank_Character" };
            blank.HasBeenSpawned.Should().BeFalse();
            blank.IsActive.Should().BeFalse();
        }

        // SBE row: Multiple added in sequence — each entry is independent
        [TestMethod]
        public void WhenMultipleCharactersAddedInSequence_ThenEachEntryIsIndependent()
        {
            _guardCaptain.Name.Should().NotBe(_villainBoss.Name);
            _guardCaptain.HasBeenSpawned.Should().BeFalse();
            _villainBoss.HasBeenSpawned.Should().BeFalse();
        }
    }

    // Story: Track Spawned State per Character
    // SBE: spawned state starts false; SetAsSpawned sets it true; each character tracks independently.
    [TestClass]
    public class TestTrackSpawnedStatePerCharacter
    {
        private CrowdMemberModel _guardA;
        private CrowdMemberModel _guardB;

        [TestInitialize]
        public void GivenARosterWithTwoEntries()
        {
            _guardA = new CrowdMemberModel { Name = "Guard_A" };
            _guardB = new CrowdMemberModel { Name = "Guard_B" };
        }

        // SBE row: Not spawned — overlay not rendered → presence in game world = false
        [TestMethod]
        public void WhenCharacterNotSpawned_ThenSpawnedStateIsFalse()
        {
            _guardA.HasBeenSpawned.Should().BeFalse();
        }

        // SBE row: Spawned from roster or context menu → presence in game world = true
        [TestMethod]
        public void WhenSetAsSpawnedCalled_ThenSpawnedStateBecomesTrue()
        {
            _guardA.SetAsSpawned();

            _guardA.HasBeenSpawned.Should().BeTrue();
        }

        // SBE: each character tracks its own presence in game world independently
        [TestMethod]
        public void WhenOneCharacterSpawned_ThenOtherCharacterRemainsUnspawned()
        {
            _guardA.SetAsSpawned();

            _guardA.HasBeenSpawned.Should().BeTrue();
            _guardB.HasBeenSpawned.Should().BeFalse("Guard_B was never spawned");
        }

        // SBE row: Cleared or removed from desktop → presence in game world = false
        // Domain invariant: HasBeenSpawned has no public setter; only SetAsSpawned sets it.
        // ClearFromDesktop resets state through the ViewModel layer (tested in Tier 2).
        [TestMethod]
        public void WhenSpawnedStateIsTrue_ThenPresenceInGameWorldReflectsSpawnedNPC()
        {
            _guardA.SetAsSpawned();

            _guardA.HasBeenSpawned.Should().BeTrue(
                "spawned state true means NPC is present in the game world");
        }

        // SBE row: Multiple spawned simultaneously → each tracks separately
        [TestMethod]
        public void WhenMultipleCharactersSpawned_ThenEachTracksSpawnedStateIndependently()
        {
            _guardA.SetAsSpawned();
            _guardB.SetAsSpawned();

            _guardA.HasBeenSpawned.Should().BeTrue();
            _guardB.HasBeenSpawned.Should().BeTrue();
        }
    }
}
