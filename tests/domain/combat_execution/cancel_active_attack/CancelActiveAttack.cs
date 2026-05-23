using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class CancelActiveAttack : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration panel is open
        }

        [TestMethod]
        public void CancelBeforeConfirm()
        {
            // When: GM clicks Cancel in the Attack Configuration panel (before Confirm)
            when_execution_cancelled();
            // Then: Combat State current role neutral; configuration linkage cleared; panel closed; locks released
            then_panel_closed();
            then_role_neutral(_attacker);
            then_role_neutral(_defender);
        }

        [TestMethod]
        public void CancelWithPartialParameters()
        {
            // Given: Attack Configuration has partial parameters entered (no targets confirmed)
            // When: GM clicks Cancel
            when_execution_cancelled();
            // Then: panel closed; Combat State neutral; Non-Attack Ability Lock released; all unsaved params discarded
            then_panel_closed();
            then_role_neutral(_attacker);
        }

        [TestMethod]
        public void CancelViaKeyboardShortcut()
        {
            // When: GM uses keyboard shortcut to cancel Attack Configuration
            when_execution_cancelled();
            // Then: same outcome — panel closed; Combat State neutral; configuration linkage cleared
            then_panel_closed();
            then_role_neutral(_attacker);
            then_role_neutral(_defender);
        }

        [TestMethod]
        public void CloseWithoutCancelOrConfirm()
        {
            // When: GM closes the panel without using Cancel or Confirm
            when_execution_cancelled();
            // Then: panel closed; Combat State neutral; configuration linkage cleared — same as Cancel
            then_panel_closed();
            then_role_neutral(_attacker);
            then_role_neutral(_defender);
        }
    }
}
