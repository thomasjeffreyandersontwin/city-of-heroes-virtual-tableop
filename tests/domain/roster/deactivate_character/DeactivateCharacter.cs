using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.Roster
{
    [TestClass]
    public class DeactivateCharacter : RosterDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Roster has entries with an Active Character
            given_character_on_roster(_guardCaptain);
            given_character_on_roster(_villainBoss);
            when_character_activated(_guardCaptain);
        }

        [TestMethod]
        public void ActiveEntryDeactivated()
        {
            // Given: Guard_Captain_01 is the active entry; gang membership indicator hidden
            then_active(_guardCaptain);
            // When: the GM triggers Deactivate on Guard_Captain_01
            when_character_deactivated(_guardCaptain);
            // Then: active designation removed (none); no other entry automatically activated
            then_not_active(_guardCaptain);
            then_not_active(_villainBoss);
        }

        [TestMethod]
        public void NotActiveNoOp()
        {
            // Given: Villain_Boss_03 is NOT the active entry; gang membership indicator hidden
            then_not_active(_villainBoss);
            // When: the GM triggers Deactivate on Villain_Boss_03
            when_character_deactivated(_villainBoss);
            // Then: action is a no-op with no error; no other entry affected
            then_not_active(_villainBoss);
            then_active(_guardCaptain); // Guard_Captain_01 unchanged
        }

        [TestMethod]
        public void GangMemberDeactivatedIndividually()
        {
            // Given: Guard_A is a gang member with gang membership indicator visible; Guard_A is active
            TestCombatant guardA = new TestCombatant("Guard_A");
            given_character_on_roster(guardA);
            given_gang_mode_active(guardA, guardA);
            when_character_activated(guardA);
            // When: the GM triggers Deactivate on Guard_A
            when_character_deactivated(guardA);
            // Then: only Guard_A is deactivated; gang mode is not ended (gang membership indicator unchanged)
            then_not_active(guardA);
            guardA.IsGangLeader.Should().BeTrue(
                "gang membership indicator remains visible — gang mode is not ended by individual deactivation");
        }
    }
}
