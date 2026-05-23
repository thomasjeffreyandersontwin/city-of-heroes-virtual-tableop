using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.PopUpMenu
{
    [TestClass]
    public class LoadPopUpMenuInGame : PopUpMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
        }

        [TestMethod]
        public void FileWrittenLoadSucceeds()
        {
            GivenMenuDefinitionContent("area_attack_menu_v1");
            WhenApplicationIssuesLoadMenuCommand();
            ThenMenuLoadedInGame();
        }

        [TestMethod]
        public void FileNotWrittenLoadFails()
        {
            GivenMenuDefinitionContent("not_written");
            WhenApplicationIssuesLoadMenuCommand();
            ThenMenuLoadFailed();
        }

        [TestMethod]
        public void CohClientNotRunningBridgeUnavailable()
        {
            GivenGameBridgeNotReady();
            GivenMenuDefinitionContent("area_attack_menu_v1");
            WhenApplicationIssuesLoadMenuCommand();
            ThenMenuLoadFailed();
        }

        [TestMethod]
        public void UpdatedFileReloadReplaces()
        {
            GivenMenuDefinitionContent("area_attack_menu_v2");
            WhenApplicationIssuesLoadMenuCommand();
            ThenMenuLoadedInGame();
        }
    }
}
