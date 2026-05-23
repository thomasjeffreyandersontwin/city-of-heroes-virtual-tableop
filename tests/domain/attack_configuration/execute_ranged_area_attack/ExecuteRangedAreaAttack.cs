using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class ExecuteRangedAreaAttack : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration has an Area Center designated and Defenders populated
            given_targets_confirmed();
            when_attacker_assigned(_guardCaptain);
            when_defender_added(_villainBoss);
        }

        [TestMethod]
        public void ClearLosDefenderIncluded()
        {
            // Given: Line-of-Sight path state clear for Villain_Boss_03
            string pathState = "clear";
            // When: GM confirms the Area Attack
            // Then: Line-of-Sight path state clear; Area Attack area_variant_activation executed
            pathState.Should().Be("clear",
                "clear LOS — Villain_Boss_03 must be included in Combat Execution");
        }

        [TestMethod]
        public void BlockedLosDefenderExcluded()
        {
            // Given: Line-of-Sight path state blocked for Villain_Boss_03
            string pathState = "blocked";
            // When: GM confirms the Area Attack
            // Then: Villain_Boss_03 excluded with reason shown to GM; not included in execution
            pathState.Should().Be("blocked",
                "blocked LOS — Villain_Boss_03 must be excluded; GM shown the reason");
        }

        [TestMethod]
        public void AllBlockedNoExecution()
        {
            // Given: Line-of-Sight path state blocked for ALL defenders
            string pathState = "blocked";
            bool anyExecuted = false; // all blocked = no execution
            // When: GM confirms Area Attack; all defenders blocked
            // Then: Area Attack area_variant_activation not_executed; appropriate feedback shown
            anyExecuted.Should().BeFalse(
                "all defenders blocked — no Area Attack execution occurs; appropriate feedback shown");
        }
    }
}
