using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatExecution
{
    [TestClass]
    public class ApplyStatusEffectToDefender : CombatExecutionHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenCombatExecutionBegun();
        }

        [TestMethod]
        public void HitStunnedApplied()
        {
            GivenPairResult("pair_1", "Hit");
            GivenPairEffect("pair_1", "Stunned");
            WhenStatusEffectExecutes("pair_1");
            ThenStatusEffectApplied("Villain_Boss_03", "Stunned");
        }

        [TestMethod]
        public void HitDeadApplied()
        {
            GivenPairResult("pair_1", "Hit");
            GivenPairEffect("pair_1", "Dead");
            WhenStatusEffectExecutes("pair_1");
            ThenStatusEffectApplied("Villain_Boss_03", "Dead");
        }

        [TestMethod]
        public void MissNoEffect()
        {
            GivenPairResult("pair_1", "Miss");
            GivenPairEffect("pair_1", "Stunned");
            WhenStatusEffectExecutes("pair_1");
            ThenStatusEffectApplied("Villain_Boss_03", "not_applied");
        }

        [TestMethod]
        public void PriorEffectReplaced()
        {
            GivenPairResult("pair_1", "Hit");
            GivenPairEffect("pair_1", "Unconscious");
            GivenNonNeutralCombatState("Villain_Boss_03", "defender", "Stunned");
            WhenStatusEffectExecutes("pair_1");
            ThenStatusEffectApplied("Villain_Boss_03", "Unconscious");
        }
    }
}
