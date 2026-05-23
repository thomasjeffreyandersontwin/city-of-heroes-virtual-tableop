using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatExecution
{
    [TestClass]
    public class UpdateCharacterAttackStateIndicators : CombatExecutionHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenDesktopOverlayWithCharacters();
        }

        [TestMethod]
        public void StatusEffectAppliedIndicatorShows()
        {
            GivenNonNeutralCombatState("Villain_Boss_03", "defender", "Stunned");
            WhenCombatStateChanges("Villain_Boss_03");
            ThenAttackStateIndicator("Villain_Boss_03", "Stunned", "defender");
        }

        [TestMethod]
        public void AttackerRoleSetIndicatorShows()
        {
            GivenNonNeutralCombatState("Guard_Captain_01", "attacker", "none");
            WhenCombatStateChanges("Guard_Captain_01");
            ThenAttackStateIndicator("Guard_Captain_01", "none", "attacker");
        }

        [TestMethod]
        public void CombatStateResetIndicatorsCleared()
        {
            GivenNonNeutralCombatState("Villain_Boss_03", "defender", "Stunned");
            WhenGmTriggersResetCombatState("Villain_Boss_03");
            ThenIndicatorCleared("Villain_Boss_03");
        }

        [TestMethod]
        public void ExecutionCompletesFinalStateShown()
        {
            GivenNonNeutralCombatState("Villain_Boss_03", "defender", "Dead");
            WhenCombatStateChanges("Villain_Boss_03");
            ThenAttackStateIndicator("Villain_Boss_03", "Dead", "defender");
        }
    }
}
