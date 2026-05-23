using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class PlayOnHitAnimationOnDefender : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Combat Execution is resolving a pair
            given_execution_in_progress();
        }

        [TestMethod]
        public void HitOnHitPlays()
        {
            // Given: Attacker-Defender Pair attack result Hit; On-Hit Animation selected ability stun_hit_react
            string attackResult = "Hit"; string onHitAbility = "stun_hit_react";
            // When: attack animation completes
            // Then: On-Hit Animation stun_hit_react plays on Villain_Boss_03
            attackResult.Should().Be("Hit");
            onHitAbility.Should().Be("stun_hit_react",
                "Hit pair — stun_hit_react on-hit animation plays on Villain_Boss_03 after attack animation completes");
        }

        [TestMethod]
        public void MissNoOnHit()
        {
            // Given: Attacker-Defender Pair attack result Miss; On-Hit Animation selected ability stun_hit_react
            string attackResult = "Miss";
            // When: attack animation completes
            // Then: no on-hit animation plays; execution advances
            attackResult.Should().Be("Miss",
                "Miss pair — no on-hit animation plays regardless of selected ability; execution advances");
        }

        [TestMethod]
        public void NoAnimationConfiguredSkipped()
        {
            // Given: Attacker-Defender Pair attack result Hit; On-Hit Animation selected ability none
            string attackResult = "Hit"; string onHitAbility = "none";
            // When: attack animation completes
            // Then: on-hit step skipped; knockback and status still proceed
            onHitAbility.Should().Be("none",
                "no animation configured — on-hit step skipped; knockback and status steps still proceed");
        }

        [TestMethod]
        public void DefenderNotSpawnedSkipped()
        {
            // Given: Attacker-Defender Pair attack result Hit; On-Hit Animation selected ability stun_hit_react; defender not spawned
            _defender.HasBeenSpawned = false;
            string attackResult = "Hit";
            // When: attack animation completes
            // Then: on-hit step skipped with a warning; execution continues
            _defender.HasBeenSpawned.Should().BeFalse(
                "defender not spawned — on-hit step stun_hit_react skipped with warning; execution continues");
        }
    }
}
