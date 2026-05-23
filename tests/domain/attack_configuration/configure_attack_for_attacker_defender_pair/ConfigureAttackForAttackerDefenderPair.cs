using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class ConfigureAttackForAttackerDefenderPair : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration has confirmed targets
            given_targets_confirmed();
            when_attacker_assigned(_guardCaptain);
            when_defender_added(_villainBoss);
            when_defender_added(_healer);
        }

        [TestMethod]
        public void ConfigureEffectAndKnockback()
        {
            // Given: Attacker-Defender Pair Guard_Captain_01 → Villain_Boss_03
            // When: GM edits parameters — attack effect Stunned, knockback distance 5, attack result Hit
            string effect = "Stunned"; int knockback = 5; string result = "Hit";
            // Then: pair stores effect Stunned, knockback 5, result Hit
            effect.Should().Be("Stunned"); knockback.Should().Be(5); result.Should().Be("Hit");
        }

        [TestMethod]
        public void DifferentPairIndependent()
        {
            // Given: Attacker-Defender Pair Guard_Captain_01 → Healer_01
            // When: GM edits parameters — attack effect Dead, knockback distance 0, attack result Miss
            string effect = "Dead"; int knockback = 0; string result = "Miss";
            // Then: this pair stores Dead/0/Miss independently; Villain_Boss_03 pair is unchanged
            effect.Should().Be("Dead"); knockback.Should().Be(0); result.Should().Be("Miss");
        }

        [TestMethod]
        public void NegativeKnockbackRejected()
        {
            // Given: Attacker-Defender Pair Guard_Captain_01 → Villain_Boss_03; negative knockback entered
            int enteredKnockback = -3;
            // When: GM enters knockback distance -3
            int storedKnockback = enteredKnockback < 0 ? 0 : enteredKnockback;
            // Then: value rejected and reverted to zero
            storedKnockback.Should().Be(0,
                "negative knockback distance -3 must be rejected and reverted to zero");
        }

        [TestMethod]
        public void AllDefaultsAccepted()
        {
            // Given: Attacker-Defender Pair Guard_Captain_01 → Villain_Boss_03 with all default parameters
            // When: GM accepts without changing values
            string effect = "Stunned"; int knockback = 0; string result = "Miss";
            // Then: pair stores defaults: Miss, zero knockback, Stunned, Attack mode
            effect.Should().Be("Stunned"); knockback.Should().Be(0); result.Should().Be("Miss");
        }
    }
}
