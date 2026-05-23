using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.PopUpMenu
{
    public class PopUpMenuHelper
    {
        protected AppDriver Driver;

        protected void GivenGameBridgeInitialized()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.SetGameBridgeState("ready");
        }

        protected void GivenMenusDirectoryState(string writableState)
        {
            Driver.SetMenusDirectoryWritableState(writableState);
        }

        protected void GivenGameBridgeNotReady()
        {
            Driver.SetGameBridgeState("not_ready");
        }

        protected void GivenMenuDefinitionContent(string content)
        {
            Driver.SetPopUpMenuContent(content);
        }

        protected void GivenDeploymentTrigger(string trigger)
        {
            Driver.SetAreaAttackDeploymentTrigger(trigger);
        }

        protected void WhenApplicationWritesMenu()
        {
            Driver.InvokeWritePopUpMenu();
        }

        protected void WhenApplicationIssuesLoadMenuCommand()
        {
            Driver.InvokeLoadPopUpMenu();
        }

        protected void WhenGameSessionInitialized()
        {
            Driver.SimulateGameSessionInit();
        }

        protected void ThenMenuWritten(string expectedContent)
        {
            string actual = Driver.GetPopUpMenuWrittenContent();
            Assert.AreEqual(expectedContent, actual,
                string.Format("Menu content: expected '{0}' got '{1}'", expectedContent, actual));
        }

        protected void ThenMenuNotWritten()
        {
            Assert.IsTrue(Driver.WasMenuWriteFailed(), "Menu write should have failed");
        }

        protected void ThenMenuLoadedInGame()
        {
            Assert.IsTrue(Driver.WasMenuLoadedInGame(), "Menu should be loaded in game");
        }

        protected void ThenMenuLoadFailed()
        {
            Assert.IsTrue(Driver.WasMenuLoadFailed(), "Menu load should have failed");
        }

        protected void ThenAreaAttackDeployed()
        {
            Assert.IsTrue(Driver.WasAreaAttackMenuDeployed(), "Area attack menu should be deployed");
        }

        protected void ThenDeploymentWarning()
        {
            string msg = Driver.GetLastValidationMessage();
            Assert.IsNotNull(msg, "Expected deployment warning");
        }
    }
}
