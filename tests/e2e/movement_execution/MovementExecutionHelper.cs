using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MovementExecution
{
    public class MovementExecutionHelper
    {
        protected AppDriver Driver;

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenMemoryInterfaceAttachedAndRegistered()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetMemoryInterfaceState("attached");
            Driver.SetTargetRegistrationState("confirmed");
        }

        protected void GivenMemoryInterfaceAttached()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetMemoryInterfaceState("attached");
        }

        protected void GivenTargetRegistrationState(string state)
        {
            Driver.SetTargetRegistrationState(state);
        }

        protected void GivenCharacterMovementActive(string movementName, string movementType,
            string distanceLimit)
        {
            Driver.AddCharacterMovement(movementName, movementType);
            Driver.SetMovementDistanceLimit(movementName, distanceLimit);
            Driver.SetActiveMovement(movementName);
        }

        protected void GivenCharacterMovement(string movementName, string movementType,
            string distanceLimit)
        {
            Driver.AddCharacterMovement(movementName, movementType);
            Driver.SetMovementDistanceLimit(movementName, distanceLimit);
        }

        protected void GivenCameraRigState(string activeState)
        {
            Driver.SetCameraRigActiveState(activeState);
        }

        protected void GivenMemoryPointerState(string pointerName, string validationState)
        {
            Driver.SetMemoryPointerValidationState(pointerName, validationState);
        }

        protected void GivenCharacterFacingVector(string x, string y, string z)
        {
            Driver.SetCharacterFacingVector(x, y, z);
        }

        protected void GivenMovementExecutionInProgress()
        {
            Driver.SetMovementExecutionInProgress(true);
        }

        protected void GivenFloorCollisionWillOccur()
        {
            Driver.SetFloorCollisionSimulated(true);
        }

        protected void GivenWallCollisionWillOccur()
        {
            Driver.SetWallCollisionSimulated(true);
        }

        protected void GivenBothCollisionsWillOccur()
        {
            Driver.SetBothCollisionsSimulated(true);
        }

        protected void GivenSpawnedNpcPresent(string characterName)
        {
            Driver.SetSpawnedNpcState(characterName, "present");
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenMovementExecutionComputesDestination()
        {
            Driver.InvokeMovementExecution();
        }

        protected void WhenGmTriggersMoveToLocation()
        {
            Driver.InvokeMoveToLocation();
        }

        protected void WhenGmTriggersMoveToCameraPosition()
        {
            Driver.InvokeMoveToCameraPosition();
        }

        protected void WhenGmTriggersTeleportToCamera()
        {
            Driver.InvokeTeleportToCamera();
        }

        protected void WhenMovementBegins(string movementType)
        {
            Driver.InvokeMovementAnimationStart(movementType);
        }

        protected void WhenMovementHalts()
        {
            Driver.InvokeMovementAnimationStop();
        }

        protected void WhenMovementActivationBegins()
        {
            Driver.InvokeMovementActivation();
        }

        protected void WhenStepsIssued(int stepCount)
        {
            Driver.SimulateMovementSteps(stepCount);
        }

        protected void WhenDistanceLimitReached()
        {
            Driver.SimulateDistanceLimitReached();
        }

        protected void WhenMovementStepComputed()
        {
            Driver.InvokeComputeNextMovementStep();
        }

        protected void WhenGmTriggersTurnToTarget()
        {
            Driver.InvokeTurnToTarget();
        }

        protected void WhenGmTriggersResetOrientation()
        {
            Driver.InvokeResetCharacterOrientation();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenMoveNpcCommandIssued(string targetName, string destX, string destY, string destZ)
        {
            Assert.IsTrue(Driver.WasMoveNpcCommandIssued(targetName, destX, destY, destZ),
                string.Format("Move NPC command for '{0}' to ({1},{2},{3}) expected",
                    targetName, destX, destY, destZ));
        }

        protected void ThenCommandHeld()
        {
            Assert.IsTrue(Driver.WasMoveCommandHeld(), "Command should be held");
        }

        protected void ThenCommandNoOp()
        {
            Assert.IsTrue(Driver.WasMoveCommandNoOp(), "Command should be no-op");
        }

        protected void ThenCumulativeDistance(int expected)
        {
            int actual = Driver.GetCumulativeDistanceTraveled();
            Assert.AreEqual(expected, actual,
                string.Format("Distance: expected {0} got {1}", expected, actual));
        }

        protected void ThenMovementHalted()
        {
            Assert.IsTrue(Driver.WasMovementHalted(), "Movement should have halted");
        }

        protected void ThenMovementAnimationCycle(string expected)
        {
            string actual = Driver.GetActiveAnimationCycle();
            Assert.AreEqual(expected, actual,
                string.Format("Animation cycle: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenFloorCollisionDetected()
        {
            Assert.IsTrue(Driver.WasFloorCollisionDetected(), "Floor collision expected");
        }

        protected void ThenNoFloorCollisionDetected()
        {
            Assert.IsFalse(Driver.WasFloorCollisionDetected(),
                "Floor collision should not be detected for levitating movement type");
        }

        protected void ThenWallCollisionDetected()
        {
            Assert.IsTrue(Driver.WasWallCollisionDetected(), "Wall collision expected");
        }

        protected void ThenNoCollisionDetected()
        {
            Assert.IsFalse(Driver.WasFloorCollisionDetected(), "No floor collision expected");
            Assert.IsFalse(Driver.WasWallCollisionDetected(), "No wall collision expected");
        }

        protected void ThenRotationMatrixWritten(string matrixLabel)
        {
            Assert.IsTrue(Driver.WasRotationMatrixWritten(),
                string.Format("Expected rotation write: {0}", matrixLabel));
        }

        protected void ThenNoRotationWriteIssued()
        {
            Assert.IsFalse(Driver.WasRotationMatrixWritten(), "No rotation write expected");
        }

        protected void ThenCharacterFacesDefaultOrientation()
        {
            Assert.IsTrue(Driver.IsCharacterInDefaultOrientation(),
                "Character should face default orientation");
        }

        protected void ThenTeleportCompleted()
        {
            Assert.IsTrue(Driver.WasTeleportCompleted(), "Teleport should complete");
        }

        protected void ThenTeleportBlocked()
        {
            Assert.IsTrue(Driver.WasTeleportBlocked(), "Teleport should be blocked");
        }

        protected void ThenNoAnimationPlayed()
        {
            Assert.IsTrue(Driver.WasNoMovementAnimationPlayed(),
                "No movement animation should play during teleport");
        }

        protected void ThenMovementProceeds()
        {
            Assert.IsTrue(Driver.IsMovementInProgress(), "Movement should proceed");
        }

        protected void ThenDistanceLimitEnforced(string movementName, int limit)
        {
            Assert.IsTrue(Driver.WasDistanceLimitEnforced(movementName, limit),
                string.Format("Distance limit {0} for '{1}' should be enforced", limit, movementName));
        }
    }
}
