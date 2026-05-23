using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class ActivateAttackAbility : AttackConfigurationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
        }

        [TestMethod]
        public void AttackAbilityActivatedPanelOpens()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            WhenGmActivatesAttackAbility("Guard_Captain_01");
            ThenPanelOpened("Guard_Captain_01");
        }

        [TestMethod]
        public void NoAttackAbilityDefinedBlocked()
        {
            WhenGmActivatesAttackAbility("Guard_Captain_01");
            ThenPanelNotOpened();
        }

        [TestMethod]
        public void PanelOpenAbilitiesLocked()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            WhenGmActivatesAttackAbility("Guard_Captain_01");
            GivenNonAttackAbilitiesLocked("Guard_Captain_01");
            ThenNonAttackAbilitiesLocked("Guard_Captain_01");
        }

        [TestMethod]
        public void GmCancelsStateReset()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            WhenGmActivatesAttackAbility("Guard_Captain_01");
            ThenPanelOpened("Guard_Captain_01");
            ThenNonAttackAbilitiesReleased("Guard_Captain_01");
        }
    }
}
