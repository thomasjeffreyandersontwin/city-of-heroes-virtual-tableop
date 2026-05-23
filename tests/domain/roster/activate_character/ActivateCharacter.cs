using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.Roster
{
    [TestClass]
    public class ActivateCharacter : RosterDomainHelper
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
        public void ActivateNewEntry()
        {
            // Given: no active character
            // When: the GM triggers Activate on Roster Entry Guard_Captain_01
            when_character_activated(_guardCaptain);
            // Then: Guard_Captain_01 active turn indicator visible; active designation is Guard_Captain_01
            then_active(_guardCaptain);
        }

        [TestMethod]
        public void ReplaceExistingActive()
        {
            // Given: Guard_Captain_01 is currently active
            when_character_activated(_guardCaptain);
            // When: the GM triggers Activate on Villain_Boss_03
            when_character_activated(_villainBoss);
            // Then: Villain_Boss_03 has active turn indicator visible; active designation Villain_Boss_03
            then_active(_villainBoss);
        }

        [TestMethod]
        public void PreviousActiveCleared()
        {
            // Given: Guard_Captain_01 is active; Villain_Boss_03 is activated
            when_character_activated(_guardCaptain);
            when_character_activated(_villainBoss);
            // Then: Guard_Captain_01 active turn indicator hidden (previous active cleared)
            then_not_active(_guardCaptain);
        }

        [TestMethod]
        public void ActivateUnspawnedEntry()
        {
            // Given: Healer_01 has spawned state false
            then_not_spawned(_healer);
            // When: the GM triggers Activate on Healer_01
            when_character_activated(_healer);
            // Then: Healer_01 has active turn indicator visible (even unspawned); active designation Healer_01
            then_active(_healer);
        }

        [TestMethod]
        public void AlreadyActiveNoOp()
        {
            // Given: Guard_Captain_01 is already active
            when_character_activated(_guardCaptain);
            // When: the GM triggers Activate on Guard_Captain_01 again
            when_character_activated(_guardCaptain);
            // Then: no-op; active designation remains Guard_Captain_01; active turn indicator still visible
            then_active(_guardCaptain);
        }

        [TestMethod]
        public void GangMemberActivatedGangOverrides()
        {
            // Given: Guard_A is a gang member; Gang Mode is active
            TestCombatant guardA = new TestCombatant("Guard_A");
            given_character_on_roster(guardA);
            given_gang_mode_active(guardA, guardA);
            // When: the GM triggers Activate on Guard_A
            when_character_activated(guardA);
            // Then: Guard_A active turn indicator visible; all entries in the gang activated collectively
            then_active(guardA);
            guardA.IsGangLeader.Should().BeTrue(
                "gang member activated — gang overrides; all gang entries activated collectively via gang leader");
        }
    }
}
