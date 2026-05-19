using Framework.WPF.Library;
using Framework.WPF.Services.MessageBoxService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.HCSIntegration;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Roster;
using Module.Shared;
using Module.Shared.Messages;
using Moq;
using Prism.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Module.UnitTest.Crowds
{
    #region Crowd Repository Test
    [TestClass]
    public class CrowdRepositoryTest : BaseCrowdTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        public void GetCrowdCollection_CreatesNewRepositoryIfNoPresentRepositoryFile()
        {
            string testRepoFileName = "test.data";
            string fullFilePath = Path.Combine(testRepoFileName);
            if (File.Exists(fullFilePath))
                File.Delete(fullFilePath);
            // Here we are directly testing the repository and need to verify file i/o. So, not mocking.
            CrowdRepository crowdRepository = new CrowdRepository();
            crowdRepository.CrowdRepositoryPath = fullFilePath;
            List<CrowdModel> retrievedCrowdList = null;
            AutoResetEvent terminateEvent = new AutoResetEvent(false);
            bool testPassed = false;
            ThreadStart ts = delegate
            {
                crowdRepository.GetCrowdCollection(
                (List<CrowdModel> crowdList) =>
                {
                    try
                    {
                        retrievedCrowdList = crowdList;
                        Assert.IsTrue((File.Exists(fullFilePath)));
                        File.Delete(fullFilePath);
                        testPassed = true;
                    }
                    catch (AssertFailedException ex)
                    {
                        terminateEvent.Set();
                    }
                    finally
                    {
                        terminateEvent.Set();
                    }
                }
                );
            };
            Thread t = new Thread(ts);
            t.Start();
            terminateEvent.WaitOne();
            Assert.IsTrue(testPassed);
        }

        /// <summary>
        /// This is actually the acceptance test for crowd repository. Although the method name suggests only SaveCrowdCollection related test, it actually performs tests on both GetCrowdCollection
        /// and SaveCrowdCollection for their basic functionalities.
        /// </summary>
        [TestMethod]
        public void SaveCrowdCollection_SavesCrowdCollectionsConsistently()
        {
            CrowdModel crowd = new CrowdModel { Name = "Test Crowd 1" };
            CrowdModel childCrowd = new CrowdModel { Name = "Child Crowd 1" };
            CrowdMemberModel crowdMember1 = new CrowdMemberModel { Name = "Test CrowdMember 1" };
            CrowdMemberModel crowdMember2 = new CrowdMemberModel { Name = "Test CrowdMember 1.1" };
            crowd.Add(new List<ICrowdMemberModel>() { crowdMember1, childCrowd });
            childCrowd.Add( new List<ICrowdMemberModel>() { crowdMember2 });
            string testRepoFileName = "test.data";
            CrowdRepository crowdRepository = new CrowdRepository();
            crowdRepository.CrowdRepositoryPath = testRepoFileName;
            List<CrowdModel> crowdCollection = new List<CrowdModel>() { crowd };
            AutoResetEvent terminateEvent = new AutoResetEvent(false);
            bool testPassed = false;

            ThreadStart ts = delegate
            {
                crowdRepository.SaveCrowdCollection(() =>
                {
                    // More crowd members being added, repository shouldn't know
                    crowdCollection.Add(new CrowdModel() { Name = "New Crowd 1" });
                    crowd.Add(new CrowdMemberModel() { Name = "New CrowdMember 1" });

                    List<CrowdModel> retrievedCrowdList = null;
                    crowdRepository.GetCrowdCollection((List<CrowdModel> crowdList) =>
                    {
                        retrievedCrowdList = crowdList;
                        try
                        {
                            CrowdModel cmodel1 = retrievedCrowdList.Where(c => c.Name == "New Crowd 1").FirstOrDefault();
                            Assert.IsNull(cmodel1);
                            CrowdMember cm1 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "New CrowdMember 1").FirstOrDefault() as CrowdMember;
                            Assert.IsNull(cm1);
                            CrowdModel cmodel2 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "Child Crowd 1").FirstOrDefault() as CrowdModel;
                            Assert.IsNotNull(cmodel2);
                            CrowdMember cm2 = cmodel2.CrowdMemberCollection.Where(c => c.Name == "Test CrowdMember 1.1").FirstOrDefault() as CrowdMember;
                            Assert.IsNotNull(cm2);
                        }
                        catch (AssertFailedException ex)
                        {
                            terminateEvent.Set();
                        }
                        // Now save the updated crowd and check if repsitory knows about them
                        crowdRepository.SaveCrowdCollection(() =>
                        {
                            crowdRepository.GetCrowdCollection((List<CrowdModel> crowdListAnother) =>
                            {
                                retrievedCrowdList = crowdListAnother;
                                try
                                {
                                    CrowdModel cmodel1 = retrievedCrowdList.Where(c => c.Name == "New Crowd 1").FirstOrDefault();
                                    Assert.IsNotNull(cmodel1);
                                    CrowdMember cm1 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "New CrowdMember 1").FirstOrDefault() as CrowdMember;
                                    Assert.IsNotNull(cm1);
                                    CrowdModel cmodel2 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "Child Crowd 1").FirstOrDefault() as CrowdModel;
                                    Assert.IsNotNull(cmodel2);
                                    CrowdMember cm2 = cmodel2.CrowdMemberCollection.Where(c => c.Name == "Test CrowdMember 1.1").FirstOrDefault() as CrowdMember;
                                    Assert.IsNotNull(cm2);
                                    File.Delete(testRepoFileName);
                                    testPassed = true;
                                }
                                catch (AssertFailedException ex)
                                {
                                    terminateEvent.Set();
                                }
                                finally
                                {
                                    terminateEvent.Set();
                                }
                            });
                        }, crowdCollection);
                    });
                }, crowdCollection);
            };
            Thread t = new Thread(ts);
            t.Start();
            terminateEvent.WaitOne();
            Assert.IsTrue(testPassed);
        }
    }
    #endregion

    #region Crowd Member Test
    [TestClass]
    public class CrowdMemberTest : BaseCrowdTest
    {
        private RosterExplorerViewModel rosterExplorerViewModel;

        [TestInitialize]
        public void TestInitialize()
        {
            ResetKeyBindGeneratorStatics();
            InitializeDefaultList();
            InitializeCrowdRepositoryMockWithDefaultList();
            this.numberOfItemsFound = 0;
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        
        #region Add Crowd Tests
        /// <summary>
        /// Adding a crowd should obviously call the repository to update the crowd collection in the repository. This is an acceptance test. 
        /// Importantly, we are not checking the contents of repository file to see if the file got updated, that is because we have tested the repository separately in CrowdRepositoryTest
        /// to make sure that the repository can save to file what it is being passed. So, making sure that repository is indeed being called with correct parameters would suffice here.
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdInRepository() 
        {
            InitializeCrowdRepositoryMockWithListAndSynchViewModel(new List<CrowdModel>());// Starting with an empty repository
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.IsAny<List<CrowdModel>>()), Times.Once());
            //Should have "All Characters" and the new Crowd
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => cmList.Count == 1 && cmList[0].Name == "Crowd")));
        }

        /// <summary>
        /// When the repository is empty, Adding a crowd should add it as a stand alone crowd and the All Characters list should not be there yet
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesOnlyOneNewCrowdWithDefaultNamIfRepositoryIsEmpty()
        {
            InitializeCrowdRepositoryMockWithListAndSynchViewModel(new List<CrowdModel>());
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cmodel1 = crowdList.Where(cr => cr.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cmodel2 = crowdList.Where(cr => cr.Name == "Crowd").FirstOrDefault();
            Assert.IsTrue(cmodel1 == null && cmodel2 != null);

        }

        /// <summary>
        /// The name of an added crowd should be unique, and should be "Crowd" or "Crowd (*)" where * stands for the empty string or first available number from 1
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesCrowdsWithUniqueNames()
        {
            InitializeCrowdRepositoryMockWithListAndSynchViewModel(new List<CrowdModel>());
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cr1 = crowdList.Where(cr => cr.Name == "Crowd");
            var cr2 = crowdList.Where(cr => cr.Name == "Crowd (1)").FirstOrDefault();
            Assert.IsTrue(cr1.Count() == 1 && cr2 != null);
        }

        /// <summary>
        /// If no crowd or character is selected, the new crowd should just be added as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesStandAloneCrowdIfNoCrowdOrCharactersSelected()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel.SelectedCrowdModel = null;
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 1);
        }
        /// <summary>
        /// If the current selected member is a Crowd, the new crowd should be added under it and also as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdUnderSelectedCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming "Gotham City" is selected
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 2);// The added crowd should be in a total of two places - one stand alone and one nested position
            CrowdModel crowdAdded = characterExplorerViewModel.CrowdCollection.Where(cm=>cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded); 
        }
        /// <summary>
        /// If the current selected member is a Crowd that appears in multiple locations, the new crowd should be added under all of them, as well as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdUnderAllOccurrancesOfSelectedCrowd()
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[2] as CrowdModel; // Assuming "The Narrows" is selected
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 4); // The added crowd should be in a total of 4 places - one stand alone and three nested positions under The Narrows
            CrowdModel crowdAdded = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            crowdAdded = (crowd1.CrowdMemberCollection[2] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd2 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "League of Shadows").FirstOrDefault();
            crowdAdded = crowd2.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            crowdAdded = (crowd2.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded);
        }
        /// <summary>
        /// If the current selected member is a character under All Characters or the All Characters crowd itself, the new crowd should be added as just another crowd and not under All Characters
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdAsStandAloneIfSelectedCrowdIsAllCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            // "All Characters" is the selected crowd as selecting a character under All Characters would also result in the containing crowd being selected in view model
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]; 
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 1); // The added crowd should be added only as a stand alone crowd
            CrowdModel crowdAdded = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            // And so on...
        }
        /// <summary>
        /// If the current selected member is a Character not under All Characters Crowd but in another Crowd, the new crowd should be added as a sibling of that character in all 
        /// occurances of that crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdAsSiblingOfSelectedCharacterNotUnderAllCharacters()
        {
            // Since selecting a character would result in the containing crowd to be selected in the view model, here the containing Crowd would be selected and this case 
            // has already been covered in another test above.
            AddCrowd_CreatesNewCrowdUnderAllOccurrancesOfSelectedCrowd();
        }
        #endregion

        #region Add Character Tests

        /// <summary>
        /// Adding a character should add it in the crowd. Depending on the situation, it could be added to one or more crowds. 
        /// This test is a combination of several other test cases.
        /// This is an acceptance test.
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterInCrowd()
        {
            AddCharacter_CreatesNewCharacterUnderAllCharactersIfNoCrowdOrCharacterIsSelected();
            TestInitialize();
            AddCharacter_CreatesNewCharacterUnderSelectedCrowd();
            TestInitialize();
            AddCharacter_CreatesNewCharacterUnderAllOccurrancesOfSelectedCrowd();
        }
        /// <summary>
        /// Adding a character should obviously call the repository to insert the character in the repository.
        /// Importantly, we are not checking the contents of repository file to see if the file got updated, that is because we have tested the repository separately in CrowdRepositoryTest
        /// to make sure that the repository can save to file what it is being passed. So, making sure that repository is indeed being called with correct parameters would suffice here.
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterInRepository()
        {
            InitializeCrowdRepositoryMockWithListAndSynchViewModel(new List<CrowdModel>());// Starting with an empty repository
            characterExplorerViewModel.SelectedCrowdModel = null;
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.IsAny<List<CrowdModel>>()), Times.Once());
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => cmList.Count == 1 && cmList[0].Name == Constants.ALL_CHARACTER_CROWD_NAME && cmList[0].CrowdMemberCollection.Count == 1 && cmList[0].CrowdMemberCollection[0].Name == "Character")));
        }
        /// <summary>
        /// When the repository is empty, Adding a character should first create the default All Characters crowd and the new character should be added under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesAllCharactersCrowdAndAddsNewCharacterUnderAllCharactersIfRepositoryIsEmpty()
        {
            InitializeCrowdRepositoryMockWithListAndSynchViewModel(new List<CrowdModel>());
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cmodel1 = crowdList.Where(cr => cr.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            Assert.IsNotNull(cmodel1);
            var char1 = cmodel1.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(char1);
        }
        /// <summary>
        /// The name of an added character should be unique, and should be "Character" or "Character (*)" where * stands for the empty string or first available number from 1
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesCharactersWithUniqueNames()
        {
            InitializeCrowdRepositoryMockWithListAndSynchViewModel(new List<CrowdModel>());
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cr1 = crowdList.Where(cr => cr.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            Assert.IsTrue(cr1 != null && cr1.CrowdMemberCollection.Count() == 2);
            var cm1 = cr1.CrowdMemberCollection.Where(c => c.Name == "Character");
            var cm2 = cr1.CrowdMemberCollection.Where(c => c.Name == "Character (1)").FirstOrDefault();
            Assert.IsTrue(cm1 != null && cm1.Count() == 1 && cm2 != null);
        }
        /// <summary>
        /// If no crowd or character is selected, the new character should just be added under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterUnderAllCharactersIfNoCrowdOrCharacterIsSelected()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel.SelectedCrowdModel = null;
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            Assert.IsTrue(this.numberOfItemsFound == 1);
            var cm1 = crowdList.ToList()[0].CrowdMemberCollection.Where(c => c.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
        }
        /// <summary>
        /// If the current selected member is a Crowd, the new character should be added under it and also under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterUnderSelectedCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming "Gotham City" is selected
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            Assert.IsTrue(this.numberOfItemsFound == 2);// The added character should be in a total of two places - one under All Characters and one under Gotham City
            CrowdModel crowdAllCharacters = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cm1 = crowdAllCharacters.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            cm1 = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
        }
        /// <summary>
        /// If the current selected member is a Crowd that appears in multiple locations, the new character should be added under all of them, as well as under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterUnderAllOccurrancesOfSelectedCrowd()
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[2] as CrowdModel; // Assuming "The Narrows" is selected
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            // The added character should be in a total of 4 places - one under All Characters and thrice under The Narrows as it appears in three places in the Crowd list
            Assert.IsTrue(this.numberOfItemsFound == 4);
            CrowdModel crowdAllCharacters = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cm1 = crowdAllCharacters.CrowdMemberCollection.Where(c => c.Name == "Character");
            Assert.IsNotNull(cm1);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            var cm2 = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNull(cm2);
            var cm3 = (crowd1.CrowdMemberCollection[2] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm3);
            CrowdModel crowd2 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "League of Shadows").FirstOrDefault();
            var cm4 = crowd2.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNull(cm4);
            var cm5 = (crowd2.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm5);
        }
        /// <summary>
        /// If the current selected member is a Character under All Characters, the new character should be added as a sibling of that character under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterAsSiblingOfSelectedCharacter()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            // Assuming a character is selected under All Characters. Then All Characters is the selected crowd
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            Assert.IsTrue(this.numberOfItemsFound == 1); // The added character should be added under All Characters
            CrowdModel crowdAllCharacters = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cm1 = crowdAllCharacters.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
        }

        #endregion

        #region Delete Crowd Tests
        /// <summary>
        /// Here we just need to make sure that the repository is being called with updated collection that does not have the deleted crowd in it.
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_RemovesCrowdFromRepository() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.No); // Just Delete the crowd
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null; // The selected crowd is in main tree, not nested in another crowd
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.IsAny<List<CrowdModel>>()), Times.Once());
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => cmList.Where(cm=>cm.Name == "Gotham City").FirstOrDefault() == null)));
        }
        /// <summary>
        /// User should not be allowed to delete the All Characters crowd
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_PreventsUserFromDeletingAllCharactersCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]; // Selecting All Characters to delete it
            bool b = characterExplorerViewModel.DeleteCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(b); // The delete command will not be available to the user
        }
        /// <summary>
        /// If the crowd is not in another crowd, rather in the main crowd collection, it will be removed from the main collection and repository
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_RemovesCrowdFromCrowdCollectionIfNotNestedWithinAnotherCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.No); // Just Delete the crowd
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null; // Since the crowd is in main collection, there is no parent for this crowd
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var crowd = crowdList.Where(cr => cr.Name == "Gotham City").FirstOrDefault();
            Assert.IsNull(crowd);
        }       
        /// <summary>
        /// If the crowd is in another crowd, it will be removed only from all instances of the containing crowd, but not from main collection or repository or any other crowd
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_RemovesCrowdOnlyFromContainingCrowdIfNestedWithinAnotherCrowd()
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[2] as CrowdModel; // Selecting The Narrows for deletion
            characterExplorerViewModel.SelectedCrowdParent = characterExplorerViewModel.CrowdCollection[1] as CrowdModel;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var crowd = crowdList[1].CrowdMemberCollection.Where(c => c.Name == "The Narrows").FirstOrDefault();
            Assert.IsNull(crowd);
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "The Narrows");
            Assert.IsTrue(this.numberOfItemsFound > 0);
            crowd = crowdList.Where(cr => cr.Name == "The Narrows").FirstOrDefault();
            Assert.IsNotNull(crowd);
        }
        /// <summary>
        /// A deletion attempt for a crowd in main collection should prompt the user about deleting contained characters
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_PromptsUserWithMessageBeforeDeletion()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.No);

            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            messageBoxServiceMock.Verify(
                msgservice => msgservice.ShowDialog(It.Is<string>(s => s == Messages.DELETE_CONTAINING_CHARACTERS_FROM_CROWD_PROMPT_MESSAGE),
                    It.Is<string>(s => s == Messages.DELETE_CROWD_CAPTION), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()), Times.Once);
        }
        /// <summary>
        /// If user chooses to remove characters contained in a crowd as well as deleting the crowd, the crowd and only the crowd specific characters (not member of any other crowd)
        /// should get removed
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_RemovesCrowdAndCrowdSpecificCharactersIfUserChoosesToRemoveContainedCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.Yes); // Pre-configuring message box to decide to delete characters
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Gotham City");
            Assert.IsTrue(this.numberOfItemsFound == 0); // Gotham City Destroyed...
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Batman"); 
            Assert.IsTrue(this.numberOfItemsFound == 0); // And no sign of Batman :(
        }
        /// <summary>
        /// If user chooses to keep characters contained in a crowd, the characters remain under All Characters while the crowd gets deleted
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_RemovesCrowdButKeepsCharactersIfUserChoosesToKeepCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.No); // Pre-configuring message box to decide not to delete characters
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Gotham City");
            Assert.IsTrue(this.numberOfItemsFound == 0); // Gotham City Destroyed...
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Batman");
            Assert.IsTrue(this.numberOfItemsFound > 0); // But Batman still lives
        }
        /// <summary>
        /// No removal of character or crowd happens if user cancels the request after being prompted
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_DoesNothingIfUserCancelsDeleteRequest()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.Cancel); // Pre-configuring message box to cancel delete request
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var crowd = crowdList.Where(cr => cr.Name == "Gotham City").FirstOrDefault();
            Assert.IsNotNull(crowd); // Crowd has not been deleted
        }

        #endregion

        #region Delete Character Tests
        /// <summary>
        /// Character should be deleted from all instances of the containing crowd
        /// </summary>
        [TestMethod]
        public void DeleteCharacter_RemovesCharacterFromCrowd() 
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdMemberModel = (characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[2] as CrowdModel).CrowdMemberCollection[0] as CrowdMemberModel; // Selecting Scarecrow to delete
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[2] as CrowdModel; // The Narrows is the selected crowd
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMemberModel> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Scarecrow");
            Assert.IsTrue(this.numberOfItemsFound == 1); // There is one occurrance of Scarecrow
            var existingChar = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection.Where(cm => cm.Name == "Scarecrow").FirstOrDefault();
            Assert.IsNotNull(existingChar); // The only one occurrance is in All Characters crowd
        }
        /// <summary>
        /// User should be prompted before deleting a character from All Characters
        /// </summary>
        [TestMethod]
        public void DeleteCharacter_PromptsUserBeforeRemovingFromAllCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.No);

            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel; // Selecting Batman to delete
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0] as CrowdModel;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            messageBoxServiceMock.Verify(
                msgservice => msgservice.ShowDialog(It.Is<string>(s => s == Messages.DELETE_CHARACTER_FROM_ALL_CHARACTERS_CONFIRMATION_MESSAGE),
                    It.Is<string>(s => s == Messages.DELETE_CHARACTER_CAPTION), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()), Times.Once);
        }
        /// <summary>
        /// If deleting from All Characters, the character should be removed from repository
        /// </summary>
        [TestMethod]
        public void DeleteCharacter_RemovesCharacterFromRepositoryIfDeletingFromAllCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.Yes); // Pre-configuring message box to confirm delete request
            
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel; // Selecting Batman to delete
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]as CrowdModel;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => 
                        cmList.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).First().CrowdMemberCollection.Where(c=>c.Name == "Batman").FirstOrDefault() == null)));
        }

        #endregion

        #region Filter Character Tests
        [TestMethod]
        public void FilterCrowdMembers_ReturnsFilteredListOfCrowdMembers()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            

            characterExplorerViewModel.Filter = "Batman";

            List<ICrowdMemberModel> matches = GetFlattenedMemberList(characterExplorerViewModel.CrowdCollection.Cast<ICrowdMemberModel>().ToList()).Where(cm => { return cm.IsMatched; }).ToList();

            Assert.AreEqual(matches.Count, 4); //Matches should be: All Character and Batman inside plus Gotham City and Batman inside
        }
        [TestMethod]
        public void FilterCrowdMembers_EmptyFilterReturnsAll()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            

            characterExplorerViewModel.Filter = string.Empty;

            List<ICrowdMemberModel> matches = GetFlattenedMemberList(characterExplorerViewModel.CrowdCollection.Cast<ICrowdMemberModel>().ToList()).Where(cm => { return cm.IsMatched; }).ToList();

            Assert.AreEqual(matches.Count, GetFlattenedMemberList(characterExplorerViewModel.CrowdCollection.Cast<ICrowdMemberModel>().ToList()).Count);

        }
        [TestMethod]
        public void FilterCrowdMembers_FilterIsNotCaseSensitive()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            

            characterExplorerViewModel.Filter = "BaTmAn";

            List<ICrowdMemberModel> matches = GetFlattenedMemberList(characterExplorerViewModel.CrowdCollection.Cast<ICrowdMemberModel>().ToList()).Where(cm => { return cm.IsMatched; }).ToList();

            Assert.AreEqual(matches.Count, 4); //Matches should be: All Character and Batman inside plus Gotham City and Batman inside
        }
        [TestMethod]
        public void FilterCrowdMembers_IfCrowdIsMatchItsExpanded()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            

            characterExplorerViewModel.Filter = "Gotham City";

            CrowdModel gotham = characterExplorerViewModel.CrowdCollection.First(cr => { return cr.Name == "Gotham City"; });

            Assert.IsTrue(gotham.IsMatched);
            Assert.IsTrue(gotham.IsExpanded);

        }
        [TestMethod]
        public void FilterCrowdMembers_IfCrowdIsMatchEveryCharacterInItIsMatch()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            

            characterExplorerViewModel.Filter = "Gotham City";

            CrowdModel gotham = characterExplorerViewModel.CrowdCollection.First(cr => { return cr.Name == "Gotham City"; });

            foreach (ICrowdMemberModel cm in gotham.CrowdMemberCollection)
            {
                Assert.IsTrue(cm.IsMatched);
            }
        }
        [TestMethod]
        public void FilterCrowdMembers_IfCharacterIsMatchContainingCrowdIsMatch()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            

            characterExplorerViewModel.Filter = "Batman";

            CrowdModel gotham = characterExplorerViewModel.CrowdCollection.First(cr => { return cr.Name == "Gotham City"; });

            Assert.IsTrue(gotham.IsMatched);
        }
        [TestMethod]
        public void FilterCrowdMembers_IfCharacterIsMatchContainingCrowdIsExpanded()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            

            characterExplorerViewModel.Filter = "Batman";

            CrowdModel gotham = characterExplorerViewModel.CrowdCollection.First(cr => { return cr.Name == "Gotham City"; });
            
            Assert.IsTrue(gotham.IsExpanded);
        }

        #endregion

        #region Rename Character/Crowd Tests
        /// <summary>
        /// Repository should be updated properly after each rename
        /// </summary>
        [TestMethod]
        public void RenameCharacterCrowd_UpdatesRepoCorrectly() 
        {
            RunOnStaThread(() =>
            {
                InitializeCrowdRepositoryMockWithDefaultList();
            
                characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel; // Selecting Batman to Rename
                characterExplorerViewModel.EnterEditModeCommand.Execute(null);
                System.Windows.Controls.TextBox txtBox = new System.Windows.Controls.TextBox();
                txtBox.Text = "Bat";
                characterExplorerViewModel.SubmitCharacterCrowdRenameCommand.Execute(txtBox);
                crowdRepositoryMock.Verify(
                    repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                        It.Is<List<CrowdModel>>(cmList =>
                            cmList.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).First().CrowdMemberCollection.Where(c => c.Name == "Batman").FirstOrDefault() == null)));
                crowdRepositoryMock.Verify(
                    repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                        It.Is<List<CrowdModel>>(cmList =>
                            cmList.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).First().CrowdMemberCollection.Where(c => c.Name == "Bat").FirstOrDefault() != null)));
            });
        }
        /// <summary>
        /// Crowd or character should not be renamed to another crowd or character that already exists
        /// </summary>
        [TestMethod]
        public void RenameCharacterCrowd_PreventsDuplication()
        {
            RunOnStaThread(() =>
            {
                InitializeCrowdRepositoryMockWithDefaultList();
            
                characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel; // Selecting Batman to Rename
                characterExplorerViewModel.EnterEditModeCommand.Execute(null);
                System.Windows.Controls.TextBox txtBox = new System.Windows.Controls.TextBox();
                txtBox.Text = "Robin"; // Trying to set a name that already exists
                characterExplorerViewModel.SubmitCharacterCrowdRenameCommand.Execute(txtBox);
                messageBoxServiceMock.Verify(
                    msgservice => msgservice.ShowDialog(It.Is<string>(s => s == Messages.DUPLICATE_NAME_MESSAGE),
                        It.Is<string>(s => s == Messages.DUPLICATE_NAME_CAPTION), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()), Times.Once); // Check if user was prompted
                var characters = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection.Where(c => c.Name == "Robin");
                Assert.IsTrue(characters.Count() == 1); // There should be only one character with name Robin
            });
        }
        /// <summary>
        /// All Characters crowd cannot be renamed
        /// </summary>
        [TestMethod]
        public void RenameCharacterCrowd_PreventsAllCharactersRename()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]; // Selecting All Characters to rename
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            bool canRename = characterExplorerViewModel.EnterEditModeCommand.CanExecute(null);
            Assert.IsFalse(canRename);
        }
        #endregion

        #region Clone and Paste Character/Crowd Tests
        /// <summary>
        /// Cloning should create a new character using the copied character in target crowd
        /// </summary>
        [TestMethod]
        public void CloneAndPasteCharacter_AddsNewCrowdMemberFromClonedCharacterToPastedCrowd() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to copy
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[2]; // Will paste to League of Shadows
            var charBeforeClone = characterExplorerViewModel.CrowdCollection[2].CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault();
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var charAfterClone = characterExplorerViewModel.CrowdCollection[2].CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault();
            Assert.IsTrue(charBeforeClone == null && charAfterClone != null);
            
        }
        /// <summary>
        /// Properties should be same for original copied character and cloned character except for their name
        /// </summary>
        [TestMethod]
        public void CloneAndPasteCharacter_CreatesAnotherCharacterWithSameProperties() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to copy
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[2]; // Will paste to League of Shadows
            var originalCharacter = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var clonedCharacter = characterExplorerViewModel.CrowdCollection[2].CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault() as CrowdMemberModel;
            if(originalCharacter.ActiveIdentity != null)
            {
                //Assert.AreEqual(originalCharacter.ActiveIdentity.IsActive, clonedCharacter.ActiveIdentity.IsActive);
                //Assert.AreEqual(originalCharacter.ActiveIdentity.IsDefault, clonedCharacter.ActiveIdentity.IsDefault);
                Assert.AreEqual(originalCharacter.ActiveIdentity.Name, clonedCharacter.ActiveIdentity.Name);
                Assert.AreEqual(originalCharacter.ActiveIdentity.Surface, clonedCharacter.ActiveIdentity.Surface);
                Assert.AreEqual(originalCharacter.ActiveIdentity.Type, clonedCharacter.ActiveIdentity.Type);
            }
            if (originalCharacter.DefaultIdentity != null)
            {
                //Assert.AreEqual(originalCharacter.DefaultIdentity.IsActive, clonedCharacter.DefaultIdentity.IsActive);
                //Assert.AreEqual(originalCharacter.DefaultIdentity.IsDefault, clonedCharacter.DefaultIdentity.IsDefault);
                Assert.AreEqual(originalCharacter.DefaultIdentity.Name, clonedCharacter.DefaultIdentity.Name);
                Assert.AreEqual(originalCharacter.DefaultIdentity.Surface, clonedCharacter.DefaultIdentity.Surface);
                Assert.AreEqual(originalCharacter.DefaultIdentity.Type, clonedCharacter.DefaultIdentity.Type);
            }
            if (originalCharacter.AvailableIdentities != null)
            {
                Assert.AreEqual(originalCharacter.AvailableIdentities.Count, clonedCharacter.AvailableIdentities.Count);
                foreach (var identity in originalCharacter.AvailableIdentities)
                {
                    var clonedIdentity = clonedCharacter.AvailableIdentities.Where(i =>
                        i.Name == identity.Name && i.Surface == identity.Surface && i.Type == identity.Type).FirstOrDefault();
                    Assert.IsNotNull(clonedIdentity);
                }
            }
            Assert.AreEqual(originalCharacter.IsExpanded, clonedCharacter.IsExpanded);
            Assert.AreEqual(originalCharacter.IsMatched, clonedCharacter.IsMatched);
        }
        /// <summary>
        /// Cloned character should not have same name as original character
        /// </summary>
        [TestMethod]
        public void CloneAndPasteCharacter_PreventsDuplicateName() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            IEnumerable<ICrowdMemberModel> baseCrowdList = characterExplorerViewModel.CrowdCollection.ToList();
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Batman");
            int countBeforeClone = numberOfItemsFound;
            numberOfItemsFound = 0;
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to copy
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[2]; // Will paste to League of Shadows
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Batman");
            Assert.AreEqual(numberOfItemsFound, countBeforeClone);
        }
        /// <summary>
        /// Cloning should create a new character using the copied character in target crowd
        /// </summary>
        [TestMethod]
        public void CloneAndPasteCrowd_AddsNewCrowdFromClonedCrowdToPastedCrowd() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Assuming Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = null; // Will copy crowd, not character
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // Will paste to League of Shadows
            var crowdBeforeClone = characterExplorerViewModel.CrowdCollection["League of Shadows"].CrowdMemberCollection.Where(c => c.Name == "Gotham City (1)").FirstOrDefault();
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var crowdAfterClone = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(c => c.Name == "Gotham City (1)").FirstOrDefault();
            Assert.IsTrue(crowdBeforeClone == null && crowdAfterClone != null);
        }
        /// <summary>
        /// Cloning a crowd that has nested crowds should clone the crowd itself, the nested crowd(s) and all the characters inside crowd and nested crowd(s) with unique names
        /// </summary>
        [TestMethod]
        public void CloneAndPasteCrowdWithNestedCrowd_ClonesNestedCrowdsAndMembersWithUniqueNames() 
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // Assuming League of Shadows is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = null; // Will copy crowd, not character
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Will paste to Gotham City
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var crowdAfterClone = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(c => c.Name == "League of Shadows (1)").FirstOrDefault();
            Assert.IsNotNull(crowdAfterClone); // Crowd added under Gotham City
            var nestedCrowdAfterClone = (crowdAfterClone as CrowdModel).CrowdMemberCollection.Where(c => c.Name == "The Narrows (1)").FirstOrDefault();
            Assert.IsNotNull(nestedCrowdAfterClone); // Nested crowd also cloned and added under League of shadows (1) under Gotham City
            crowdAfterClone = characterExplorerViewModel.CrowdCollection.Where(c => c.Name == "League of Shadows (1)").FirstOrDefault();
            Assert.IsNotNull(crowdAfterClone); // Crowd added to main collection
            crowdAfterClone = characterExplorerViewModel.CrowdCollection.Where(c => c.Name == "The Narrows (1)").FirstOrDefault();
            Assert.IsNotNull(crowdAfterClone); // Copied nested crowd as well to main collection
            var allCharactersCrowd = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME];
            var charAfterClone = allCharactersCrowd.CrowdMemberCollection.Where(c => c.Name == "Scarecrow (1)").FirstOrDefault();
            Assert.IsNotNull(charAfterClone); // Character in the cloned crowd has been cloned
            charAfterClone = allCharactersCrowd.CrowdMemberCollection.Where(c => c.Name == "Ra'as Al Ghul (1)").FirstOrDefault();
            Assert.IsNotNull(charAfterClone); // Character in the nested cloned crowd has also been cloned
        }
        /// <summary>
        /// Cloning should also update the repository with cloned characters and crowds
        /// </summary>
        [TestMethod]
        public void CloneAndPasteCharacterOrCrowd_UpdatesRepository() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to copy
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[2]; // Will paste to League of Shadows
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList =>
                        cmList.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).First().CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault() != null)));

        }
        /// <summary>
        /// Cloning should not allow pasting a copied crowd to the All Characters crowd
        /// </summary>
        [TestMethod]
        public void CloneAndPasteCrowd_PreventsPastingCrowdToAllCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]; // Will paste to All Characters
            bool canPaste = characterExplorerViewModel.CloneCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(canPaste);
        }

        /// <summary>
        /// Cloning should always add to number
        /// </summary>
        [TestMethod]
        public void CloneAndPasteCharacterCrowd_AddsToNumberAlways()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.Yes); // Pre-configuring message box to confirm delete request
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Assuming Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to copy
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // Will paste to League of Shadows
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var char1AfterClone = characterExplorerViewModel.CrowdCollection[2].CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault();
            var char2AfterClone = characterExplorerViewModel.CrowdCollection[2].CrowdMemberCollection.Where(c => c.Name == "Batman (2)").FirstOrDefault();
            var char3AfterClone = characterExplorerViewModel.CrowdCollection[2].CrowdMemberCollection.Where(c => c.Name == "Batman (3)").FirstOrDefault();
            Assert.IsTrue(char1AfterClone != null && char2AfterClone != null && char3AfterClone != null);
            // Now delete some characters from All Characters
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; 
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault() as CrowdMemberModel;
            characterExplorerViewModel.DeleteCharacterCrowd(null);
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(c => c.Name == "Batman (3)").FirstOrDefault() as CrowdMemberModel;
            characterExplorerViewModel.DeleteCharacterCrowd(null);
            // Now clone and paste Batman two more times to see if Batman (1) and Batman (3) are recreated
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // Will paste to League of Shadows
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null); 
            characterExplorerViewModel.CloneCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            char1AfterClone = characterExplorerViewModel.CrowdCollection[2].CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault();
            char2AfterClone = characterExplorerViewModel.CrowdCollection[2].CrowdMemberCollection.Where(c => c.Name == "Batman (2)").FirstOrDefault();
            char3AfterClone = characterExplorerViewModel.CrowdCollection[2].CrowdMemberCollection.Where(c => c.Name == "Batman (3)").FirstOrDefault();
            Assert.IsTrue(char1AfterClone != null && char2AfterClone != null && char3AfterClone != null); // The available numbers 1 and 3 should be used
            char2AfterClone = characterExplorerViewModel.CrowdCollection[2].CrowdMemberCollection.Where(c => c.Name == "Batman (4)").FirstOrDefault();
            char3AfterClone = characterExplorerViewModel.CrowdCollection[2].CrowdMemberCollection.Where(c => c.Name == "Batman (5)").FirstOrDefault();
            Assert.IsTrue(char2AfterClone == null && char3AfterClone == null); // Batman (4) or (5) should not be created
        }
        #endregion

        #region Cut and Paste Character Tests
        /// <summary>
        /// Cut-Paste results in moving the character from source crowd to destination crowd
        /// </summary>
        [TestMethod]
        public void CutAndPasteCharacter_MovesCharacterFromSourceCrowdToPastedCrowd() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Assuming Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to cut
            characterExplorerViewModel.CutCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // Will paste to League of Shadows
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var character = characterExplorerViewModel.CrowdCollection["League of Shadows"].CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault(); // Should not be created
            Assert.IsNull(character);
            character = characterExplorerViewModel.CrowdCollection["League of Shadows"].CrowdMemberCollection.Where(c => c.Name == "Batman").FirstOrDefault();
            Assert.IsNotNull(character);
            character = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection.Where(c => c.Name == "Batman").FirstOrDefault();
            Assert.IsNull(character);// Batman should not be in Gotham anymore
        }
        /// <summary>
        /// If character is in All Characters crowd, cut-paste will actually result in linking the character to the destination crowd instead of it cutting it from All Characters
        /// </summary>
        [TestMethod]
        public void CutAndPasteCharacter_LinksCharacterFromAllCharactersToPastedCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // All Characters is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to cut
            characterExplorerViewModel.CutCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // Will paste to League of Shadows
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var character = characterExplorerViewModel.CrowdCollection["League of Shadows"].CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault(); // Should not be created
            Assert.IsNull(character);
            character = characterExplorerViewModel.CrowdCollection["League of Shadows"].CrowdMemberCollection.Where(c => c.Name == "Batman").FirstOrDefault(); // Should be there as reference
            Assert.IsNotNull(character);
            character = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection.Where(c => c.Name == "Batman").FirstOrDefault(); // Batman should still be in Gotham
            Assert.IsNotNull(character);
            character = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME].CrowdMemberCollection.Where(c => c.Name == "Batman").FirstOrDefault(); // Should also be in All Characters
            Assert.IsNotNull(character);
        }
        /// <summary>
        /// If a character or crowd is already within a crowd, the cut-paste will not do anything
        /// </summary>
        [TestMethod]
        public void CutAndPasteCharacterCrowd_DoesNotPasteCharacterOrCrowdIfAlreadyExistingInPastingCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // All Characters is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to cut
            characterExplorerViewModel.CutCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Assuming Gotham City is selected
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var linkedChar = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(c => c.Name == "Batman");
            Assert.IsTrue(linkedChar.Count() == 1); // There should still be only one Batman in Gotham
            var clonedChar = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault();
            Assert.IsNull(clonedChar); // And no cloning should be done either
            // Now try the same with a crowd
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection[1] as CrowdModel; // The Narrows is selected
            characterExplorerViewModel.SelectedCrowdParent = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = null;// Will cut the crowd
            characterExplorerViewModel.CutCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Gotham City is selected
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var linkedCrowd = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(cr => cr.Name == "The Narrows");
            Assert.IsTrue(linkedCrowd.Count() == 1); // Still should be only one Narrows
            var clonedCrowd = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(cr => cr.Name == "The Narrows (1)").FirstOrDefault();
            Assert.IsNull(clonedCrowd); // And no cloning should be done either
        }
        /// <summary>
        /// The All Characters crowd cannot be pasted to from cut
        /// </summary>
        [TestMethod]
        public void CutAndPasteCharacterCrowd_PreventsPastingToAllCharacterCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // All Characters is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to copy
            characterExplorerViewModel.CutCharacterCrowdCommand.Execute(null);
            bool canPaste = characterExplorerViewModel.PasteCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(canPaste);
            // Now try the same with a crowd
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection[1] as CrowdModel; // The Narrows is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.CutCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // All Characters is selected
            canPaste = characterExplorerViewModel.PasteCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(canPaste);
        }
        /// <summary>
        /// The All Characters crowd cannot be cut
        /// </summary>
        [TestMethod]
        public void CutAndPasteCharacterCrowd_PreventsCuttingAllCharacterCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // All Characters is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            bool canCut = characterExplorerViewModel.CutCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(canCut);
        }
        /// <summary>
        /// Successful cut-paste results in repository update
        /// </summary>
        [TestMethod]
        public void CutAndPasteCharacterCrowd_UpdatesRepository()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Assuming Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to cut
            characterExplorerViewModel.CutCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // Will paste to League of Shadows
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList =>
                        cmList.Where(cm => cm.Name == "League of Shadows").First().CrowdMemberCollection.Where(c => c.Name == "Batman").FirstOrDefault() != null)));
        }
        /// <summary>
        /// Cut-paste moves the crowd from source crowd to destination crowd
        /// </summary>
        [TestMethod]
        public void CutAndPasteCrowd_MovesCrowdFromSourceCrowdToPastedCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection[2] as CrowdModel; // The Narrows is selected
            characterExplorerViewModel.SelectedCrowdParent = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = null;// Will cut the crowd
            characterExplorerViewModel.CutCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // League of Shadows is selected
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var linkedCrowd = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(cr => cr.Name == "The Narrows").FirstOrDefault();
            Assert.IsNotNull(linkedCrowd); // The Narrows has been linked with League of Shadows
            var cutCrowd = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection.Where(cr => cr.Name == "The Narrows").FirstOrDefault();
            Assert.IsNull(cutCrowd);
        
        }
        /// <summary>
        /// Crowd in the main collection, if cut, should not be moved, rather linked, to destination crowd
        /// </summary>
        [TestMethod]
        public void CutAndPasteCrowd_LinksCrowdFromMainCollectionToPastedCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["The Narrows"]; // The Narrows is selected
            characterExplorerViewModel.SelectedCrowdParent = null;
            characterExplorerViewModel.SelectedCrowdMemberModel = null;// Will cut the crowd
            characterExplorerViewModel.CutCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // League of Shadows is selected
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var linkedCrowd = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(cr => cr.Name == "The Narrows").FirstOrDefault();
            Assert.IsNotNull(linkedCrowd); // The Narrows has been linked with League of Shadows
            var cutCrowd = characterExplorerViewModel.CrowdCollection.Where(cr => cr.Name == "The Narrows").FirstOrDefault();
            Assert.IsNotNull(cutCrowd); // The Narrows is still on Main Collection
        }
        /// <summary>
        /// Crowd cannot be cut and pasted to itself
        /// </summary>
        [TestMethod]
        public void CutAndPasteCrowd_PreventsPastingWithinSameCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection[2] as CrowdModel; // The Narrows is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.CutCharacterCrowdCommand.Execute(null);
            bool canPaste = characterExplorerViewModel.PasteCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(canPaste);
        }
        /// <summary>
        /// Cannot cut and paste crowd to any of its nested crowds
        /// </summary>
        [TestMethod]
        public void CutAndPasteCrowd_PreventsPastingToAnyNestedCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.CutCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["The Narrows"]; // Will try to paste to child crowd
            bool canPaste = characterExplorerViewModel.PasteCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(canPaste);
        }
        #endregion

        #region Link and Paste Character/Crowd Tests
        /// <summary>
        /// Link-paste will result in copying the character to the destination crowd instead of it removing it from source crowd
        /// </summary>
        [TestMethod]
        public void LinkAndPasteCharacter_CopiesCharacterToPastedCrowdWithoutRemovingFromSourceCrowd() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to link
            characterExplorerViewModel.LinkCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // Will paste to League of Shadows
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var character = characterExplorerViewModel.CrowdCollection["League of Shadows"].CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault(); // Should not be cloned
            Assert.IsNull(character);
            character = characterExplorerViewModel.CrowdCollection["League of Shadows"].CrowdMemberCollection.Where(c => c.Name == "Batman").FirstOrDefault(); // Should be there as a reference
            Assert.IsNotNull(character);
            character = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection.Where(c => c.Name == "Batman").FirstOrDefault(); // Batman should still be in Gotham
            Assert.IsNotNull(character);
        }
        /// <summary>
        /// If a character or crowd is already within a crowd, the link-paste will not do anything
        /// </summary>
        [TestMethod]
        public void LinkAndPasteCharacterCrowd_DoesNotPasteCharacterOrCrowdIfAlreadyExistingInPastingCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // All Characters is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to link
            characterExplorerViewModel.LinkCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Assuming Gotham City is selected
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var linkedChar = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(c => c.Name == "Batman");
            Assert.IsTrue(linkedChar.Count() == 1); // There should still be only one Batman in Gotham
            var clonedChar = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(c => c.Name == "Batman (1)").FirstOrDefault();
            Assert.IsNull(clonedChar); // And no cloning should be done either
            // Now try the same with a crowd
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection[1] as CrowdModel; // The Narrows is selected
            characterExplorerViewModel.SelectedCrowdParent = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = null;// Will link the crowd
            characterExplorerViewModel.LinkCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Gotham City is selected
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var linkedCrowd = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(cr => cr.Name == "The Narrows");
            Assert.IsTrue(linkedCrowd.Count() == 1); // Still should be only one Narrows
            var clonedCrowd = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(cr => cr.Name == "The Narrows (1)").FirstOrDefault();
            Assert.IsNull(clonedCrowd); // And no cloning should be done either
        }
        /// <summary>
        /// The All Characters crowd cannot be pasted to from link
        /// </summary>
        [TestMethod]
        public void LinkAndPasteCharacterCrowd_PreventsPastingToAllCharacterCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // All Characters is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to link
            characterExplorerViewModel.LinkCharacterCrowdCommand.Execute(null);
            bool canPaste = characterExplorerViewModel.PasteCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(canPaste);
            // Now try the same with a crowd
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection[1] as CrowdModel; // The Narrows is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.LinkCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // All Characters is selected
            canPaste = characterExplorerViewModel.PasteCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(canPaste);
        }
        /// <summary>
        /// The All Characters crowd cannot be linked
        /// </summary>
        [TestMethod]
        public void LinkAndPasteCharacterCrowd_PreventsLinkingAllCharacterCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // All Characters is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            bool canCut = characterExplorerViewModel.LinkCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(canCut);
        }
        /// <summary>
        /// Successful link-paste results in repository update
        /// </summary>
        [TestMethod]
        public void LinkAndPasteCharacterCrowd_UpdatesRepository()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Assuming Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;// Selecting Batman to link
            characterExplorerViewModel.LinkCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // Will paste to League of Shadows
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList =>
                        cmList.Where(cm => cm.Name == "League of Shadows").First().CrowdMemberCollection.Where(c => c.Name == "Batman").FirstOrDefault() != null)));
        }
        /// <summary>
        /// Crowd, if linked, should only be copied as a reference to destination crowd and should not be removed from source crowd
        /// </summary>
        [TestMethod]
        public void LinkAndPasteCrowd_CopiesCrowdToPastedCrowdWithoutRemovingFromSourceCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection[2] as CrowdModel; // The Narrows is selected
            characterExplorerViewModel.SelectedCrowdParent = null;
            characterExplorerViewModel.SelectedCrowdMemberModel = null;// Will link the crowd
            characterExplorerViewModel.LinkCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["League of Shadows"]; // League of Shadows is selected
            characterExplorerViewModel.PasteCharacterCrowdCommand.Execute(null);
            var linkedCrowd = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection.Where(cr => cr.Name == "The Narrows").FirstOrDefault();
            Assert.IsNotNull(linkedCrowd); // The Narrows has been linked with League of Shadows
            var originalCrowd = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection.Where(cr => cr.Name == "The Narrows").FirstOrDefault();
            Assert.IsNotNull(originalCrowd); // The Narrows is still inside Gotham City
        }
        /// <summary>
        /// Crowd cannot be linked and pasted to itself
        /// </summary>
        [TestMethod]
        public void LinkAndPasteCrowd_PreventsPastingWithinSameCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"].CrowdMemberCollection[2] as CrowdModel; // The Narrows is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.LinkCharacterCrowdCommand.Execute(null);
            bool canPaste = characterExplorerViewModel.PasteCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(canPaste);
        }
        /// <summary>
        /// Cannot link and paste crowd to any of its nested crowds
        /// </summary>
        [TestMethod]
        public void LinkAndPasteCrowd_PreventsPastingToAnyNestedCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"]; // Gotham City is selected
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.LinkCharacterCrowdCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["The Narrows"]; // Will try to paste to child crowd
            bool canPaste = characterExplorerViewModel.PasteCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(canPaste);
        }
        #endregion

        #region Spawn Character In Crowd Tests
        public void SpawnCharacterInCrowd_AssignsLabelWithBothCharacterAndCrowdName() { }

        #endregion

        #region Save Placement of Character Tests
        /// <summary>
        /// Character's current position in the game should be saved
        /// </summary>
        [TestMethod]
        public void SavePositionForCharacter_SavesPositionOfCharacterForCrowdMember()
        {
            InitializeCrowdRepositoryMockWithDefaultList();

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, keyEventHandlerMock.Object, new Mock<IHCSIntegrator>().Object, eventAggregator);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;
            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            Assert.IsNull(character.SavedPosition);
            rosterExplorerViewModel.SpawnCommand.Execute(null);
            rosterExplorerViewModel.SavePositionCommand.Execute(null);
            Assert.IsNotNull(character.SavedPosition);
        }
        /// <summary>
        /// The containing Roster crowd should also have the character's saved position within the crowd for future use
        /// </summary>
        [TestMethod]
        public void SavePositionForCharacter_SavesPositionOfCharacterInCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, keyEventHandlerMock.Object, new Mock<IHCSIntegrator>().Object, eventAggregator);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;
            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            CrowdModel crModel = character.RosterCrowd as CrowdModel;
            bool keyExists = crModel.SavedPositions.ContainsKey(character.Name);
            Assert.IsFalse(keyExists);
            rosterExplorerViewModel.SpawnCommand.Execute(null);
            rosterExplorerViewModel.SavePositionCommand.Execute(null);
            var position = crModel.SavedPositions[character.Name];
            Assert.IsNotNull(position);
        }
        /// <summary>
        /// Repository should be updated after save position
        /// </summary>
        [TestMethod]
        public void SavePositionForCharacter_UpdatesRepository()
        { 
            InitializeCrowdRepositoryMockWithDefaultList();

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, keyEventHandlerMock.Object, new Mock<IHCSIntegrator>().Object, eventAggregator);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;
            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);
            rosterExplorerViewModel.SavePositionCommand.Execute(null);

            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList =>
                        cmList.Where(cm => cm.Name == "Gotham City").First().SavedPositions.ContainsKey("Batman"))));
        }
        /// <summary>
        /// User should not be able to save position of a character without having it spawned in the game
        /// </summary>
        [TestMethod]
        public void SavePositionForCharacter_PreventsSavingWithoutSpawning()
        {
            InitializeCrowdRepositoryMockWithDefaultList();

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, keyEventHandlerMock.Object, new Mock<IHCSIntegrator>().Object, eventAggregator);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;
            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            bool canSavePosition = rosterExplorerViewModel.SavePositionCommand.CanExecute(null);
            Assert.IsFalse(canSavePosition);
        }

        #endregion

        #region Place Character Tests
        /// <summary>
        /// If the character is under no crowd crowd in roster, it should be placed using its saved position
        /// </summary>
        [TestMethod]
        public void PlaceCharacter_MovesCharacterToPositionBasedOnSavedLocationIfCharacterIsInNoCrowd() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, keyEventHandlerMock.Object, new Mock<IHCSIntegrator>().Object, eventAggregator);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME]; // Selected crowd is All Characters
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel; // Selecting Batman
            characterExplorerViewModel.SelectedCrowdMemberModel.SavedPosition = GetRandomPosition(); // Assigning a saved position for Batman
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;
            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.PlaceCommand.Execute(null);
            Assert.IsNotNull(character.Position); // position should be cloned from saved position
            Assert.AreEqual(character.Position.X, character.SavedPosition.X);
            Assert.AreEqual(character.Position.Y, character.SavedPosition.Y);
            Assert.AreEqual(character.Position.Z, character.SavedPosition.Z);
        }
        /// <summary>
        /// If a character is not under no crowd crowd in roster but in another crowd, it should be placed using its saved position within that crowd
        /// </summary>
        [TestMethod]
        public void PlaceCharacter_MovesCharacterToPositionBasedOnSavedLocationInCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, keyEventHandlerMock.Object, new Mock<IHCSIntegrator>().Object, eventAggregator);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel; // Selecting Batman from Gotham City
            characterExplorerViewModel.SelectedCrowdModel.SavedPositions.Add("Batman", GetRandomPosition(-500));// Saving a position for Batman within Gotham
            characterExplorerViewModel.SelectedCrowdMemberModel.SavedPosition = GetRandomPosition(50000); // Also assigning a saved position for Batman
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;
            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.PlaceCommand.Execute(null);
            Assert.IsNotNull(character.Position);
            // Position should not be equal to saved position of the character
            Assert.AreNotEqual(character.Position.X, character.SavedPosition.X);
            Assert.AreNotEqual(character.Position.Y, character.SavedPosition.Y);
            Assert.AreNotEqual(character.Position.Z, character.SavedPosition.Z);
            // Position should be equal to what was saved in this crowd for the character
            CrowdModel rosterCrowd = character.RosterCrowd as CrowdModel;
            Assert.AreEqual(character.Position.X, rosterCrowd.SavedPositions[character.Name].X);
            Assert.AreEqual(character.Position.Y, rosterCrowd.SavedPositions[character.Name].Y);
            Assert.AreEqual(character.Position.Z, rosterCrowd.SavedPositions[character.Name].Z);
        }
        /// <summary>
        /// If multiple characters are selected for placing, the characters that don't have any saved positions within their respective crowds would only be spawned
        /// </summary>
        [TestMethod]
        public void PlaceCharacter_SpawnsTheCharacterOnlyIfThereIsNoSavedLocationInCrowdInCaseOfMultipleCharacterSelection()
        {
            InitializeCrowdRepositoryMockWithDefaultList();

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, keyEventHandlerMock.Object, new Mock<IHCSIntegrator>().Object, eventAggregator);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[1] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdMemberModel.SavedPosition = GetRandomPosition(); // Assigning a saved position for Robin

            CrowdMemberModel character1 = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;
            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character1};
            rosterExplorerViewModel.PlaceCommand.Execute(null);
            // Position of character 1 (Robin) should not be equal to saved position of the character
            Assert.AreNotEqual(character1.Position.X, character1.SavedPosition.X);
            Assert.AreNotEqual(character1.Position.Y, character1.SavedPosition.Y);
            Assert.AreNotEqual(character1.Position.Z, character1.SavedPosition.Z);
            // Position of character 1 (Robin) should be the default position as the character is just spawned
            Assert.AreEqual(character1.Position.X, 0.0);
            Assert.AreEqual(character1.Position.Y, 0.0);
            Assert.AreEqual(character1.Position.Z, 0.0);
        }
        /// <summary>
        /// If a character has no saved position within its crowd and the crowd is not no crowd crowd, the character cannot be placed
        /// </summary>
        [TestMethod]
        public void PlaceCharacter_PreventsPlacingIfThereIsNoSavedLocationInCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, keyEventHandlerMock.Object, new Mock<IHCSIntegrator>().Object, eventAggregator);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[1] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character1 = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;
            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character1 };
            bool canPlace = rosterExplorerViewModel.PlaceCommand.CanExecute(null);
            Assert.IsFalse(canPlace);
        }
        /// <summary>
        /// If multiple characters are placed together, the ones that have saved positions in the crowd will be placed, others will just be spawned
        /// </summary>
        [TestMethod]
        public void PlaceCharacters_SpawnsOrPlacesMultipleCharactersInTheCrowdAccordingToTheirSavedPositionsInTheCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, keyEventHandlerMock.Object, new Mock<IHCSIntegrator>().Object, eventAggregator);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection["Gotham City"];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[1] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdMemberModel.SavedPosition = GetRandomPosition(); // Assigning a saved position for Robin
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);
            characterExplorerViewModel.SelectedCrowdModel.SavedPositions.Add("Batman", GetRandomPosition(-100));// Saving a position for Batman within Gotham
            characterExplorerViewModel.SelectedCrowdMemberModel.SavedPosition = GetRandomPosition(10000); // Also assigning a different saved position for Batman

            CrowdMemberModel character1 = rosterExplorerViewModel.Participants[1] as CrowdMemberModel;
            CrowdMemberModel character2 = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;
            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character1, character2 };
            rosterExplorerViewModel.PlaceCommand.Execute(null);
            // Position of character 1 (Robin) should not be equal to saved position of the character
            Assert.AreNotEqual(character1.Position.X, character1.SavedPosition.X);
            Assert.AreNotEqual(character1.Position.Y, character1.SavedPosition.Y);
            Assert.AreNotEqual(character1.Position.Z, character1.SavedPosition.Z);
            // Position of character 1 (Robin) should be the default position as the character is just spawned
            Assert.AreEqual(character1.Position.X, 0.0);
            Assert.AreEqual(character1.Position.Y, 0.0);
            Assert.AreEqual(character1.Position.Z, 0.0);
            // Position of character 2 (Batman) should not be equal to saved position of the character
            Assert.AreNotEqual(character2.Position.X, character2.SavedPosition.X);
            Assert.AreNotEqual(character2.Position.Y, character2.SavedPosition.Y);
            Assert.AreNotEqual(character2.Position.Z, character2.SavedPosition.Z);
            // Position of character 2 (Batman) should be equal to saved position of the character inside the crowd
            CrowdModel rosterCrowd = character2.RosterCrowd as CrowdModel;
            Assert.AreEqual(character2.Position.X, rosterCrowd.SavedPositions[character2.Name].X);
            Assert.AreEqual(character2.Position.Y, rosterCrowd.SavedPositions[character2.Name].Y);
            Assert.AreEqual(character2.Position.Z, rosterCrowd.SavedPositions[character2.Name].Z);
        }

        #endregion

        #region Command Delegation in Characters from Crowd Tests
        public void ExecutingSpawnOrSaveOrPlaceOrRemoveOnACrowd_ActivatesTheCommandOnAllCrowdmembersInCrowd() { }

        #endregion
    }

    // =========================================================================
    // STORY: Folder-Based Crowd Repository
    // Each crowd lives in its own file inside data/crowds/.
    // Load merges all files. Save writes per-crowd file. Delete removes the file.
    // =========================================================================
    [TestClass]
    public class TestFolderBasedCrowdRepository : BaseCrowdTest
    {
        private string crowdsFolderPath;
        private CrowdRepository crowdRepository;

        // ── Helpers ───────────────────────────────────────────────────────────

        private string given_crowds_folder_exists()
        {
            string folder = Path.Combine(Path.GetTempPath(), "hvt_crowds_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return folder;
        }

        private void given_crowd_file_in_folder(string folder, string crowdName, List<CrowdMemberModel> members)
        {
            var crowd = new CrowdModel { Name = crowdName };
            foreach (var m in members)
                crowd.Add(m);
            var crowdList = new List<CrowdModel> { crowd };
            string filePath = Path.Combine(folder, crowdName + ".data");
            Helper.SerializeObjectAsJSONToFile(filePath, crowdList);
        }

        private CrowdRepository when_repository_loads_from_folder(string folder)
        {
            var repo = new CrowdRepository();
            repo.CrowdsFolderPath = folder;
            return repo;
        }

        private List<CrowdModel> when_get_crowd_collection(CrowdRepository repo)
        {
            List<CrowdModel> result = null;
            var done = new AutoResetEvent(false);
            repo.GetCrowdCollection(crowds => { result = crowds; done.Set(); });
            done.WaitOne(5000);
            return result;
        }

        private void when_save_crowd_collection(CrowdRepository repo, List<CrowdModel> crowds)
        {
            var done = new AutoResetEvent(false);
            repo.SaveCrowdCollection(() => done.Set(), crowds);
            done.WaitOne(5000);
        }

        private void then_folder_contains_file_for_crowd(string folder, string crowdName)
        {
            string expected = Path.Combine(folder, crowdName + ".data");
            Assert.IsTrue(File.Exists(expected),
                "Expected crowd file not found: " + expected);
        }

        private void then_folder_does_not_contain_file_for_crowd(string folder, string crowdName)
        {
            string expected = Path.Combine(folder, crowdName + ".data");
            Assert.IsFalse(File.Exists(expected),
                "Deleted crowd file still exists: " + expected);
        }

        private void cleanup(string folder)
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }

        // ── Tests (RED — will fail until CrowdRepository.CrowdsFolderPath is implemented) ──

        [TestMethod]
        public void GetCrowdCollection_LoadsAllCrowdsFromCrowdsFolder()
        {
            // Given: crowds folder with two crowd files
            string folder = given_crowds_folder_exists();
            given_crowd_file_in_folder(folder, "Armageddons",  new List<CrowdMemberModel> { new CrowdMemberModel { Name = "Pre-Emptive Strike" } });
            given_crowd_file_in_folder(folder, "Mob Flattened", new List<CrowdMemberModel> { new CrowdMemberModel { Name = "Hellion Boss" } });

            // When: repository loads from folder
            var repo = when_repository_loads_from_folder(folder);
            var crowds = when_get_crowd_collection(repo);

            // Then: both crowds are present in the merged collection
            Assert.IsNotNull(crowds);
            Assert.IsTrue(crowds.Any(c => c.Name == "Armageddons"),   "Armageddons crowd missing");
            Assert.IsTrue(crowds.Any(c => c.Name == "Mob Flattened"), "Mob Flattened crowd missing");
            Assert.IsTrue(crowds.First(c => c.Name == "Armageddons").CrowdMemberCollection.Any(m => m.Name == "Pre-Emptive Strike"));

            cleanup(folder);
        }

        [TestMethod]
        public void GetCrowdCollection_ReturnsEmptyListWhenCrowdsFolderIsEmpty()
        {
            // Given: an empty crowds folder
            string folder = given_crowds_folder_exists();

            // When
            var repo = when_repository_loads_from_folder(folder);
            var crowds = when_get_crowd_collection(repo);

            // Then: empty list, no error
            Assert.IsNotNull(crowds);
            Assert.AreEqual(0, crowds.Count);

            cleanup(folder);
        }

        [TestMethod]
        public void SaveCrowdCollection_WritesOneFilePerCrowdInCrowdsFolder()
        {
            // Given: repository pointing at an empty crowds folder and two crowds
            string folder = given_crowds_folder_exists();
            var repo = when_repository_loads_from_folder(folder);
            var crowd1 = new CrowdModel { Name = "Armageddons" };
            var crowd2 = new CrowdModel { Name = "System Characters" };
            var crowds = new List<CrowdModel> { crowd1, crowd2 };

            // When: collection is saved
            when_save_crowd_collection(repo, crowds);

            // Then: one file per crowd exists in the folder
            then_folder_contains_file_for_crowd(folder, "Armageddons");
            then_folder_contains_file_for_crowd(folder, "System Characters");

            cleanup(folder);
        }

        [TestMethod]
        public void SaveCrowdCollection_DeletesFileWhenCrowdIsRemovedFromCollection()
        {
            // Given: folder already has a crowd file for "Armageddons"
            string folder = given_crowds_folder_exists();
            given_crowd_file_in_folder(folder, "Armageddons", new List<CrowdMemberModel> { new CrowdMemberModel { Name = "Pre-Emptive Strike" } });
            var repo = when_repository_loads_from_folder(folder);

            // When: saved with "Armageddons" absent from the collection
            var crowds = new List<CrowdModel> { new CrowdModel { Name = "System Characters" } };
            when_save_crowd_collection(repo, crowds);

            // Then: Armageddons file is deleted, System Characters file exists
            then_folder_does_not_contain_file_for_crowd(folder, "Armageddons");
            then_folder_contains_file_for_crowd(folder, "System Characters");

            cleanup(folder);
        }

        [TestMethod]
        public void SaveCrowdCollection_UpdatesExistingCrowdFile()
        {
            // Given: folder with an existing crowd file
            string folder = given_crowds_folder_exists();
            given_crowd_file_in_folder(folder, "Armageddons", new List<CrowdMemberModel> { new CrowdMemberModel { Name = "Pre-Emptive Strike" } });
            var repo = when_repository_loads_from_folder(folder);

            // When: same crowd saved with an additional member
            var crowd = new CrowdModel { Name = "Armageddons" };
            crowd.Add(new CrowdMemberModel { Name = "Pre-Emptive Strike" });
            crowd.Add(new CrowdMemberModel { Name = "Suzerain" });
            when_save_crowd_collection(repo, new List<CrowdModel> { crowd });

            // Then: file exists and contains both members
            then_folder_contains_file_for_crowd(folder, "Armageddons");
            var loaded = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(Path.Combine(folder, "Armageddons.data"));
            Assert.AreEqual(2, loaded[0].CrowdMemberCollection.Count);

            cleanup(folder);
        }
    }
#endregion
}
