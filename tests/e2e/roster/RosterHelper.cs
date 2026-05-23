using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.Roster
{
    public class RosterHelper
    {
        protected AppDriver Driver;

        protected void GivenSessionActive()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetSessionActive(true);
        }

        protected void GivenGameBridgeInitialized()
        {
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenRosterHasEntries()
        {
            Driver.EnsureRosterHasEntries();
        }

        protected void GivenRosterEntry(string characterName, string spawnedState, string gangIndicator)
        {
            Driver.AddRosterEntry(characterName, spawnedState, gangIndicator);
        }

        protected void GivenActiveCharacter(string characterName)
        {
            Driver.SetActiveCharacterDesignation(characterName);
        }

        protected void GivenGangMode(string collectiveState, string[] members)
        {
            Driver.SetGangModeState(collectiveState, members);
        }

        protected void GivenCrowdOnRoster(string crowdName, string[] members)
        {
            Driver.SetCrowdOnRoster(crowdName, members);
        }

        protected void GivenSpawnWillFail()
        {
            Driver.SetSpawnWillFail(true);
        }

        protected void GivenDespawnWillFail()
        {
            Driver.SetDespawnWillFail(true);
        }

        protected void WhenGmAddsCharacterToRoster(string characterName)
        {
            Driver.InvokeAddCharacterToRoster(characterName);
        }

        protected void WhenGmAddsCrowdToRoster(string crowdName)
        {
            Driver.InvokeAddCrowdToRoster(crowdName);
        }

        protected void WhenGmSpawnsFromRoster(string characterName)
        {
            Driver.InvokeSpawnFromRoster(characterName);
        }

        protected void WhenGmRemovesFromRoster(string characterName)
        {
            Driver.InvokeRemoveFromRoster(characterName);
        }

        protected void WhenGmClearsFromDesktop(string characterName)
        {
            Driver.InvokeClearFromDesktop(characterName);
        }

        protected void WhenGmActivatesEntry(string characterName)
        {
            Driver.InvokeActivateRosterEntry(characterName);
        }

        protected void WhenGmDeactivatesEntry(string characterName)
        {
            Driver.InvokeDeactivateRosterEntry(characterName);
        }

        protected void WhenGmActivatesGang(string crowdName, string leader)
        {
            Driver.InvokeActivateGang(crowdName, leader);
        }

        protected void WhenGmDeactivatesGang()
        {
            Driver.InvokeDeactivateGang();
        }

        protected void ThenRosterEntryExists(string characterName, string spawnedState)
        {
            Assert.IsTrue(Driver.RosterEntryExists(characterName),
                string.Format("Roster entry '{0}' should exist", characterName));
            string actual = Driver.GetRosterEntrySpawnedState(characterName);
            Assert.AreEqual(spawnedState, actual,
                string.Format("Spawned state for '{0}': expected '{1}' got '{2}'", characterName, spawnedState, actual));
        }

        protected void ThenRosterEntryRejected()
        {
            string msg = Driver.GetLastValidationMessage();
            Assert.IsNotNull(msg, "Expected rejection message");
        }

        protected void ThenRosterEntryNotExists(string characterName)
        {
            Assert.IsFalse(Driver.RosterEntryExists(characterName),
                string.Format("Roster entry '{0}' should not exist", characterName));
        }

        protected void ThenSpawnedState(string characterName, string expected)
        {
            string actual = Driver.GetRosterEntrySpawnedState(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Spawned for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenActiveCharacter(string expected)
        {
            string actual = Driver.GetActiveCharacterDesignation();
            Assert.AreEqual(expected, actual,
                string.Format("Active character: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenNoActiveCharacter()
        {
            string actual = Driver.GetActiveCharacterDesignation();
            Assert.IsTrue(string.IsNullOrEmpty(actual) || actual == "none",
                "No active character expected");
        }

        protected void ThenGangModeState(string expected)
        {
            string actual = Driver.GetGangModeCollectiveState();
            Assert.AreEqual(expected, actual,
                string.Format("Gang mode: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenGangLeader(string expected)
        {
            string actual = Driver.GetGangLeaderDesignation();
            Assert.AreEqual(expected, actual,
                string.Format("Gang leader: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenActiveTurnIndicator(string characterName, string expected)
        {
            string actual = Driver.GetRosterEntryActiveTurnIndicator(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Active indicator for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }

        protected void ThenGangIndicator(string characterName, string expected)
        {
            string actual = Driver.GetRosterEntryGangIndicator(characterName);
            Assert.AreEqual(expected, actual,
                string.Format("Gang indicator for '{0}': expected '{1}' got '{2}'", characterName, expected, actual));
        }
    }
}
