using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AttackConfiguration
{
    [TestClass]
    public class SelectAttackingCharacter : AttackConfigurationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
        }

        [TestMethod]
        public void CharacterPreAssignedOnOpen()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            WhenGmSelectsAttacker("Guard_Captain_01");
            ThenAttackerRole("Guard_Captain_01", "attacker");
        }

        [TestMethod]
        public void DifferentAttackerSelected()
        {
            GivenSpawnedState("Guard_Captain_01", "true");
            GivenSpawnedState("Villain_Boss_03", "true");
            WhenGmSelectsAttacker("Guard_Captain_01");
            WhenGmSelectsAttacker("Villain_Boss_03");
            ThenAttackerRole("Villain_Boss_03", "attacker");
        }

        [TestMethod]
        public void AlreadyDefenderRejected()
        {
            GivenSpawnedState("Villain_Boss_03", "true");
            GivenAttackConfigPanelOpen();
            GivenDefenderAdded("Villain_Boss_03");
            WhenGmSelectsAttacker("Villain_Boss_03");
            ThenSelectionRejected();
        }

        [TestMethod]
        public void UnspawnedCharacterRejected()
        {
            GivenSpawnedState("Guard_Captain_01", "false");
            WhenGmSelectsAttacker("Guard_Captain_01");
            ThenSelectionRejected();
        }
    }
}
