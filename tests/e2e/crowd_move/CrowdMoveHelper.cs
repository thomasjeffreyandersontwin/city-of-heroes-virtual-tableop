using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CrowdMove
{
    public class CrowdMoveHelper
    {
        protected AppDriver Driver;

        protected void GivenRosterWithSpawnedCrowdMembers(string[] members)
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetSessionActive(true);
            Driver.SetGameBridgeState("ready");
            foreach (string member in members)
                Driver.AddRosterEntry(member, "true", "hidden");
        }

        protected void GivenCrowdMoveStrategy(string strategy, string[] members)
        {
            Driver.SetCrowdMovePositioningStrategy(strategy, members);
        }

        protected void GivenGroupFormationOffsets(string offsets)
        {
            Driver.SetGroupFormationOffsets(offsets);
        }

        protected void GivenGangModeActive(string leader, string[] members)
        {
            Driver.SetGangModeState("active", members);
            Driver.SetGangLeaderDesignation(leader);
        }

        protected void GivenGangModeInactive()
        {
            Driver.SetGangModeState("inactive", new string[0]);
        }

        protected void GivenGangLeaderFacing(string facingVector)
        {
            Driver.SetGangLeaderFacingVector(facingVector);
        }

        protected void GivenMemberAtDestination(string characterName)
        {
            Driver.SetMemberAtDestination(characterName, true);
        }

        protected void GivenPartialObstruction()
        {
            Driver.SetCollisionObstructionPresent(true);
        }

        protected void WhenGmDesignatesDestination(string x, string y, string z)
        {
            Driver.InvokeCrowdMoveToDestination(x, y, z);
        }

        protected void WhenCrowdMoveCompletes()
        {
            Driver.WaitForCrowdMoveCompletion();
        }

        protected void WhenFacingCommandsIssued()
        {
            Driver.InvokeFacingCommandsPostMove();
        }

        protected void WhenGmTriggersAlignWithGangLeader()
        {
            Driver.InvokeAlignFacingWithGangLeader();
        }

        protected void ThenDisplacementVector(string expected)
        {
            string actual = Driver.GetCrowdMoveDisplacementVector();
            Assert.AreEqual(expected, actual,
                string.Format("Displacement vector: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenFormationPreserved(string expectedOffsets)
        {
            string actual = Driver.GetGroupFormationOffsets();
            Assert.AreEqual(expectedOffsets, actual,
                string.Format("Formation offsets: expected '{0}' got '{1}'", expectedOffsets, actual));
        }

        protected void ThenSpreadSlots(string expected)
        {
            string actual = Driver.GetComputedSpreadSlots();
            Assert.AreEqual(expected, actual,
                string.Format("Spread slots: expected '{0}' got '{1}'", expected, actual));
        }

        protected void ThenFacingVector(string member, string expected)
        {
            string actual = Driver.GetCharacterFacingVector(member);
            Assert.AreEqual(expected, actual,
                string.Format("Facing for '{0}': expected '{1}' got '{2}'", member, expected, actual));
        }

        protected void ThenFacingUnavailable()
        {
            Assert.IsTrue(Driver.WasGangLeaderFacingUnavailable(),
                "Gang leader facing should be unavailable");
        }

        protected void ThenMoveBlocked()
        {
            Assert.IsTrue(Driver.WasCrowdMoveBlocked(), "Crowd move should be blocked");
        }
    }
}
