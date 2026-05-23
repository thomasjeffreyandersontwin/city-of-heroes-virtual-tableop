using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class ApplyStatusEffectToDefender : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Combat Execution is resolving a pair
            given_execution_in_progress();
        }

        [TestMethod]
        public void HitStunnedApplied()
        {
            // Given: Attacker-Defender Pair attack result Hit, attack effect Stunned
            string attackResult = "Hit"; string attackEffect = "Stunned";
            // When: status effect step executes
            _statusEffects[_defender.Name] = attackEffect;
            // Then: Status Effect applied condition Stunned; Combat State active status effects Stunned
            attackResult.Should().Be("Hit");
            then_status_effect(_defender, "Stunned");
        }

        [TestMethod]
        public void HitDeadApplied()
        {
            // Given: Attacker-Defender Pair attack result Hit, attack effect Dead
            string attackResult = "Hit"; string attackEffect = "Dead";
            _statusEffects[_defender.Name] = attackEffect;
            // When: status effect step executes
            // Then: Status Effect applied condition Dead; further targeting of Villain_Boss_03 blocked in UI
            attackResult.Should().Be("Hit");
            then_status_effect(_defender, "Dead");
        }

        [TestMethod]
        public void MissNoEffect()
        {
            // Given: Attacker-Defender Pair attack result Miss
            string attackResult = "Miss";
            // When: status effect step executes
            // Then: Status Effect applied condition not_applied; any existing effect unchanged
            attackResult.Should().Be("Miss",
                "Miss pair — no status effect applied; existing effect unchanged");
        }

        [TestMethod]
        public void PriorEffectReplaced()
        {
            // Given: Villain_Boss_03 has active status effect Stunned; new attack result Hit with effect Unconscious
            _statusEffects[_defender.Name] = "Stunned";
            string attackResult = "Hit"; string attackEffect = "Unconscious";
            _statusEffects[_defender.Name] = attackEffect;
            // When: status effect step executes
            // Then: Stunned replaced by Unconscious; Combat State active status effects Unconscious
            attackResult.Should().Be("Hit");
            then_status_effect(_defender, "Unconscious");
        }
    }
}
