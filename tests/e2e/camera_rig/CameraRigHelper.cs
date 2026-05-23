using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CameraRig
{
    public class CameraRigHelper
    {
        protected AppDriver Driver;

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenGameBridgeInitialized()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenCameraRigState(string activeState)
        {
            Driver.SetCameraRigActiveState(activeState);
        }

        protected void GivenCameraFollowState(string followState, string followedCharacter)
        {
            Driver.SetCameraFollowState(followState, followedCharacter);
        }

        protected void GivenManeuverWithCameraModeState(string activeState)
        {
            Driver.SetManeuverWithCameraModeState(activeState);
        }

        protected void GivenMemoryInterfaceAttachedAndRegistered()
        {
            Driver.SetMemoryInterfaceState("attached");
            Driver.SetTargetRegistrationState("confirmed");
        }

        protected void GivenCharacterPosition(string x, string y, string z)
        {
            Driver.SetCharacterPositionInMemory(x, y, z);
        }

        protected void GivenSpawnedNpc(string characterName)
        {
            Driver.SetSpawnedNpcState(characterName, "present");
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenGmActivatesCameraRig()
        {
            Driver.InvokeActivateCameraRig();
        }

        protected void WhenGmDeactivatesCameraRig()
        {
            Driver.InvokeDeactivateCameraRig();
        }

        protected void WhenCameraRelativeCommandAttempted()
        {
            Driver.InvokeReadCameraPosition();
        }

        protected void WhenGmTriggersFollow(string characterName)
        {
            Driver.InvokeCameraFollow(characterName);
        }

        protected void WhenGmTriggersCameraDetach()
        {
            Driver.InvokeCameraDetach();
        }

        protected void WhenSpawnedNpcMoves(string characterName, string x, string y, string z)
        {
            Driver.SimulateNpcMovement(characterName, x, y, z);
        }

        protected void WhenFollowedNpcDespawned(string characterName)
        {
            Driver.SimulateNpcDespawn(characterName);
        }

        protected void WhenGmTriggersUnfollow()
        {
            Driver.InvokeCameraUnfollow();
        }

        protected void WhenGmActivatesManeuverWithCameraMode()
        {
            Driver.InvokeActivateManeuverWithCameraMode();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenCameraRigState(string expected)
        {
            string actual = Driver.GetCameraRigActiveState();
            Assert.AreEqual(expected, actual,
                string.Format("Camera Rig state: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenScriptDeployed(string scriptType)
        {
            Assert.IsTrue(Driver.WasCameraScriptDeployed(scriptType),
                string.Format("Camera '{0}' script should be deployed", scriptType));
        }

        protected void ThenCameraFollowState(string expectedFollowState, string expectedCharacter)
        {
            string state = Driver.GetCameraFollowState();
            string character = Driver.GetCameraFollowedCharacter();
            Assert.AreEqual(expectedFollowState, state,
                string.Format("Follow state: expected '{0}' got '{1}'", expectedFollowState, state));
            Assert.AreEqual(expectedCharacter, character,
                string.Format("Followed character: expected '{0}' got '{1}'", expectedCharacter, character));
        }

        protected void ThenManeuverModeState(string expected)
        {
            string actual = Driver.GetManeuverWithCameraModeState();
            Assert.AreEqual(expected, actual,
                string.Format("Maneuver mode: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenCameraTracksCharacter()
        {
            Assert.IsTrue(Driver.IsCameraTrackingCharacter(),
                "Camera should track character position");
        }

        protected void ThenCommandBlocked(string reason)
        {
            string msg = Driver.GetLastValidationMessage();
            Assert.IsNotNull(msg, "Expected blocked message");
            Assert.IsTrue(msg.Contains(reason) || msg.Contains("camera") || msg.Contains("rig"),
                string.Format("Message should indicate '{0}'", reason));
        }

        protected void ThenCommandProceeds()
        {
            Assert.IsTrue(Driver.DidLastCommandProceed(),
                "Command should proceed");
        }

        protected void ThenFollowRejected()
        {
            Assert.IsTrue(Driver.WasFollowRejected(), "Follow should be rejected");
        }

        protected void ThenNoError()
        {
            Assert.IsNull(Driver.GetLastGameBridgeError(), "No error expected");
        }

        protected void ThenCameraInFreeRoam()
        {
            Assert.IsTrue(Driver.IsCameraInFreeRoamMode(), "Camera should be in free-roam");
        }
    }
}
