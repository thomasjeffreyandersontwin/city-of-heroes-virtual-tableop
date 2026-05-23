using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class SelectAttackingCharacter : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Game Bridge is initialized
        }

        [TestMethod]
        public void CharacterPreAssignedOnOpen()
        {
            // Given: Attack Configuration opens with Guard_Captain_01 pre-assigned as attacker
            // When: GM opens or changes the Attacker in the Attack Configuration
            bool assigned = when_attacker_assigned(_guardCaptain);
            // Then: Attacker has attacking role Guard_Captain_01; Combat State current role is attacker
            assigned.Should().BeTrue();
            then_role(_guardCaptain, "attacker");
        }

        [TestMethod]
        public void DifferentAttackerSelected()
        {
            // Given: Guard_Captain_01 is the current attacker
            when_attacker_assigned(_guardCaptain);
            // When: GM selects Villain_Boss_03 as a different attacker
            when_defender_removed(_guardCaptain);
            bool assigned = when_attacker_assigned(_villainBoss);
            // Then: Attacker attacking role Villain_Boss_03; previous Guard_Captain_01 role resets to neutral
            assigned.Should().BeTrue();
            then_role(_villainBoss, "attacker");
            then_role(_guardCaptain, RoleNeutral);
        }

        [TestMethod]
        public void AlreadyADefenderRejected()
        {
            // Given: Villain_Boss_03 is already a Defender
            when_defender_added(_villainBoss);
            // When: GM attempts to assign Villain_Boss_03 as Attacker
            bool assigned = when_attacker_assigned(_villainBoss);
            // Then: selection rejected; Attacker attacking role remains unchanged
            assigned.Should().BeFalse(
                "Villain_Boss_03 is already a defender — assignment as attacker must be rejected");
        }

        [TestMethod]
        public void UnspawnedCharacterRejected()
        {
            // Given: Unspawned_Guard has spawned state false
            _guardCaptain.HasBeenSpawned = false;
            // When: GM attempts to assign Unspawned_Guard as Attacker
            bool assigned = when_attacker_assigned(_guardCaptain);
            // Then: selection rejected; Combat State current role unchanged
            assigned.Should().BeFalse(
                "unspawned character must be rejected as attacker — HasBeenSpawned is false");
        }
    }
}
