using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class DesignateCenterTargetForAreaAttack : AttackConfigurationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
            GivenAttackConfigPanelOpen();
            GivenAttackerAssigned("Guard_Captain_01");
        }

        [TestMethod]
        public void CenterDesignatedTargetsAutoAdded()
        {
            GivenCharactersInRange(new[] { "Villain_A", "Villain_B", "Villain_C" });
            WhenGmDesignatesAreaCenter("Guard_Captain_01");
            ThenAreaCenterDesignated("Guard_Captain_01");
            ThenDefendersPopulated(new[] { "Villain_A", "Villain_B", "Villain_C" });
        }

        [TestMethod]
        public void PopUpMenuNotDeployedBlocked()
        {
            GivenPopUpMenuNotDeployed();
            WhenGmDesignatesAreaCenter("Guard_Captain_01");
            ThenAreaCenterDesignated("blocked");
        }

        [TestMethod]
        public void NoTargetsInRadiusEmpty()
        {
            WhenGmDesignatesAreaCenter("Guard_Captain_01");
            ThenAreaCenterDesignated("Guard_Captain_01");
            ThenDefendersEmpty();
        }

        [TestMethod]
        public void AreaCenterUncheckedReverts()
        {
            WhenGmDesignatesAreaCenter("Guard_Captain_01");
            WhenGmUnchecksAreaCenter();
            ThenAreaCenterDesignated("cleared");
        }
    }
}
