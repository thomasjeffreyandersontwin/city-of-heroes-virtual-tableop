using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatGeometry
{
    [TestClass]
    public class CalculateLineOfSightForRangedAttack : CombatGeometryHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenRangedAttackConfirmed();
        }

        [TestMethod]
        public void ClearToDefenderIncluded()
        {
            GivenDllCapability("available");
            WhenCollisionDetectionProcesses();
            ThenLineOfSight("Villain_Boss_03", "clear");
        }

        [TestMethod]
        public void BlockedToDefenderExcluded()
        {
            GivenDllCapability("available");
            GivenBlockedLos("Villain_Boss_03");
            WhenCollisionDetectionProcesses();
            ThenLineOfSight("Villain_Boss_03", "blocked");
        }

        [TestMethod]
        public void AllBlockedConfirmBlocked()
        {
            GivenDllCapability("available");
            GivenBlockedLos("Villain_Boss_03");
            WhenCollisionDetectionProcesses();
            ThenLineOfSight("Villain_Boss_03", "blocked");
        }

        [TestMethod]
        public void GameClientUnavailableSafeDefault()
        {
            GivenDllCapability("unavailable");
            WhenCollisionDetectionProcesses();
            ThenLineOfSight("Villain_Boss_03", "clear");
            ThenWarningLogged();
        }
    }
}
