using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameStateQuery
{
    public class GameStateQueryHelper
    {
        protected AppDriver Driver;

        protected void GivenApplicationRunning()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
        }

        protected void GivenGameStateQueryAvailable()
        {
            Driver.SetGameStateQueryAvailability("available");
        }

        protected void GivenGameStateQueryUnavailable()
        {
            Driver.SetGameStateQueryAvailability("unavailable");
        }

        protected void GivenGameSessionEnded()
        {
            Driver.SetGameDonePreState("ended");
        }

        protected void GivenGameDoneEventReceived()
        {
            Driver.SetGameDonePreState("ended");
        }

        protected void GivenNewSessionStartedAfterGameDone()
        {
            Driver.SetGameDonePreState("reset");
        }

        protected void GivenGameBridgeInitialized()
        {
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenCommandChain(string[] commands)
        {
            Driver.SetCommandChain(commands);
        }

        protected void GivenRosterWithEntries()
        {
            Driver.EnsureRosterHasEntries();
        }

        protected void GivenMouseWorldCoordinates(string coords)
        {
            Driver.SetMouseWorldCoordinates(coords);
        }

        protected void WhenMouseHoversOverEntity(string scenario)
        {
            Driver.SimulateMouseHoverOnEntity(scenario);
        }

        protected void WhenApplicationRequestsMousePosition()
        {
            Driver.InvokeQueryMouseXyzPosition();
        }

        protected void WhenApplicationPollsGameDoneState()
        {
            Driver.InvokePollGameDoneState();
        }

        protected void WhenApplicationAssemblesAndDeliversChain()
        {
            Driver.InvokeDeliverCommandChain();
        }

        protected void WhenGmClosesApplication()
        {
            Driver.InvokeApplicationShutdown();
        }

        protected void WhenGmTriggersLoadMap(string mapId)
        {
            Driver.InvokeLoadMapCommand(mapId);
        }

        protected void ThenHoveredNpcInfo(string observedState, string npcName)
        {
            string state = Driver.GetHoveredNpcObservedState();
            Assert.AreEqual(observedState, state,
                string.Format("Hovered NPC observed state: expected '{0}' got '{1}'", observedState, state));
            if (observedState == "present")
            {
                string name = Driver.GetHoveredNpcName();
                Assert.AreEqual(npcName, name,
                    string.Format("Hovered NPC name: expected '{0}' got '{1}'", npcName, name));
            }
        }

        protected void ThenMouseXyzPosition(string coordinates)
        {
            string actual = Driver.GetMouseWorldSpaceCoordinates();
            Assert.AreEqual(coordinates, actual,
                string.Format("Mouse position: expected '{0}' got '{1}'", coordinates, actual));
        }

        protected void ThenGameDoneState(string sessionEnded)
        {
            string actual = Driver.GetGameDoneSessionEnded();
            Assert.AreEqual(sessionEnded, actual,
                string.Format("Game done state: expected '{0}' got '{1}'", sessionEnded, actual));
        }

        protected void ThenOversizedChainDetected(string detectedState)
        {
            string actual = Driver.GetOversizedChainDetectedState();
            Assert.AreEqual(detectedState, actual,
                string.Format("Oversized chain: expected '{0}' got '{1}'", detectedState, actual));
        }

        protected void ThenShutdownCompleted()
        {
            Assert.IsTrue(Driver.WasShutdownCompleted(), "Shutdown should complete");
        }

        protected void ThenNoError()
        {
            Assert.IsNull(Driver.GetLastGameBridgeError(), "No error expected");
        }

        protected void ThenLoadMapSucceeded()
        {
            Assert.IsTrue(Driver.WasLoadMapSuccessful(), "Load map should succeed");
        }

        protected void ThenLoadMapBlocked()
        {
            Assert.IsTrue(Driver.WasLoadMapBlocked(), "Load map should be blocked");
        }
    }
}
