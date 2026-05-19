using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Sevices;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Module.UnitTest
{
    public class BaseCrowdTest : BaseTest
    {
        protected Mock<ICrowdRepository> crowdRepositoryMock = new Mock<ICrowdRepository>();
        protected Mock<ITargetObserver> targetObserverMock = new Mock<ITargetObserver>();
        protected Mock<IDesktopKeyEventHandler> keyEventHandlerMock = new Mock<IDesktopKeyEventHandler>();
        protected List<CrowdModel> crowdModelList;
        protected CharacterExplorerViewModel characterExplorerViewModel;

        public BaseCrowdTest() : base()
        {
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, keyEventHandlerMock.Object, eventAggregator);
        }

        protected void InitializeCrowdRepositoryMockWithDefaultList()
        {
            InitializeCrowdRepositoryMockWithListAndSynchViewModel(this.crowdModelList);
        }

        protected void InitializeCrowdRepositoryMockWithListAndSynchViewModel(List<CrowdModel> crowdModelList)
        {
            crowdRepositoryMock.Reset();
            this.crowdRepositoryMock
               .Setup(repository => repository.GetCrowdCollection(It.IsAny<Action<List<CrowdModel>>>()))
               .Callback((Action<List<CrowdModel>> action) => action(crowdModelList));
            this.crowdRepositoryMock
               .Setup(repository => repository.SaveCrowdCollection(It.IsAny<Action>(), It.IsAny<List<CrowdModel>>()))
               .Callback((Action action, List<CrowdModel> cm) => action());
            characterExplorerViewModel.CrowdCollection = new HashedObservableCollection<CrowdModel, string>(crowdModelList,
                 (CrowdModel c) => { return c.Name; }, (CrowdModel c) => { return c.Order; }, (CrowdModel c) => { return c.Name; }
                 );
        }

        protected void InitializeMessageBoxService(MessageBoxResult messageBoxResult)
        {
            this.messageBoxServiceMock
                .Setup(messageboxService => messageboxService.ShowDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()))
                .Returns(messageBoxResult);
        }

        protected void InitializeDefaultList(bool nestCrowd = false)
        {
            CrowdModel crowdAllChars = new CrowdModel { Name = "All Characters", Order = -1 };
            CrowdModel crowd1 = new CrowdModel { Name = "Gotham City" };
            CrowdMemberModel crowdMember1 = new CrowdMemberModel { Name = "Batman" };
            crowd1.SavedPositions = new Dictionary<string, IMemoryElementPosition>();
            CrowdModel childCrowd = new CrowdModel { Name = "The Narrows"};
            CrowdMemberModel crowdMember2 = new CrowdMemberModel { Name = "Scarecrow"};
            crowd1.Add(crowdMember1);
            crowd1.Add(childCrowd);
            childCrowd.Add(crowdMember2);
            CrowdMemberModel crowdMember4 = new CrowdMemberModel() { Name = "Robin" };
            crowd1.Add(crowdMember4);
            CrowdModel crowd2 = new CrowdModel { Name = "League of Shadows" };
            CrowdMemberModel crowdMember3 = new CrowdMemberModel { Name = "Ra'as Al Ghul"};
            crowd2.Add(crowdMember3);
            if (nestCrowd)
                crowd2.Add(childCrowd);
            crowdAllChars.Add(new List<ICrowdMemberModel>() { crowdMember1, crowdMember2, crowdMember3, crowdMember4 }); 
            this.crowdModelList = new List<CrowdModel> { crowdAllChars, crowd1, crowd2, childCrowd };
        }

        protected IMemoryElementPosition GetRandomPosition(int seed = 0)
        {
            Random rand = seed == 0 ? new Random() : new Random(seed);
            Mock<IMemoryElementPosition> position = new Mock<IMemoryElementPosition>();
            Random random = new Random(rand.Next());
            position.Setup(p => p.X).Returns(random.Next(-1000, 1000));
            position.Setup(p => p.Y).Returns(random.Next(-1000, 1000));
            position.Setup(p => p.Z).Returns(random.Next(-1000, 1000));

            Mock<IMemoryElementPosition> clonedPosition = new Mock<IMemoryElementPosition>();
            clonedPosition.Setup(cp => cp.X).Returns(position.Object.X);
            clonedPosition.Setup(cp => cp.Y).Returns(position.Object.Y);
            clonedPosition.Setup(cp => cp.Z).Returns(position.Object.Z);
            position.Setup(p => p.Clone(It.IsAny<bool>(), It.IsAny<uint>())).Returns(clonedPosition.Object);

            return position.Object;
        }

        protected void DeleteTempRepositoryFile(string path = "test.data")
        {
            File.Delete(path);
        }

        protected int numberOfItemsFound = 0;
        protected void CountNumberOfCrowdMembersByName(List<ICrowdMemberModel> collection, string name)
        {
            foreach (ICrowdMember bcm in collection)
            {
                if (bcm.Name == name)
                    numberOfItemsFound++;
                if (bcm is CrowdModel)
                {
                    CrowdModel cm = bcm as CrowdModel;
                    if (cm.CrowdMemberCollection != null && cm.CrowdMemberCollection.Count > 0)
                    {
                        CountNumberOfCrowdMembersByName(cm.CrowdMemberCollection.ToList(), name);
                    }
                }
            }
        }

        protected List<ICrowdMemberModel> GetFlattenedMemberList(List<ICrowdMemberModel> list)
        {
            List<ICrowdMemberModel> _list = new List<ICrowdMemberModel>();
            foreach (ICrowdMemberModel cm in list)
            {
                if (cm is CrowdModel)
                {
                    CrowdModel crm = (cm as CrowdModel);
                    if (crm.CrowdMemberCollection != null && crm.CrowdMemberCollection.Count > 0)
                        _list.AddRange(GetFlattenedMemberList(crm.CrowdMemberCollection.ToList()));
                }
                _list.Add(cm);
            }
            return _list;
        }

        /// <summary>WPF controls such as TextBox require STA; unit tests often run on MTA.</summary>
        protected static void RunOnStaThread(Action action)
        {
            ExceptionDispatchInfo captured = null;
            Thread t = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (captured != null)
                captured.Throw();
        }

    }
}
