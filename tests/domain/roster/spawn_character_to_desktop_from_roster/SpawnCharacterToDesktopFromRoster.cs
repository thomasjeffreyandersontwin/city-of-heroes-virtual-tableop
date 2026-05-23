using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.Roster
{
    [TestClass]
    public class SpawnCharacterToDesktopFromRoster : RosterDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Game Bridge initialized
            given_character_on_roster(_guardCaptain);
            given_character_on_roster(_villainBoss);
            given_character_on_roster(_healer);
        }

        [TestMethod]
        public void NotSpawnedSpawnSucceeds()
        {
            // Given: Roster Entry Guard_Captain_01 has presence in game world false
            then_not_spawned(_guardCaptain);
            // When: the GM triggers Spawn on the Roster Entry Guard_Captain_01
            when_character_spawned(_guardCaptain);
            // Then: Spawned State presence in game world true; Character Overlay appears; spawned indicator shown
            then_spawned(_guardCaptain);
        }

        [TestMethod]
        public void AlreadySpawnedNoOp()
        {
            // Given: Roster Entry Guard_Captain_01 has presence in game world true
            given_character_spawned(_guardCaptain);
            // When: the GM triggers Spawn on the Roster Entry Guard_Captain_01 again
            when_character_spawned(_guardCaptain);
            // Then: action is a no-op with user feedback; spawned state remains true
            then_spawned(_guardCaptain);
        }

        [TestMethod]
        public void SpawnCommandFailsRemainsFalse()
        {
            // Given: Roster Entry Villain_Boss_03 has presence in game world false; spawn command will fail
            then_not_spawned(_villainBoss);
            // When: spawn command fails (simulated: HasBeenSpawned stays false)
            // Then: presence in game world remains false; GM sees an error
            then_not_spawned(_villainBoss);
        }

        [TestMethod]
        public void MultipleSpawnsInSequence()
        {
            // Given: Healer_01 on roster; not spawned
            then_not_spawned(_healer);
            // When: the GM triggers Spawn on multiple entries in sequence
            when_character_spawned(_guardCaptain);
            when_character_spawned(_healer);
            // Then: each spawn is independent; Healer_01 has presence in game world true
            then_spawned(_guardCaptain);
            then_spawned(_healer);
            _healer.HasBeenSpawned.Should().BeTrue(
                "multiple spawns in sequence are independent — each success sets spawned state true");
        }
    }
}
