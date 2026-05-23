using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    public class MemoryInterfaceHelper
    {
        protected AppDriver Driver;

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenApplicationStarted()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
        }

        protected void GivenMemoryInterfaceAttached()
        {
            Driver.SetMemoryInterfaceState("attached");
        }

        protected void GivenMemoryInterfaceUnattached()
        {
            Driver.SetMemoryInterfaceState("unattached");
        }

        protected void GivenGameProcessRunning()
        {
            Driver.SetGameProcessState("running");
        }

        protected void GivenGameProcessNotRunning()
        {
            Driver.SetGameProcessState("not running");
        }

        protected void GivenTargetRegistrationState(string state)
        {
            Driver.SetTargetRegistrationState(state);
        }

        protected void GivenCurrentTarget(string entityIdentifier)
        {
            Driver.SetCurrentTarget(entityIdentifier);
        }

        protected void GivenMemoryPointerState(string pointerName, string validationState)
        {
            Driver.SetMemoryPointerValidationState(pointerName, validationState);
        }

        protected void GivenCameraRigState(string activeState)
        {
            Driver.SetCameraRigActiveState(activeState);
        }

        protected void GivenCharacterPosition(string x, string y, string z)
        {
            Driver.SetCharacterPositionInMemory(x, y, z);
        }

        protected void GivenCharacterFacingVector(string x, string y, string z)
        {
            Driver.SetCharacterFacingVector(x, y, z);
        }

        protected void GivenSpawnedNpcJustCreated()
        {
            Driver.SimulateNpcSpawnCommand();
        }

        protected void GivenSessionActive()
        {
            Driver.SetSessionActive(true);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenMemoryInterfaceAttemptsToAttach()
        {
            Driver.InvokeMemoryInterfaceAttach();
        }

        protected void WhenGmSelectsCharacterInGame(string characterName)
        {
            Driver.SimulateGameTargetChange(characterName);
        }

        protected void WhenTargetChanges(string newTarget)
        {
            Driver.SimulateGameTargetChange(newTarget);
        }

        protected void WhenMemoryInterfacePollsForRegistration()
        {
            Driver.InvokePollForTargetRegistration();
        }

        protected void WhenPeriodicScanRuns()
        {
            Driver.InvokeMemoryPointerScan();
        }

        protected void WhenMovementRequestsPosition()
        {
            Driver.InvokeReadCharacterPosition();
        }

        protected void WhenMovementComputesDestination(string x, string y, string z)
        {
            Driver.InvokeWriteCharacterPosition(x, y, z);
        }

        protected void WhenMovementNeedsModelMatrix()
        {
            Driver.InvokeReadCharacterModelMatrix();
        }

        protected void WhenMovementComputesNewFacing()
        {
            Driver.InvokeWriteCharacterRotationMatrix();
        }

        protected void WhenMovementNeedsFacingVector()
        {
            Driver.InvokeReadCharacterFacingVector();
        }

        protected void WhenMovementDeterminesNewFacing(string facingX, string facingY, string facingZ)
        {
            Driver.InvokeWriteCharacterFacingDirection(facingX, facingY, facingZ);
        }

        protected void WhenCameraRelativeCommandTriggered()
        {
            Driver.InvokeReadCameraPosition();
        }

        protected void WhenGameProcessTerminates()
        {
            Driver.SimulateGameProcessTermination();
        }

        protected void WhenMovementTriggeredBeforeRegistration()
        {
            Driver.InvokeMoveBeforeRegistration();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenMemoryInterfaceState(string expectedState)
        {
            string actual = Driver.GetMemoryInterfaceAttachedState();
            Assert.AreEqual(expectedState, actual,
                string.Format("Memory Interface state: expected '{0}' got '{1}'", expectedState, actual));
        }

        protected void ThenGameProcessState(string expectedState)
        {
            string actual = Driver.GetGameProcessRunningState();
            Assert.AreEqual(expectedState, actual,
                string.Format("Game Process state: expected '{0}' got '{1}'", expectedState, actual));
        }

        protected void ThenCurrentTarget(string expected)
        {
            string actual = Driver.GetCurrentTargetIdentifier();
            Assert.AreEqual(expected, actual,
                string.Format("Current Target: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenTargetRegistrationState(string expected)
        {
            string actual = Driver.GetTargetRegistrationState();
            Assert.AreEqual(expected, actual,
                string.Format("Registration state: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenMemoryPointerState(string pointerName, string expected)
        {
            string actual = Driver.GetMemoryPointerValidationState(pointerName);
            Assert.AreEqual(expected, actual,
                string.Format("Pointer '{0}' state: expected '{1}' got '{2}'", pointerName, expected, actual));
        }

        protected void ThenStalePointerDetected(string pointerName)
        {
            Assert.IsTrue(Driver.WasStalePointerDetected(pointerName),
                string.Format("Stale pointer '{0}' should be detected", pointerName));
        }

        protected void ThenStalePointerNotDetected(string pointerName)
        {
            Assert.IsFalse(Driver.WasStalePointerDetected(pointerName),
                string.Format("Stale pointer '{0}' should NOT be detected", pointerName));
        }

        protected void ThenCharacterPositionReturned(string x, string y, string z)
        {
            var pos = Driver.GetLastReadCharacterPosition();
            Assert.AreEqual(x, pos.Item1, "X coordinate mismatch");
            Assert.AreEqual(y, pos.Item2, "Y coordinate mismatch");
            Assert.AreEqual(z, pos.Item3, "Z coordinate mismatch");
        }

        protected void ThenCharacterPositionWritten(string x, string y, string z)
        {
            Assert.IsTrue(Driver.WasCharacterPositionWritten(x, y, z),
                string.Format("Expected position write ({0},{1},{2})", x, y, z));
        }

        protected void ThenWriteBlocked()
        {
            Assert.IsTrue(Driver.WasWriteBlocked(), "Write should be blocked");
        }

        protected void ThenReadBlocked()
        {
            Assert.IsTrue(Driver.WasReadBlocked(), "Read should be blocked");
        }

        protected void ThenModelMatrixReturned()
        {
            Assert.IsTrue(Driver.WasModelMatrixReturned(), "Model matrix should be returned");
        }

        protected void ThenRotationMatrixWritten()
        {
            Assert.IsTrue(Driver.WasRotationMatrixWritten(), "Rotation matrix should be written");
        }

        protected void ThenNoRotationWriteIssued()
        {
            Assert.IsFalse(Driver.WasRotationMatrixWritten(), "No rotation write expected");
        }

        protected void ThenFacingVectorReturned()
        {
            Assert.IsTrue(Driver.WasFacingVectorReturned(), "Facing vector should be returned");
        }

        protected void ThenCameraPositionReturned(string x, string y, string z)
        {
            var pos = Driver.GetLastReadCameraPosition();
            Assert.AreEqual(x, pos.Item1, "Camera X mismatch");
            Assert.AreEqual(y, pos.Item2, "Camera Y mismatch");
            Assert.AreEqual(z, pos.Item3, "Camera Z mismatch");
        }

        protected void ThenMovementServicesAvailable()
        {
            Assert.IsTrue(Driver.AreMovementServicesAvailable(),
                "Movement services should be available");
        }

        protected void ThenMovementCommandsBlocked()
        {
            Assert.IsTrue(Driver.AreMovementCommandsBlocked(),
                "Movement commands should be blocked");
        }

        protected void ThenMovementExecutionNotified()
        {
            Assert.IsTrue(Driver.WasMovementExecutionNotified(),
                "Movement Execution should be notified");
        }
    }
}
