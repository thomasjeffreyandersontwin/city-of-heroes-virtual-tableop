using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatExecution
{
    [TestClass]
    public class ResetCharacterCombatState : CombatExecutionHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void ResetAfterCompletedAttack()
        {
            GivenNonNeutralCombatState("Villain_Boss_03", "defender", "Stunned");
            WhenGmTriggersResetCombatState("Villain_Boss_03");
            ThenCombatStateNeutral("Villain_Boss_03");
            ThenNonAttackAbilitiesReleased("Villain_Boss_03");
            ThenIndicatorCleared("Villain_Boss_03");
        }

        [TestMethod]
        public void ResetDuringActiveConfigBlocked()
        {
            GivenNonNeutralCombatState("Guard_Captain_01", "attacker", "none");
            GivenConfigurationLinkage("Guard_Captain_01", "active");
            WhenGmTriggersResetCombatState("Guard_Captain_01");
            ThenResetBlocked();
        }

        [TestMethod]
        public void ResetDeadCharacterEligibleForCombatAgain()
        {
            GivenNonNeutralCombatState("Villain_Boss_03", "defender", "Dead");
            WhenGmTriggersResetCombatState("Villain_Boss_03");
            ThenCombatStateNeutral("Villain_Boss_03");
            ThenNonAttackAbilitiesReleased("Villain_Boss_03");
        }
    }
}
