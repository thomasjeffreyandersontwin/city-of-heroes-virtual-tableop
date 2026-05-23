using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class ConfigureAttackForAttackerDefenderPair : AttackConfigurationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
            GivenAttackConfigPanelOpen();
            GivenAttackerAssigned("Guard_Captain_01");
            GivenDefenderAdded("Villain_Boss_03");
            GivenTargetsConfirmed();
        }

        [TestMethod]
        public void ConfigureEffectAndKnockback()
        {
            WhenGmEditsAttackParameters("pair_1", "Stunned", 5, "Hit");
            ThenPairParameters("pair_1", "Stunned", "5", "Hit");
        }

        [TestMethod]
        public void DifferentPairIndependent()
        {
            GivenDefenderAdded("Healer_01");
            WhenGmEditsAttackParameters("pair_1", "Stunned", 5, "Hit");
            WhenGmEditsAttackParameters("pair_2", "Dead", 0, "Miss");
            ThenPairParameters("pair_2", "Dead", "0", "Miss");
        }

        [TestMethod]
        public void NegativeKnockbackRejectedRevertsToZero()
        {
            WhenGmEditsAttackParameters("pair_1", "Stunned", -5, "Hit");
            ThenPairParameters("pair_1", "Stunned", "0", "Hit");
        }

        [TestMethod]
        public void AllDefaultsAccepted()
        {
            ThenPairParameters("pair_1", "Stunned", "0", "Miss");
        }
    }
}
