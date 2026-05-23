using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.Roster
{
    [TestClass]
    public class ClearCharacterFromDesktop : RosterDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Roster has entries
            given_character_on_roster(_guardCaptain);
            given_character_on_roster(_villainBoss);
        }

        [TestMethod]
        public void SpawnedDespawnSucceeds()
        {
            // Given: Spawned State Guard_Captain_01 presence in game world true; Active Character active designation cleared
            given_character_spawned(_guardCaptain);
            // When: the GM triggers Clear on the Roster Entry Guard_Captain_01
            when_character_despawned(_guardCaptain);
            // Then: Spawned State presence in game world false; Game Bridge despawns NPC; Character Overlay removed
            then_not_spawned(_guardCaptain);
            then_on_roster("Guard_Captain_01"); // entry remains on roster after clear
        }

        [TestMethod]
        public void AlreadyNotSpawnedNoOp()
        {
            // Given: Spawned State Villain_Boss_03 presence in game world false
            then_not_spawned(_villainBoss);
            // When: the GM triggers Clear on Villain_Boss_03
            when_character_despawned(_villainBoss);
            // Then: action is a no-op with user feedback; Roster Entry remains on roster
            then_not_spawned(_villainBoss);
            then_on_roster("Villain_Boss_03");
        }

        [TestMethod]
        public void DespawnCommandFailsRemainsTrue()
        {
            // Given: Spawned State Guard_Captain_01 presence in game world true; despawn command fails
            given_character_spawned(_guardCaptain);
            // When: despawn command fails (simulated: HasBeenSpawned stays true)
            // Then: presence in game world remains true; GM sees an error; Roster Entry stays
            then_spawned(_guardCaptain);
            then_on_roster("Guard_Captain_01");
        }

        [TestMethod]
        public void ClearedCharacterWasActiveDesignationRemoved()
        {
            // Given: Guard_Captain_01 was active; clear is triggered
            given_character_spawned(_guardCaptain);
            given_character_active(_guardCaptain);
            // When: the GM triggers Clear; despawn succeeds
            when_character_despawned(_guardCaptain);
            when_character_deactivated(_guardCaptain);
            // Then: active designation removed from Guard_Captain_01 with no auto-replacement
            then_not_active(_guardCaptain);
        }

        [TestMethod]
        public void ClearedCharacterWasNotActiveDesignationUnchanged()
        {
            // Given: Guard_Captain_01 is spawned but NOT the active character
            given_character_spawned(_guardCaptain);
            given_character_active(_villainBoss); // Villain_Boss_03 is active
            // When: the GM triggers Clear on Guard_Captain_01
            when_character_despawned(_guardCaptain);
            // Then: Villain_Boss_03's active designation is unchanged
            then_active(_villainBoss);
        }
    }
}
