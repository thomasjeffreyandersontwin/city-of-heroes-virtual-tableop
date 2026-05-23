using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class SetAttackResult : AttackConfigurationHelper
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
        public void HitSelected()
        {
            WhenGmSelectsAttackResult("pair_1", "Hit");
            ThenPairParameters("pair_1", "Stunned", "0", "Hit");
        }

        [TestMethod]
        public void MissSelected()
        {
            WhenGmSelectsAttackResult("pair_1", "Miss");
            ThenPairParameters("pair_1", "Stunned", "0", "Miss");
        }

        [TestMethod]
        public void MultiDefenderMixedResults()
        {
            GivenDefenderAdded("Healer_01");
            WhenGmSelectsAttackResult("pair_1", "Hit");
            WhenGmSelectsAttackResult("pair_2", "Miss");
            ThenPairParameters("pair_1", "Stunned", "0", "Hit");
            ThenPairParameters("pair_2", "Stunned", "0", "Miss");
        }

        [TestMethod]
        public void NoResultSelectedBlocked()
        {
            WhenGmSelectsAttackResult("pair_1", "");
            ThenConfirmBlocked();
        }
    }
}
