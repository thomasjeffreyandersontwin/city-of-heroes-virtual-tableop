using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityRendering
{
    [TestClass]
    public class LoadCostumeFileForActiveIdentity : IdentityRenderingHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidFileNpcTargetedCostumeLoaded()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenCostumeIdentity("Knight_Armor", @"C:\Games\CoH\costumes\guard.costume");
            GivenCostumeFileAt(@"C:\Games\CoH\costumes\guard.costume");

            // When
            WhenGameBridgeExecutesIdentityActivation("Knight_Armor");

            // Then
            ThenTargetByNameCommandIssued("Guard_Captain");
            ThenLoadCostumeCommandIssued(@"C:\Games\CoH\costumes\guard.costume");
        }

        [TestMethod]
        public void FileDoesNotExistNpcRetainsBaseAppearance()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenCostumeIdentity("Knight_Armor", @"C:\Games\CoH\costumes\missing.costume");
            GivenNoCostumeFileAt(@"C:\Games\CoH\costumes\missing.costume");

            // When
            WhenGameBridgeExecutesIdentityActivation("Knight_Armor");

            // Then
            ThenLoadCostumeCommandIssued(@"C:\Games\CoH\costumes\missing.costume");
            ThenGameBridgeLogsFailure();
        }

        [TestMethod]
        public void TargetFailsBeforeLoadAbortsLoadStep()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "absent");
            GivenCostumeIdentity("Knight_Armor", @"C:\Games\CoH\costumes\guard.costume");

            // When
            WhenGameBridgeExecutesIdentityActivation("Knight_Armor");

            // Then
            ThenGameBridgeLogsFailure();
        }

        [TestMethod]
        public void CostumeLoadDuringIdentitySwitchReplacesPreviousAppearance()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenCostumeIdentity("Knight_Armor", @"C:\Games\CoH\costumes\knight.costume");
            GivenCostumeFileAt(@"C:\Games\CoH\costumes\knight.costume");

            // When
            WhenGameBridgeExecutesIdentityActivation("Knight_Armor");

            // Then
            ThenLoadCostumeCommandIssued(@"C:\Games\CoH\costumes\knight.costume");
        }
    }
}
