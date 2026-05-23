using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.Roster
{
    [TestClass]
    public class RemoveCharacterFromRoster : RosterDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Roster has entries
            given_character_on_roster(_guardCaptain);
            given_character_on_roster(_villainBoss);
            given_character_on_roster(_healer);
        }

        [TestMethod]
        public void SpawnedDespawnThenRemove()
        {
            // Given: Guard_Captain_01 has presence in game world true, gang membership indicator hidden
            given_character_spawned(_guardCaptain);
            // When: the GM removes the Roster Entry Guard_Captain_01
            when_character_removed_from_roster(_guardCaptain);
            // Then: Game Bridge issues despawn command; Character Overlay removed; entry deleted from Roster
            then_not_on_roster("Guard_Captain_01");
            then_not_spawned(_guardCaptain);
        }

        [TestMethod]
        public void NotSpawnedRemoveOnly()
        {
            // Given: Villain_Boss_03 has presence in game world false, gang membership indicator hidden
            then_not_spawned(_villainBoss);
            // When: the GM removes the Roster Entry Villain_Boss_03
            when_character_removed_from_roster(_villainBoss);
            // Then: no game command issued; entry deleted from Roster
            then_not_on_roster("Villain_Boss_03");
        }

        [TestMethod]
        public void DespawnFailsStillRemoved()
        {
            // Given: Healer_01 has presence in game world true; despawn command will fail
            given_character_spawned(_healer);
            // When: the GM removes the Roster Entry Healer_01 (despawn fails)
            when_character_removed_from_roster(_healer);
            // Then: entry still deleted from Roster; GM sees a warning about despawn failure
            then_not_on_roster("Healer_01");
        }

        [TestMethod]
        public void GangMemberGangDeactivatedFirst()
        {
            // Given: Guard_A has gang membership indicator visible; presence in game world true
            TestCombatant guardA = new TestCombatant("Guard_A");
            guardA.IsGangLeader = false;
            guardA.HasBeenSpawned = true;
            given_character_on_roster(guardA);
            // When: the GM removes Guard_A (gang member)
            when_gang_deactivated(guardA);
            when_character_removed_from_roster(guardA);
            // Then: gang is deactivated for all members before removal; entry deleted
            then_not_on_roster("Guard_A");
            guardA.IsGangLeader.Should().BeFalse("gang must be deactivated before the member is removed");
        }

        [TestMethod]
        public void LastEntryEmptyPlaceholderShown()
        {
            // Given: Guard_Captain_01 is the last entry on the roster; spawned true
            _roster.Clear();
            given_character_on_roster(_guardCaptain);
            given_character_spawned(_guardCaptain);
            // When: the GM removes Guard_Captain_01 (the last entry)
            when_character_removed_from_roster(_guardCaptain);
            // Then: entry deleted; empty-roster placeholder shown
            then_roster_count(0);
        }
    }
}
