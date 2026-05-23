using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.DesktopOverlay
{
    public class DesktopOverlayHelper
    {
        protected AppDriver Driver;

        protected void GivenDesktopOverlayWithCharacters()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.EnsureDesktopOverlayRendered();
        }

        protected void GivenCharacterOverlay(string characterName, string selectionHighlight)
        {
            Driver.SetCharacterOverlaySelection(characterName, selectionHighlight);
        }

        protected void GivenSpawnedState(string characterName, string presenceInWorld)
        {
            Driver.SetRosterEntrySpawnedState(characterName, presenceInWorld);
        }

        protected void GivenMultiSelect(string[] overlays)
        {
            Driver.SetMultiSelectOverlays(overlays);
        }

        protected void GivenGangMode(string state)
        {
            Driver.SetGangModeState(state, new string[0]);
        }

        protected void GivenMemoryInterfaceMonitoring()
        {
            Driver.SetMemoryInterfaceState("attached");
        }

        protected void WhenGmSingleClicks(string target)
        {
            Driver.SimulateSingleClick(target);
        }

        protected void WhenGmShiftClicks(string target)
        {
            Driver.SimulateShiftClick(target);
        }

        protected void WhenGmDragsOverlay(string characterName, string destX, string destY, string destZ)
        {
            Driver.SimulateDragOverlay(characterName, destX, destY, destZ);
        }

        protected void WhenGmDoubleClicks(string characterName)
        {
            Driver.SimulateDoubleClick(characterName);
        }

        protected void WhenGameTargetChanges(string characterName)
        {
            Driver.SimulateGameTargetChange(characterName);
        }

        protected void WhenLifecycleEvent(string eventType, string characterName)
        {
            Driver.SimulateLifecycleEvent(eventType, characterName);
        }

        protected void ThenSelectionHighlight(string characterName, string expected)
        {
            string actual = Driver.GetCharacterOverlaySelection(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Selection for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenMultiSelectContains(string[] expected)
        {
            foreach (string name in expected)
            {
                Assert.IsTrue(Driver.IsInMultiSelect(name),
                    string.Format("'{0}' should be in multi-select", name));
            }
        }

        protected void ThenOverlayPosition(string characterName, string expected)
        {
            string actual = Driver.GetCharacterOverlayPosition(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Position for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenActiveCharacter(string expected)
        {
            string actual = Driver.GetActiveCharacterDesignation();
            Assert.AreEqual(expected, actual,
                string.Format("Active character: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenSpawnedState(string characterName, string expected)
        {
            string actual = Driver.GetRosterEntrySpawnedState(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Spawned state for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenDragUnavailable(string characterName)
        {
            Assert.IsFalse(Driver.IsDragAvailableForOverlay(characterName),
                string.Format("Drag should be unavailable for '{0}'", characterName));
        }

        protected void ThenNoHighlights()
        {
            Assert.IsTrue(Driver.AreAllSelectionsCleared(), "All selections should be cleared");
        }
    }
}
