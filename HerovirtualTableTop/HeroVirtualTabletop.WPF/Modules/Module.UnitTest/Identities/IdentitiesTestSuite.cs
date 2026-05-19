using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Identities;
using Moq;
using Module.HeroVirtualTabletop.Library.Enumerations;
using System.IO;
using Module.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Module.UnitTest.Identities
{
    [TestClass]
    public class IdentitiesTest : BaseTest
    {
        Character TestCharacter;
        CharacterEditorViewModel CharEditorVM;

        [TestInitialize]
        public void Initialize()
        {
            TestCharacter = new Mock<Character>().Object;
            var keyHandlerMock = new Mock<IDesktopKeyEventHandler>();
            CharEditorVM = new CharacterEditorViewModel(busyServiceMock.Object, unityContainerMock.Object, keyHandlerMock.Object, eventAggregator);
            CharEditorVM.EditedCharacter = TestCharacter;
        }

        [TestMethod]
        public void AddIdentity_CreatesNewIdentity()
        {
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);

            Assert.IsTrue(TestCharacter.AvailableIdentities.Count == 1);
        }

        [TestMethod]
        public void AddIdentity_CreatesNewIdentity_WithUniqueName()
        {
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);

            var id1 = TestCharacter.AvailableIdentities.Where(id => id.Name == "Identity");
            var id2 = TestCharacter.AvailableIdentities.Where(id => id.Name == "Identity (1)").FirstOrDefault();
            Assert.IsTrue(id1.Count() == 1 && id2 != null);
        }

        [TestMethod]
        public void AddIdentity_SetItAsDefaultIfItIsTheFirst()
        {
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);

            Assert.AreEqual(TestCharacter.AvailableIdentities[0], TestCharacter.DefaultIdentity);

            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);
            
            Assert.AreNotEqual(TestCharacter.AvailableIdentities[1], TestCharacter.DefaultIdentity);
        }

        [TestMethod]
        public void AddIdentity_SetItAsActiveIfItIsTheFirst()
        {
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);

            Assert.IsTrue(TestCharacter.AvailableIdentities.Count == 1);

            Assert.AreEqual(TestCharacter.AvailableIdentities[0], TestCharacter.ActiveIdentity);

            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);

            Assert.AreNotEqual(TestCharacter.AvailableIdentities[1], TestCharacter.ActiveIdentity);
        }

        [TestMethod]
        public void RemoveIdentity_RemoveSelectedIdentity()
        {
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);

            Assert.IsTrue(TestCharacter.AvailableIdentities.Count == 2);

            Identity id = TestCharacter.AvailableIdentities[1];
            CharEditorVM.IdentitiesViewModel.SelectedOption = id;

            CharEditorVM.IdentitiesViewModel.RemoveOptionCommand.Execute(null);

            Assert.IsFalse(TestCharacter.AvailableIdentities.Contains(id));
            Assert.IsFalse(CharEditorVM.IdentitiesViewModel.OptionGroup.Contains(id));
        }

        [TestMethod] //NEED TO CHECK IF WE CAN REMOVE ALL IDENTITIES
        public void RemoveIdentity_PreventFromRemovingAllIdentities()
        {
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);

            Assert.IsTrue(TestCharacter.AvailableIdentities.Count == 1);

            Identity id = TestCharacter.AvailableIdentities[0];
            CharEditorVM.IdentitiesViewModel.SelectedOption = id;

            CharEditorVM.IdentitiesViewModel.RemoveOptionCommand.Execute(null);

            Assert.IsTrue(TestCharacter.AvailableIdentities.Contains(id));
        }

        [TestMethod]
        public void RemoveIdentity_RemovingTheDefaultIdentitySetAnotherOneAsDefault()
        {
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);

            Assert.IsTrue(TestCharacter.AvailableIdentities.Count == 2);

            Identity id = TestCharacter.DefaultIdentity;
            CharEditorVM.IdentitiesViewModel.SelectedOption = id;

            CharEditorVM.IdentitiesViewModel.RemoveOptionCommand.Execute(null);
            
            Assert.IsNotNull(TestCharacter.DefaultIdentity);
            Assert.AreNotEqual(id, TestCharacter.DefaultIdentity);
        }

        [TestMethod]
        public void RemoveIdentity_RemovingTheActiveIdentitySetTheDefaultOneAsActive()
        {
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);
            CharEditorVM.IdentitiesViewModel.AddOptionCommand.Execute(null);

            Assert.IsTrue(TestCharacter.AvailableIdentities.Count == 2);

            Identity defaultId = TestCharacter.AvailableIdentities[0];
            TestCharacter.DefaultIdentity = defaultId;

            Identity activeId = TestCharacter.AvailableIdentities[1];
            TestCharacter.ActiveIdentity = activeId;

            CharEditorVM.IdentitiesViewModel.SelectedOption = activeId;

            CharEditorVM.IdentitiesViewModel.RemoveOptionCommand.Execute(null);

            Assert.IsNotNull(TestCharacter.ActiveIdentity);
            Assert.AreNotEqual(activeId, TestCharacter.ActiveIdentity);
            Assert.AreEqual(TestCharacter.DefaultIdentity, TestCharacter.ActiveIdentity);
        }

    }
}
