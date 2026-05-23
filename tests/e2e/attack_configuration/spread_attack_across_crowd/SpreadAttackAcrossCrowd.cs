using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class SpreadAttackAcrossCrowd : AttackConfigurationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
            GivenAttackConfigPanelOpen();
            GivenAttackerAssigned("Guard_Captain_01");
        }

        [TestMethod]
        public void MembersInRangeAutoAdded()
        {
            GivenCharactersInRange(new[] { "Villain_A", "Villain_B" });
            WhenGmTriggersSpreadAttack("Guard_Captain_01");
            ThenDefendersPopulated(new[] { "Villain_A", "Villain_B" });
        }

        [TestMethod]
        public void MultipleCrowdsInRangeAllIncluded()
        {
            GivenCharactersInRange(new[] { "Villain_A", "Guard_X", "Ally_Y" });
            WhenGmTriggersSpreadAttack("Guard_Captain_01");
            ThenDefendersPopulated(new[] { "Villain_A", "Guard_X", "Ally_Y" });
        }

        [TestMethod]
        public void NoMembersInRangeEmpty()
        {
            WhenGmTriggersSpreadAttack("Guard_Captain_01");
            ThenDefendersEmpty();
        }
    }
}
