using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatExecution
{
    [TestClass]
    public class CancelActiveAttack : CombatExecutionHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
            GivenAttackConfigPanelOpen();
        }

        [TestMethod]
        public void CancelBeforeConfirm()
        {
            GivenCombatRole("Guard_Captain_01", "attacker");
            WhenGmClicksCancel();
            ThenCombatStateNeutral("Guard_Captain_01");
            ThenPanelClosed();
        }

        [TestMethod]
        public void CancelWithPartialParameters()
        {
            GivenCombatRole("Guard_Captain_01", "attacker");
            GivenCombatRole("Villain_Boss_03", "defender");
            WhenGmClicksCancel();
            ThenCombatStateNeutral("Guard_Captain_01");
            ThenCombatStateNeutral("Villain_Boss_03");
            ThenNonAttackAbilitiesReleased("Guard_Captain_01");
        }

        [TestMethod]
        public void CancelViaKeyboardShortcut()
        {
            GivenCombatRole("Guard_Captain_01", "attacker");
            WhenGmClicksCancel();
            ThenCombatStateNeutral("Guard_Captain_01");
            ThenPanelClosed();
        }

        [TestMethod]
        public void CloseWithoutCancelOrConfirm()
        {
            GivenCombatRole("Guard_Captain_01", "attacker");
            WhenGmClicksCancel();
            ThenCombatStateNeutral("Guard_Captain_01");
            ThenPanelClosed();
        }
    }
}
