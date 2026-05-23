using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatGeometry
{
    [TestClass]
    public class DetectKnockbackObstructionViaCollisionRay : CombatGeometryHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenCombatExecutionApplyingKnockback();
        }

        [TestMethod]
        public void ClearPathFullKnockback()
        {
            GivenCollisionRay("(100, 0, -200)", "(1, 0, 0)", "5");
            WhenCollisionDetectionProcesses();
            ThenObstructionPoint("none (full distance)");
        }

        [TestMethod]
        public void ObstructionDetectedClipped()
        {
            GivenCollisionRay("(100, 0, -200)", "(1, 0, 0)", "5");
            GivenObstructionPresent();
            WhenCollisionDetectionProcesses();
            ThenObstructionPoint("(103, 0, -200)");
        }

        [TestMethod]
        public void GameClientNotRunningSafeDefault()
        {
            GivenCollisionRay("(100, 0, -200)", "(1, 0, 0)", "5");
            GivenDllCapability("unavailable");
            WhenCollisionDetectionProcesses();
            ThenObstructionPoint("none (full distance)");
            ThenWarningLogged();
        }
    }
}
