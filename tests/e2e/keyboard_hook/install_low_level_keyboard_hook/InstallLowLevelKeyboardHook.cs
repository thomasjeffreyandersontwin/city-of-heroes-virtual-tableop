using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.KeyboardHook
{
    [TestClass]
    public class InstallLowLevelKeyboardHook : KeyboardHookHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void SuccessfulHookInstallationAtStartup()
        {
            // Given
            GivenApplicationRunning();

            // When
            WhenApplicationStartsAndHookInstallationRequested();

            // Then
            ThenKeyboardHookState("installed");
        }

        [TestMethod]
        public void HookInstalledEnablesAbilityDispatch()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();
            GivenActiveCharacter("Guard_Captain");
            GivenAbilityWithKey("Fire Strike", "F1");
            GivenAbilityEligible("Fire Strike");

            // When
            WhenGmPressesKey("F1");

            // Then
            ThenAbilityDispatchFires("Fire Strike");
        }

        [TestMethod]
        public void HookInstallationFails()
        {
            // Given
            GivenApplicationRunning();

            // When
            WhenApplicationStartsAndHookInstallationFails();

            // Then
            ThenKeyboardHookState("not installed");
            ThenDispatchDisabledForSession();
            ThenDirectPlayActionsStillFunctional();
        }

        [TestMethod]
        public void HookUninstalledOnApplicationShutdown()
        {
            // Given
            GivenApplicationRunning();
            GivenKeyboardHookInstalled();

            // When
            WhenApplicationShutsDown();

            // Then
            ThenKeyboardHookState("not installed");
        }
    }
}
