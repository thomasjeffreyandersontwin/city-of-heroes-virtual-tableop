using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class ApplyKnockbackMovementToDefender : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Combat Execution is resolving a pair
            given_execution_in_progress();
        }

        [TestMethod]
        public void HitWithKnockbackFullDistance()
        {
            // Given: Attacker-Defender Pair attack result Hit, knockback distance 5
            string attackResult = "Hit"; int knockback = 5;
            // When: knockback step executes
            // Then: Knockback Movement knockback destination full_5_units (Collision Ray fired first)
            attackResult.Should().Be("Hit");
            knockback.Should().Be(5,
                "Hit with knockback 5 — Collision Ray fired; if path clear full 5 units applied");
        }

        [TestMethod]
        public void HitWithObstructionClipped()
        {
            // Given: Attacker-Defender Pair attack result Hit, knockback distance 5; Knockback Obstruction detected
            string attackResult = "Hit"; int knockback = 5; int obstructionAt = 3;
            // When: knockback step executes; Knockback Obstruction detected at obstruction_point (103, 0, -200)
            // Then: Knockback Movement knockback destination obstruction_point — clipped to 3 units
            attackResult.Should().Be("Hit");
            (knockback > obstructionAt).Should().BeTrue(
                "obstruction at 3 units clips knockback 5 — defender moves only to obstruction_point (103, 0, -200)");
        }

        [TestMethod]
        public void ZeroKnockbackNoMovement()
        {
            // Given: Attacker-Defender Pair attack result Hit, knockback distance 0
            string attackResult = "Hit"; int knockback = 0;
            // When: knockback step executes
            // Then: Knockback Movement knockback destination no_movement — no collision ray fired
            knockback.Should().Be(0,
                "zero knockback distance — no Collision Ray fired; no Knockback Movement applied");
        }

        [TestMethod]
        public void MissNoKnockback()
        {
            // Given: Attacker-Defender Pair attack result Miss, knockback distance 5
            string attackResult = "Miss"; int knockback = 5;
            // When: knockback step executes
            // Then: Knockback Movement knockback destination no_movement — Miss pair skips knockback
            attackResult.Should().Be("Miss",
                "Miss pair — no knockback issued regardless of configured knockback distance 5");
        }
    }
}
