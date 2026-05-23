using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class SelectDefenderTargets : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration panel is open with an Attacker assigned
            given_panel_open();
            when_attacker_assigned(_guardCaptain);
        }

        [TestMethod]
        public void AddSpawnedDefender()
        {
            // Given: Villain_Boss_03 is spawned; not the attacker
            // When: GM adds Villain_Boss_03 as a Defender
            bool added = when_defender_added(_villainBoss);
            // Then: Defender defending role Villain_Boss_03; Combat State current role defender
            added.Should().BeTrue();
            then_defender_in_list("Villain_Boss_03");
            then_role(_villainBoss, "defender");
        }

        [TestMethod]
        public void AddSecondDefender()
        {
            // Given: Villain_Boss_03 is already a defender
            when_defender_added(_villainBoss);
            // When: GM adds Healer_01 as a second Defender
            bool added = when_defender_added(_healer);
            // Then: Defender defending role Healer_01; Attacker-Defender Pair created with default parameters
            added.Should().BeTrue();
            then_defender_in_list("Healer_01");
            then_role(_healer, "defender");
        }

        [TestMethod]
        public void AlreadyTheAttackerRejected()
        {
            // Given: Guard_Captain_01 is already the Attacker
            // When: GM attempts to add Guard_Captain_01 as a Defender
            bool added = when_defender_added(_guardCaptain);
            // Then: addition rejected; Guard_Captain_01 remains attacker
            added.Should().BeFalse(
                "Guard_Captain_01 is already the attacker — adding as defender must be rejected");
        }

        [TestMethod]
        public void UnspawnedRejected()
        {
            // Given: Villain_Boss_03 has spawned state false
            _villainBoss.HasBeenSpawned = false;
            // When: GM adds Villain_Boss_03 as a Defender
            bool added = when_defender_added(_villainBoss);
            // Then: addition rejected; unspawned character cannot be a defender
            added.Should().BeFalse(
                "unspawned Villain_Boss_03 must be rejected as defender — HasBeenSpawned is false");
        }

        [TestMethod]
        public void RemoveDefender()
        {
            // Given: Villain_Boss_03 is a defender in the list
            when_defender_added(_villainBoss);
            // When: GM removes Villain_Boss_03 from the Defender list
            when_defender_removed(_villainBoss);
            // Then: Attacker-Defender Pair deleted; Combat State current role resets to neutral
            then_defender_not_in_list("Villain_Boss_03");
            then_role(_villainBoss, "neutral");
        }
    }
}
