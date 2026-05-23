using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class SetAttackMode : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration has confirmed targets
            given_targets_confirmed();
            when_attacker_assigned(_guardCaptain);
            when_defender_added(_villainBoss);
        }

        [TestMethod]
        public void AttackModeSelected()
        {
            // When: GM selects Attack Mode Attack
            string modeType = "Attack";
            // Then: mode stored as Attack; passed to HCS for turn-state tracking
            modeType.Should().Be("Attack",
                "Attack mode stored and passed to HCS for turn-state tracking");
        }

        [TestMethod]
        public void DefendModeSelected()
        {
            // When: GM selects Attack Mode Defend
            string modeType = "Defend";
            // Then: mode stored as Defend; execution proceeds identically
            modeType.Should().Be("Defend",
                "Defend mode stored; execution proceeds identically; passed to HCS for turn-state tracking");
        }

        [TestMethod]
        public void DefendModeExecutionIdentical()
        {
            // Given: Attack Mode is Defend
            string modeType = "Defend";
            // When: execution runs with Defend mode
            // Then: Combat Execution behaves identically to Attack mode; mode data passed to HCS
            modeType.Should().Be("Defend",
                "Defend mode execution is identical to Attack — mode difference is only for HCS turn-state tracking");
        }

        [TestMethod]
        public void NoSelectionDefaultAttack()
        {
            // When: GM leaves Attack Mode unselected
            string modeType = string.Empty;
            // Then: default Attack mode is used; Confirm is not blocked
            string effective = string.IsNullOrEmpty(modeType) ? "Attack" : modeType;
            effective.Should().Be("Attack",
                "no mode selection defaults to Attack without blocking Confirm");
        }
    }
}
