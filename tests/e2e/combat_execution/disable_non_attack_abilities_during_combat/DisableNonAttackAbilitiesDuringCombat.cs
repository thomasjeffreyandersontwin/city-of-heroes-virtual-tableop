using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatExecution
{
    [TestClass]
    public class DisableNonAttackAbilitiesDuringCombat : CombatExecutionHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
            GivenAttackConfigPanelOpen();
        }

        [TestMethod]
        public void AssignedAsAttackerLocked()
        {
            GivenCombatRole("Guard_Captain_01", "attacker");
            WhenNonAttackLockEvaluated("Guard_Captain_01");
            ThenNonAttackAbilitiesLocked("Guard_Captain_01");
        }

        [TestMethod]
        public void AssignedAsDefenderLocked()
        {
            GivenCombatRole("Villain_Boss_03", "defender");
            WhenNonAttackLockEvaluated("Villain_Boss_03");
            ThenNonAttackAbilitiesLocked("Villain_Boss_03");
        }

        [TestMethod]
        public void ConfigCancelledReleased()
        {
            GivenCombatRole("Guard_Captain_01", "attacker");
            WhenGmClicksCancel();
            ThenNonAttackAbilitiesReleased("Guard_Captain_01");
        }

        [TestMethod]
        public void RemovedBeforeConfirmReleased()
        {
            GivenCombatRole("Villain_Boss_03", "defender");
            WhenRoleRemoved("Villain_Boss_03");
            ThenNonAttackAbilitiesReleased("Villain_Boss_03");
        }
    }
}
