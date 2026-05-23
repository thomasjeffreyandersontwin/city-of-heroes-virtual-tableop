using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.ContextMenu
{
    public class ContextMenuHelper
    {
        protected AppDriver Driver;

        protected void GivenDesktopOverlayWithCharacters()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.EnsureDesktopOverlayRendered();
        }

        protected void GivenSpawnedState(string characterName, string presenceInWorld)
        {
            Driver.SetRosterEntrySpawnedState(characterName, presenceInWorld);
        }

        protected void GivenMousePosition(string focusValidity, string coordinates)
        {
            Driver.SetMouseXyzFocusValidity(focusValidity);
            Driver.SetMouseWorldCoordinates(coordinates);
        }

        protected void GivenGangMode(string state)
        {
            Driver.SetGangModeState(state, new string[0]);
        }

        protected void GivenTargetCharacter(string characterName)
        {
            Driver.SetContextMenuTarget(characterName);
        }

        protected void GivenCameraRigState(string state)
        {
            Driver.SetCameraRigActiveState(state);
        }

        protected void GivenCharacterMemoryPosition(string characterName, string position)
        {
            Driver.SetCharacterMemoryPosition(characterName, position);
        }

        protected void GivenSpawnWillFail()
        {
            Driver.SetSpawnWillFail(true);
        }

        protected void GivenCollisionAtDestination()
        {
            Driver.SetCollisionObstructionPresent(true);
        }

        protected void GivenCollisionOnPath()
        {
            Driver.SetCollisionObstructionPresent(true);
        }

        protected void GivenLibrarySaveWillFail()
        {
            Driver.SetLibrarySaveWillFail(true);
        }

        protected void GivenRosterEntryExists(string characterName)
        {
            Driver.AddRosterEntry(characterName, "false", "none");
        }

        protected void GivenManeuverModeActive()
        {
            Driver.SetManeuverWithCameraModeState("active");
        }

        protected void WhenGmSelectsSpawn(string characterName)
        {
            Driver.InvokeContextMenuAction("Spawn", characterName);
        }

        protected void WhenGmSelectsPlaceAtLocation(string characterName)
        {
            Driver.InvokeContextMenuAction("PlaceAtLocation", characterName);
        }

        protected void WhenGmSelectsSavePosition(string characterName)
        {
            Driver.InvokeContextMenuAction("SavePosition", characterName);
        }

        protected void WhenGmSelectsMoveCameraToTarget(string characterName)
        {
            Driver.InvokeContextMenuAction("MoveCameraToTarget", characterName);
        }

        protected void WhenGmSelectsMoveTargetToCamera(string characterName)
        {
            Driver.InvokeContextMenuAction("MoveTargetToCamera", characterName);
        }

        protected void WhenGmSelectsResetOrientation(string characterName)
        {
            Driver.InvokeContextMenuAction("ResetOrientation", characterName);
        }

        protected void WhenGmSelectsManeuverWithCamera(string characterName)
        {
            Driver.InvokeContextMenuAction("ManeuverWithCamera", characterName);
        }

        protected void WhenGmSelectsActivateOption(string characterName)
        {
            Driver.InvokeContextMenuAction("Activate", characterName);
        }

        protected void WhenGmSelectsCloneLink(string characterName)
        {
            Driver.InvokeContextMenuAction("CloneLink", characterName);
        }

        protected void ThenSpawnedState(string characterName, string expected)
        {
            string actual = Driver.GetRosterEntrySpawnedState(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Spawned state for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenActionAvailable(string action)
        {
            Assert.IsTrue(Driver.IsContextMenuActionAvailable(action),
                string.Format("Action '{0}' should be available", action));
        }

        protected void ThenActionNotAvailable(string action)
        {
            Assert.IsFalse(Driver.IsContextMenuActionAvailable(action),
                string.Format("Action '{0}' should not be available", action));
        }

        protected void ThenOverlayPosition(string characterName, string expected)
        {
            string actual = Driver.GetCharacterOverlayPosition(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Position for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenSavedPosition(string characterName, string expected)
        {
            string actual = Driver.GetSavedCharacterPosition(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Saved position for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenActiveCharacter(string expected)
        {
            string actual = Driver.GetActiveCharacterDesignation();
            Assert.AreEqual(expected, actual,
                string.Format("Active character: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenManeuverModeState(string expected)
        {
            string actual = Driver.GetManeuverWithCameraModeState();
            Assert.AreEqual(expected, actual,
                string.Format("Maneuver mode: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenRosterEntryCreated(string characterName)
        {
            Assert.IsTrue(Driver.RosterEntryExists(characterName),
                string.Format("Roster entry '{0}' should be created", characterName));
        }

        protected void ThenRosterEntryNotCreated(string characterName)
        {
            Assert.IsFalse(Driver.RosterEntryExists(characterName),
                string.Format("Roster entry '{0}' should not be created", characterName));
        }

        protected void ThenFeedbackShown()
        {
            Assert.IsNotNull(Driver.GetLastValidationMessage(), "Expected feedback message");
        }

        protected void ThenCameraMovedToTarget()
        {
            Assert.IsTrue(Driver.WasCameraMovedToTarget(), "Camera should move to target");
        }
    }
}
