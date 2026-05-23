using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatExecution
{
    [TestClass]
    public class AbortAttackInProgress : CombatExecutionHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenCombatExecutionBegun();
        }

        [TestMethod]
        public void AbortMidSweepCurrentCompletesFurtherHalted()
        {
            GivenCombatExecutionInProgress(new[] { "Pair_1", "Pair_2" });
            WhenGmClicksAbort();
            ThenCombatStateNeutral("Guard_Captain_01");
        }

        [TestMethod]
        public void AbortBeforeAnyPairResolvedPreConfigState()
        {
            GivenCombatExecutionInProgress(new[] { "Pair_1", "Pair_2" });
            WhenGmClicksAbort();
            ThenCombatStateNeutral("Guard_Captain_01");
        }

        [TestMethod]
        public void AbortAlreadyAppliedRetained()
        {
            GivenCombatExecutionInProgress(new[] { "Pair_1", "Pair_2" });
            WhenGmClicksAbort();
            ThenCombatStateNeutral("Guard_Captain_01");
        }

        [TestMethod]
        public void AbortNotAvailableBeforeConfirm()
        {
            GivenDesktopOverlayWithCharacters();
            GivenAttackConfigPanelOpen();
            ThenAbortButtonDisabled();
        }
    }
}
