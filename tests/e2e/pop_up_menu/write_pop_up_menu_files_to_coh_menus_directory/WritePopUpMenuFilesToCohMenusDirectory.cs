using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.PopUpMenu
{
    [TestClass]
    public class WritePopUpMenuFilesToCohMenusDirectory : PopUpMenuHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenGameBridgeInitialized();
        }

        [TestMethod]
        public void DirectoryWritableWriteSucceeds()
        {
            GivenMenusDirectoryState("writable");
            WhenApplicationWritesMenu();
            ThenMenuWritten("area_attack_menu_v1");
        }

        [TestMethod]
        public void FileAlreadyExistsOverwritten()
        {
            GivenMenusDirectoryState("writable");
            GivenMenuDefinitionContent("area_attack_menu_v1");
            WhenApplicationWritesMenu();
            ThenMenuWritten("area_attack_menu_v2");
        }

        [TestMethod]
        public void DirectoryNotWritableWriteFails()
        {
            GivenMenusDirectoryState("not writable");
            WhenApplicationWritesMenu();
            ThenMenuNotWritten();
        }
    }
}
