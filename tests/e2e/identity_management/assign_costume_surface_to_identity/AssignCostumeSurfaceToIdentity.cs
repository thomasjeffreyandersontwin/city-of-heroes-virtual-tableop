using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityManagement
{
    [TestClass]
    public class AssignCostumeSurfaceToIdentity : IdentityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidFilePathSurfaceSaved()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenCostumeIdentity("Knight_Armor", null);
            GivenCostumeFileAt(@"C:\Games\CoH\costumes\guard.costume");

            // When
            WhenGmAssignsCostumeSurface("Knight_Armor", @"C:\Games\CoH\costumes\guard.costume");

            // Then
            ThenCostumeSurfaceField("Knight_Armor", @"C:\Games\CoH\costumes\guard.costume");
        }

        [TestMethod]
        public void FileDoesNotExistValidationErrorShown()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenCostumeIdentity("Knight_Armor", null);

            // When
            WhenGmAssignsCostumeSurface("Knight_Armor", @"C:\Games\CoH\costumes\missing.costume");

            // Then
            ThenIdentityRejected("does not exist");
        }

        [TestMethod]
        public void SurfaceClearedActivationBlocked()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenCostumeIdentity("Knight_Armor", @"C:\Games\CoH\costumes\guard.costume");

            // When
            WhenGmAssignsCostumeSurface("Knight_Armor", "");

            // Then
            ThenCostumeSurfaceCleared("Knight_Armor");
        }

        [TestMethod]
        public void CostumeSurfaceNotAvailableOnModelIdentity()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenModelIdentity("Dragon_Model", "Skull_Lt_01");

            // When
            WhenGmAssignsCostumeSurface("Dragon_Model", @"C:\Games\CoH\costumes\guard.costume");

            // Then
            ThenIdentityRejected("not available");
        }
    }
}
