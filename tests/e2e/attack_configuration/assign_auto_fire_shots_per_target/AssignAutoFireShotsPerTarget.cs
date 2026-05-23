using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class AssignAutoFireShotsPerTarget : AttackConfigurationHelper
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
        public void DividesEvenly6Shots3Targets()
        {
            WhenGmEntersAutoFireShots("6");
            ThenAutoFireDistribution("2, 2, 2");
        }

        [TestMethod]
        public void Remainder7Shots3Targets()
        {
            WhenGmEntersAutoFireShots("7");
            ThenAutoFireDistribution("3, 2, 2");
        }

        [TestMethod]
        public void ZeroOrBlankSingleExchange()
        {
            WhenGmEntersAutoFireShots("0");
            ThenAutoFireDistribution("1, 1, 1");
        }

        [TestMethod]
        public void MultiShotPerPairRepeats()
        {
            WhenGmEntersAutoFireShots("4");
            ThenAutoFireDistribution("2, 1, 1");
        }
    }
}
