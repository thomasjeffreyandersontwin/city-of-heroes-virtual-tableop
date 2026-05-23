using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class SetAttackEffect : AttackConfigurationHelper
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
        public void StunnedSelectedHitPair()
        {
            GivenAttackResult("pair_1", "Hit");
            WhenGmSelectsAttackEffect("pair_1", "Stunned");
            ThenStatusEffect("pair_1", "Stunned");
        }

        [TestMethod]
        public void UnconsciousSelectedHitPair()
        {
            GivenAttackResult("pair_1", "Hit");
            WhenGmSelectsAttackEffect("pair_1", "Unconscious");
            ThenStatusEffect("pair_1", "Unconscious");
        }

        [TestMethod]
        public void DeadSelectedHitPair()
        {
            GivenAttackResult("pair_1", "Hit");
            WhenGmSelectsAttackEffect("pair_1", "Dead");
            ThenStatusEffect("pair_1", "Dead");
        }

        [TestMethod]
        public void AnyEffectMissPairNotApplied()
        {
            GivenAttackResult("pair_1", "Miss");
            WhenGmSelectsAttackEffect("pair_1", "Dying");
            ThenStatusEffect("pair_1", "not_applied");
        }

        [TestMethod]
        public void NoEffectSelectedBlocked()
        {
            GivenAttackResult("pair_1", "Hit");
            WhenGmSelectsAttackEffect("pair_1", "");
            ThenConfirmBlocked();
        }
    }
}
