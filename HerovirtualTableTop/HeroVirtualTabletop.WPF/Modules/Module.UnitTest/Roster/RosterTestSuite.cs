using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.HCSIntegration;
using Module.HeroVirtualTabletop.Roster;
using Module.Shared;
using System.Collections;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using System.IO;
using System.Linq;
using Moq;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.Library.Sevices;

namespace Module.UnitTest.Roster
{
    [TestClass]
    public class RosterTest : BaseCrowdTest
    {
        private RosterExplorerViewModel rosterExplorerViewModel;
        protected Mock<IHCSIntegrator> hcsIntegratorMock = new Mock<IHCSIntegrator>();

        [TestInitialize]
        public void TestInitialize()
        {
            ResetKeyBindGeneratorStatics();
            InitializeDefaultList();
            InitializeCrowdRepositoryMockWithDefaultList();
            this.numberOfItemsFound = 0;

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, keyEventHandlerMock.Object, hcsIntegratorMock.Object, eventAggregator);
        }

        #region Add To Roster

        /// <summary>
        /// A character should properly be added to roster (acceptance test)
        /// </summary>
        [TestMethod]
        public void AddCharacterToRoster_AddsTheCharacterToRoster()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            Assert.IsTrue(rosterExplorerViewModel.Participants.Contains(characterExplorerViewModel.SelectedCrowdMemberModel));
        }
        /// <summary>
        /// All the characters under a crowd should be added properly to the roster (acceptance test)
        /// </summary>
        [TestMethod]
        public void AddCharacterToRoster_AddsAllCharacterFromACrowdToRoster()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            foreach (ICrowdMemberModel model in characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection)
            {
                if(model is CrowdMemberModel)
                {
                    Assert.IsTrue(rosterExplorerViewModel.Participants.Contains(model));
                    Assert.AreEqual(model.RosterCrowd, characterExplorerViewModel.SelectedCrowdModel);
                }
            }
        }
        /// <summary>
        /// If a crowd has nested crowds, adding the outer crowd should result in characters from the inner crowd to be added to roster using the inner crowd as the roster crowd
        /// </summary>
        [TestMethod]
        public void AddCharacterToRoster_UsesNestedCrowdNameInRosterWhenAddedFromContainingCrowd()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            var char1 = rosterExplorerViewModel.Participants["Batman"];
            Assert.IsTrue(char1.RosterCrowd.Name == "Gotham City");
            char1 = rosterExplorerViewModel.Participants["Robin"];
            Assert.IsTrue(char1.RosterCrowd.Name == "Gotham City");
            char1 = rosterExplorerViewModel.Participants["Scarecrow"];
            Assert.IsTrue(char1.RosterCrowd.Name == "The Narrows");
        }
        /// <summary>
        /// If a character is already added to roster under a different crowd, it should be cloned first in the current crowd before being added to roster
        /// The cloned character should be added to the roster under the current crowd and the original character should remain as it is
        /// </summary>
        [TestMethod]
        public void AddCrowdToRoster_ClonesCharacterIfAlreadyPresentInRosterUnderDifferentCrowd()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // Selecting All Characters crowd
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel; // Selecting Batman to add to roster
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Selecting Gotham City
            characterExplorerViewModel.SelectedCrowdMemberModel = null; // All the characters would be added to roster
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            var clonedChar = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault();
            Assert.IsNotNull(clonedChar); // Cloning was done for batman

            var char1 = rosterExplorerViewModel.Participants["Batman"];
            Assert.IsFalse(char1.RosterCrowd.Name == "Gotham City");
            Assert.IsTrue(char1.RosterCrowd.Name == Constants.ALL_CHARACTER_CROWD_NAME);
            char1 = rosterExplorerViewModel.Participants["Robin"];
            Assert.IsTrue(char1.RosterCrowd.Name == "Gotham City");
            char1 = rosterExplorerViewModel.Participants["Batman (1)"];
            Assert.IsTrue(char1.RosterCrowd.Name == "Gotham City");
        }
        /// <summary>
        /// If cloning was necessary to add to roster, the repository should be updated
        /// </summary>
        [TestMethod]
        public void AddCharacterToRoster_UpdatesRepositoryIfCloningIsDone()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // Selecting All Characters crowd
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel; // Selecting Batman to add to roster
            characterExplorerViewModel.AddToRosterCommand.Execute(null);
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.IsAny<List<CrowdModel>>()), Times.Once()); // We added to roster so saving RosterCrowd change
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Selecting Gotham City
            characterExplorerViewModel.SelectedCrowdMemberModel = null; // All the characters would be added to roster
            characterExplorerViewModel.AddToRosterCommand.Execute(null);
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList =>
                        cmList.Where(cm => cm.Name == "Gotham City").First().CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault() != null))); // Now repository should be updated
        }
        #endregion

        #region Remove Character from Desktop Tests

        [TestMethod]
        public void RemoveCharacterFromDesktop_GeneratesTargetAndDeleteKeybindAndRemovesFromRoster()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            rosterExplorerViewModel.ClearFromDesktopCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains("target_name Batman$$delete_npc"));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);

            Assert.IsFalse(rosterExplorerViewModel.Participants.Contains(character));
        }

        #endregion
    }
}
