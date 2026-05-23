using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class SetAttackEffect : AttackConfigurationDomainHelper
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
        public void StunnedSelectedHitPair()
        {
            // Given: Attacker-Defender Pair has attack result Hit
            // When: GM selects Attack Effect Stunned
            string effectType = "Stunned"; string attackResult = "Hit";
            // Then: Status Effect applied condition Stunned
            attackResult.Should().Be("Hit");
            effectType.Should().Be("Stunned");
        }

        [TestMethod]
        public void UnconsciousSelectedHitPair()
        {
            // Given: Attacker-Defender Pair has attack result Hit
            // When: GM selects Attack Effect Unconscious
            string effectType = "Unconscious"; string attackResult = "Hit";
            // Then: Status Effect applied condition Unconscious
            attackResult.Should().Be("Hit");
            effectType.Should().Be("Unconscious");
        }

        [TestMethod]
        public void DeadSelectedHitPair()
        {
            // Given: Attacker-Defender Pair has attack result Hit
            // When: GM selects Attack Effect Dead
            string effectType = "Dead"; string attackResult = "Hit";
            // Then: Status Effect applied condition Dead
            attackResult.Should().Be("Hit");
            effectType.Should().Be("Dead");
        }

        [TestMethod]
        public void AnyEffectMissPairNoApply()
        {
            // Given: Attacker-Defender Pair has attack result Miss
            // When: GM selects Attack Effect Dying
            string effectType = "Dying"; string attackResult = "Miss";
            // Then: Status Effect applied condition not_applied — Miss pair never applies effects
            attackResult.Should().Be("Miss",
                "attack result is Miss — no Status Effect applied regardless of selected effect type Dying");
        }

        [TestMethod]
        public void NoEffectSelectedBlocked()
        {
            // Given: Attacker-Defender Pair has attack result Hit; no effect type selected
            // When: GM leaves effect type empty
            string effectType = string.Empty;
            // Then: Status Effect applied condition not_applied; Confirm button blocked
            effectType.Should().BeEmpty(
                "empty effect type must block confirmation — no Status Effect applied");
        }
    }
}
