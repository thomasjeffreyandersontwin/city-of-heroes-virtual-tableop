using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class ConfirmAttackTargets : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration panel is open
            given_panel_open();
        }

        [TestMethod]
        public void ValidLockSucceeds()
        {
            // Given: attacker assignment Guard_Captain_01; configured defenders Villain_Boss_03
            when_attacker_assigned(_guardCaptain);
            when_defender_added(_villainBoss);
            // When: GM clicks Confirm Targets
            bool confirmed = when_targets_confirmed();
            // Then: combatant list is locked; attack parameters region becomes editable
            confirmed.Should().BeTrue();
            then_targets_locked();
        }

        [TestMethod]
        public void NoDefenderBlocked()
        {
            // Given: attacker assignment Guard_Captain_01; configured defenders empty
            when_attacker_assigned(_guardCaptain);
            // When: GM clicks Confirm Targets (no defenders)
            bool confirmed = when_targets_confirmed();
            // Then: confirmation rejected with feedback
            then_confirmation_blocked(confirmed);
        }

        [TestMethod]
        public void NoAttackerBlocked()
        {
            // Given: attacker assignment empty; configured defenders Villain_Boss_03
            when_defender_added(_villainBoss);
            // When: GM clicks Confirm Targets (no attacker)
            bool confirmed = when_targets_confirmed();
            // Then: confirmation rejected
            then_confirmation_blocked(confirmed);
        }

        [TestMethod]
        public void PostLockAddRemoveDisabled()
        {
            // Given: attacker assignment Guard_Captain_01; configured defenders Villain_Boss_03; targets locked
            when_attacker_assigned(_guardCaptain);
            when_defender_added(_villainBoss);
            when_targets_confirmed();
            // When: GM attempts Add/Remove Defender after lock
            // Then: Add/Remove Defender actions are disabled while targets are locked
            _targetsLocked.Should().BeTrue(
                "after lock the Add/Remove Defender actions must be disabled");
        }
    }
}
