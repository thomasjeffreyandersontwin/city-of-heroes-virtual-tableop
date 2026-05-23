using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class SetAttackMode : AttackConfigurationHelper
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
        public void AttackModeSelected()
        {
            WhenGmSelectsAttackMode("Attack");
            ThenAttackMode("Attack");
        }

        [TestMethod]
        public void DefendModeSelected()
        {
            WhenGmSelectsAttackMode("Defend");
            ThenAttackMode("Defend");
        }

        [TestMethod]
        public void DefendModeExecutionIdentical()
        {
            WhenGmSelectsAttackMode("Defend");
            ThenAttackMode("Defend");
        }

        [TestMethod]
        public void NoSelectionDefaultAttack()
        {
            ThenAttackMode("Attack");
        }
    }
}
