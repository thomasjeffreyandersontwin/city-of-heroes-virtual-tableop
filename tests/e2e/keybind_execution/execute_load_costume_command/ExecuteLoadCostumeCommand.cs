using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.KeybindExecution
{
    [TestClass]
    public class ExecuteLoadCostumeCommand : KeybindExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidCostumeNpcTargetedCostumeApplied()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenCostumeFileAt(@"C:\Games\CoH\costumes\guard.costume");

            // When
            WhenGameBridgeExecutesLoadCostumeCommand(@"C:\Games\CoH\costumes\guard.costume");

            // Then
            ThenSpawnedNpcHasPresence("Guard_Captain", "present");
            ThenCostumeAppliedToNpc("Guard_Captain");
        }

        [TestMethod]
        public void CostumeFileDoesNotExistNpcUnchanged()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");

            // When
            WhenGameBridgeExecutesLoadCostumeCommand(@"C:\Games\CoH\costumes\missing.costume");

            // Then
            ThenSpawnedNpcHasPresence("Guard_Captain", "present");
            ThenGameBridgeReportsError("missing file");
        }

        [TestMethod]
        public void GhostCostumeOntoGhostNpcLoadsViaSamePipeline()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain_Ghost", "present");
            GivenCostumeFileAt(@"C:\Games\CoH\costumes\guard_ghost.costume");

            // When
            WhenGameBridgeExecutesLoadCostumeCommand(@"C:\Games\CoH\costumes\guard_ghost.costume");

            // Then
            ThenSpawnedNpcHasPresence("Guard_Captain_Ghost", "present");
            ThenCostumeAppliedToNpc("Guard_Captain_Ghost");
        }

        [TestMethod]
        public void NoNpcTargetedWhenLoadCostumeIssuedWarningLogged()
        {
            // Given
            GivenGameBridgeReady();

            // When
            WhenGameBridgeExecutesLoadCostumeCommand(@"C:\Games\CoH\costumes\guard.costume");

            // Then
            ThenGameBridgeReportsError("ambiguous");
        }
    }
}
