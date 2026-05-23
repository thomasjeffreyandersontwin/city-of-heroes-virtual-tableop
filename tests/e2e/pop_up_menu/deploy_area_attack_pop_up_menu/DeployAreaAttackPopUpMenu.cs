using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.PopUpMenu
{
    [TestClass]
    public class DeployAreaAttackPopUpMenu : PopUpMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
        }

        [TestMethod]
        public void SessionInitDeploySucceeds()
        {
            GivenDeploymentTrigger("session initialization");
            WhenGameSessionInitialized();
            ThenAreaAttackDeployed();
        }

        [TestMethod]
        public void WriteOrLoadFailsWarnsButSessionContinues()
        {
            GivenDeploymentTrigger("session initialization");
            GivenMenusDirectoryState("not writable");
            WhenGameSessionInitialized();
            ThenDeploymentWarning();
        }

        [TestMethod]
        public void AlreadyDeployedRedeployOverwrites()
        {
            GivenDeploymentTrigger("session initialization");
            GivenMenuDefinitionContent("area_attack_menu_v1");
            WhenGameSessionInitialized();
            ThenAreaAttackDeployed();
        }

        [TestMethod]
        public void GmUsesHudMenuEntriesDesignationReceived()
        {
            GivenDeploymentTrigger("session initialization");
            WhenGameSessionInitialized();
            ThenAreaAttackDeployed();
        }
    }
}
