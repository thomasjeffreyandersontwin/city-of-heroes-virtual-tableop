using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class ConfirmAttackTargets : AttackConfigurationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
            GivenAttackConfigPanelOpen();
        }

        [TestMethod]
        public void ValidLockSucceeds()
        {
            GivenAttackerAssigned("Guard_Captain_01");
            GivenDefenderAdded("Villain_Boss_03");
            WhenGmClicksConfirmTargets();
            ThenTargetsLocked();
        }

        [TestMethod]
        public void NoDefenderBlocked()
        {
            GivenAttackerAssigned("Guard_Captain_01");
            WhenGmClicksConfirmTargets();
            ThenConfirmBlocked();
        }

        [TestMethod]
        public void NoAttackerBlocked()
        {
            GivenDefenderAdded("Villain_Boss_03");
            WhenGmClicksConfirmTargets();
            ThenConfirmBlocked();
        }

        [TestMethod]
        public void PostLockAddRemoveDisabled()
        {
            GivenAttackerAssigned("Guard_Captain_01");
            GivenDefenderAdded("Villain_Boss_03");
            WhenGmClicksConfirmTargets();
            ThenTargetsLocked();
        }
    }
}
