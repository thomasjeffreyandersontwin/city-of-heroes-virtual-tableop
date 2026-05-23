using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatGeometry
{
    [TestClass]
    public class QueryGameCollisionDetectionViaHookCostumeDll : CombatGeometryHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationNeedsCollisionData();
        }

        [TestMethod]
        public void DllAvailableObstructionReturned()
        {
            GivenDllCapability("available");
            GivenObstructionPresent();
            GivenCollisionRay("(100, 0, -200)", "(1, 0, 0)", "5");
            WhenCollisionRayQueryIssued();
            ThenCollisionResult("obstruction");
        }

        [TestMethod]
        public void DllAvailableClearPath()
        {
            GivenDllCapability("available");
            GivenClearPath();
            GivenCollisionRay("(100, 0, -200)", "(1, 0, 0)", "5");
            WhenCollisionRayQueryIssued();
            ThenCollisionResult("clear");
        }

        [TestMethod]
        public void GameBridgeNotInitializedDefault()
        {
            GivenDllCapability("unavailable");
            GivenCollisionRay("(100, 0, -200)", "(1, 0, 0)", "5");
            WhenCollisionRayQueryIssued();
            ThenCollisionResult("clear");
            ThenWarningLogged();
        }

        [TestMethod]
        public void ZeroMaxDistanceImmediateClear()
        {
            GivenDllCapability("available");
            GivenCollisionRay("(100, 0, -200)", "(1, 0, 0)", "0");
            WhenCollisionRayQueryIssued();
            ThenCollisionResult("clear");
        }

        [TestMethod]
        public void DllErrorResponseFallback()
        {
            GivenDllCapability("error");
            GivenCollisionRay("(100, 0, -200)", "(1, 0, 0)", "5");
            WhenCollisionRayQueryIssued();
            ThenCollisionResult("clear");
            ThenWarningLogged();
        }
    }
}
