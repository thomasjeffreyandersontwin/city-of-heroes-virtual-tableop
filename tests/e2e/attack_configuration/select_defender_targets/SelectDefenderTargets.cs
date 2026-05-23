using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class SelectDefenderTargets : AttackConfigurationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
            GivenAttackConfigPanelOpen();
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenAttackerAssigned("Guard_Captain_01");
        }

        [TestMethod]
        public void AddSpawnedDefender()
        {
            GivenSpawnedState("Villain_Boss_03", "true");
            WhenGmAddsDefender("Villain_Boss_03");
            ThenAttackerRole("Villain_Boss_03", "defender");
        }

        [TestMethod]
        public void AddSecondDefender()
        {
            GivenSpawnedState("Villain_Boss_03", "true");
            GivenSpawnedState("Healer_01", "true");
            WhenGmAddsDefender("Villain_Boss_03");
            WhenGmAddsDefender("Healer_01");
            ThenAttackerRole("Healer_01", "defender");
        }

        [TestMethod]
        public void AlreadyAttackerRejected()
        {
            WhenGmAddsDefender("Guard_Captain_01");
            ThenSelectionRejected();
        }

        [TestMethod]
        public void UnspawnedRejected()
        {
            GivenSpawnedState("Villain_Boss_03", "false");
            WhenGmAddsDefender("Villain_Boss_03");
            ThenSelectionRejected();
        }

        [TestMethod]
        public void RemoveDefenderResetsToNeutral()
        {
            GivenSpawnedState("Villain_Boss_03", "true");
            WhenGmAddsDefender("Villain_Boss_03");
            WhenGmRemovesDefender("Villain_Boss_03");
            ThenAttackerRole("Villain_Boss_03", "neutral");
        }
    }
}
