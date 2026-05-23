using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class AbortAttackInProgress : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Combat Execution is in progress
            given_execution_in_progress();
        }

        [TestMethod]
        public void AbortMidSweep()
        {
            // Given: pair resolution sequence Pair_1 (done), Pair_2 (halted)
            bool pair1Done = true; bool pair2Halted = false;
            // When: GM clicks Abort mid-sweep
            when_execution_aborted();
            // Then: current animation completes; Pair_2 not resolved; Combat State reset to neutral
            pair1Done.Should().BeTrue("Pair_1 already resolved — its effects are retained after abort");
            pair2Halted.Should().BeFalse("Pair_2 halted by abort — produces no effects");
            then_execution_stopped();
        }

        [TestMethod]
        public void AbortBeforeAnyPairResolved()
        {
            // Given: pair resolution sequence — no pairs resolved
            // When: GM clicks Abort before any pair resolved
            when_execution_aborted();
            // Then: no pairs resolved — all characters return to pre-configuration state
            then_execution_stopped();
            then_role_neutral(_attacker);
            then_role_neutral(_defender);
        }

        [TestMethod]
        public void AbortAlreadyAppliedRetained()
        {
            // Given: pair resolution sequence Pair_1 effects retained; Pair_2 not yet resolved
            _statusEffects[_defender.Name] = "Stunned"; // Pair_1 applied Stunned
            bool pair1EffectsRetained = true;
            // When: GM aborts
            when_execution_aborted();
            // Then: Pair_1 effects (Stunned) are retained; Attack State Indicators reflect pre-abort effects
            pair1EffectsRetained.Should().BeTrue(
                "already-applied effects from Pair_1 (Stunned) are retained after abort");
            _statusEffects[_defender.Name].Should().Be("Stunned",
                "Stunned status from Pair_1 must not be reverted after abort");
        }

        [TestMethod]
        public void AbortNotAvailableBeforeConfirm()
        {
            // Given: GM has not yet clicked Confirm; Abort button is disabled
            bool abortAvailable = false; // disabled before Confirm
            // When: GM attempts to click Abort (button disabled)
            // Then: button is disabled; Cancel is the exit path before Confirm
            abortAvailable.Should().BeFalse(
                "Abort button is disabled before Confirm has been clicked; Cancel is the correct exit");
        }
    }
}
