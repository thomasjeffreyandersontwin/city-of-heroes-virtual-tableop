using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class ExecuteSweepAttackAcrossMultipleTargets : AttackConfigurationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
            GivenAttackConfigPanelOpen();
            GivenAttackerAssigned("Guard_Captain_01");
            GivenDefenderAdded("Villain_A");
            GivenDefenderAdded("Villain_B");
            GivenDefenderAdded("Villain_C");
            GivenTargetsConfirmed();
        }

        [TestMethod]
        public void AllPairsResolved()
        {
            GivenSweepAttackOrder(new[] { "Pair_1", "Pair_2", "Pair_3" });
            WhenGmConfirmsSweepAttack();
            ThenSweepResolved(new[] { "Pair_1", "Pair_2", "Pair_3" });
        }

        [TestMethod]
        public void MissPairAdvancesWithoutEffects()
        {
            GivenSweepAttackOrder(new[] { "Pair_1", "Pair_2" });
            GivenAttackResult("Pair_1", "Miss");
            GivenAttackResult("Pair_2", "Hit");
            WhenGmConfirmsSweepAttack();
            ThenSweepResolved(new[] { "Pair_1", "Pair_2" });
        }

        [TestMethod]
        public void AbortMidSweepUnresolvedNotApplied()
        {
            GivenSweepAttackOrder(new[] { "Pair_1", "Pair_2" });
            WhenGmConfirmsSweepAttack();
            ThenSweepResolved(new[] { "Pair_1" });
            ThenSweepNotResolved(new[] { "Pair_2" });
        }
    }
}
