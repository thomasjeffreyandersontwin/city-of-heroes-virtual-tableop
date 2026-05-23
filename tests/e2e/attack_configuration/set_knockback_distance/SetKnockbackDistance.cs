using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class SetKnockbackDistance : AttackConfigurationHelper
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
        public void PositiveValueEntered()
        {
            WhenGmEntersKnockbackDistance("pair_1", "5");
            ThenKnockbackStored("pair_1", "5");
        }

        [TestMethod]
        public void ZeroEnteredNoKnockback()
        {
            WhenGmEntersKnockbackDistance("pair_1", "0");
            ThenKnockbackStored("pair_1", "0");
        }

        [TestMethod]
        public void NonNumericRejected()
        {
            WhenGmEntersKnockbackDistance("pair_1", "abc");
            ThenSelectionRejected();
        }

        [TestMethod]
        public void ObstructionClipsDistance()
        {
            WhenGmEntersKnockbackDistance("pair_1", "5");
            ThenKnockbackStored("pair_1", "5");
        }
    }
}
