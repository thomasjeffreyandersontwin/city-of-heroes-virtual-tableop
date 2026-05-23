using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class ExecuteRangedAreaAttack : AttackConfigurationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
            GivenAttackConfigPanelOpen();
            GivenAttackerAssigned("Guard_Captain_01");
            GivenAreaCenterDesignated("Guard_Captain_01");
        }

        [TestMethod]
        public void ClearLosDefenderIncluded()
        {
            GivenDefenderAdded("Villain_Boss_03");
            WhenGmConfirmsAreaAttack();
            ThenLineOfSight("Villain_Boss_03", "clear");
        }

        [TestMethod]
        public void BlockedLosDefenderExcluded()
        {
            GivenDefenderAdded("Villain_Boss_03");
            GivenBlockedLos("Villain_Boss_03");
            WhenGmConfirmsAreaAttack();
            ThenLineOfSight("Villain_Boss_03", "blocked");
        }

        [TestMethod]
        public void AllBlockedNoExecution()
        {
            GivenDefenderAdded("Villain_Boss_03");
            GivenBlockedLos("Villain_Boss_03");
            WhenGmConfirmsAreaAttack();
            ThenLineOfSight("Villain_Boss_03", "blocked");
        }
    }
}
