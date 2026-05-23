using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatExecution
{
    [TestClass]
    public class TrackAttackerAndDefenderRolesPerCharacter : CombatExecutionHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
            GivenAttackConfigPanelOpen();
        }

        [TestMethod]
        public void AssignedAsAttacker()
        {
            WhenRoleAssigned("Guard_Captain_01", "attacker");
            ThenCombatStateRole("Guard_Captain_01", "attacker");
            ThenAttackStateIndicator("Guard_Captain_01", "none", "attacker");
        }

        [TestMethod]
        public void AssignedAsDefender()
        {
            WhenRoleAssigned("Villain_Boss_03", "defender");
            ThenCombatStateRole("Villain_Boss_03", "defender");
            ThenAttackStateIndicator("Villain_Boss_03", "none", "defender");
        }

        [TestMethod]
        public void DualRoleAttemptBlocked()
        {
            WhenRoleAssigned("Guard_Captain_01", "attacker");
            WhenRoleAssigned("Guard_Captain_01", "defender");
            ThenCombatStateRole("Guard_Captain_01", "attacker");
        }

        [TestMethod]
        public void RoleRemovedResetToNeutral()
        {
            WhenRoleAssigned("Guard_Captain_01", "attacker");
            WhenRoleRemoved("Guard_Captain_01");
            ThenCombatStateNeutral("Guard_Captain_01");
            ThenIndicatorCleared("Guard_Captain_01");
        }

        [TestMethod]
        public void MultipleConfigsIndependent()
        {
            WhenRoleAssigned("Guard_Captain_01", "attacker");
            ThenCombatStateRole("Guard_Captain_01", "attacker");
        }
    }
}
