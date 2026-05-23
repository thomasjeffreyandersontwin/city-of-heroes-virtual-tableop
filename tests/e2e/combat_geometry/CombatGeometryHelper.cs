using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatGeometry
{
    public class CombatGeometryHelper
    {
        protected AppDriver Driver;

        protected void GivenCombatExecutionApplyingKnockback()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
            Driver.BeginCombatExecution();
        }

        protected void GivenRangedAttackConfirmed()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
            Driver.SetRangedAttackConfirmed(true);
        }

        protected void GivenApplicationNeedsCollisionData()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenCollisionRay(string origin, string direction, string maxDistance)
        {
            Driver.SetCollisionRayParameters(origin, direction, maxDistance);
        }

        protected void GivenDllCapability(string capability)
        {
            Driver.SetGameCollisionDllCapability(capability);
        }

        protected void GivenObstructionPresent()
        {
            Driver.SetCollisionObstructionPresent(true);
        }

        protected void GivenClearPath()
        {
            Driver.SetCollisionObstructionPresent(false);
        }

        protected void WhenCollisionDetectionProcesses()
        {
            Driver.InvokeCollisionDetection();
        }

        protected void WhenCollisionRayQueryIssued()
        {
            Driver.InvokeCollisionRayQuery();
        }

        protected void ThenObstructionPoint(string expected)
        {
            string actual = Driver.GetKnockbackObstructionPoint();
            Assert.AreEqual(expected, actual,
                string.Format("Obstruction point: expected '{0}' got '{1}'", expected, actual));
        }

        protected void GivenBlockedLos(string defender)
        {
            Driver.SetLosBlocked(defender, true);
        }

        protected void ThenLineOfSight(string defender, string expected)
        {
            string actual = Driver.GetLineOfSightState(defender);
            Assert.AreEqual(expected, actual,
                string.Format("LOS for '{0}': expected '{1}' got '{2}'", defender, expected, actual));
        }

        protected void ThenCollisionResult(string expected)
        {
            string actual = Driver.GetCollisionDetectionResult();
            Assert.AreEqual(expected, actual,
                string.Format("Collision result: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenWarningLogged()
        {
            Assert.IsNotNull(Driver.GetLastWarning(), "Expected a warning to be logged");
        }
    }
}
