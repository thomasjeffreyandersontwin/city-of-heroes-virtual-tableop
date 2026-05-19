using Framework.WPF.Library;
using Framework.WPF.Library.Enumerations;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Properties;
using Module.HeroVirtualTabletop.Roster;
using Module.Shared;
using Module.Shared.Messages;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Resources;

namespace Module.HeroVirtualTabletop.Library
{
    public class HeroVirtualTabletopMainViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        Module.Shared.Logging.ILogManager logService = new Module.Shared.Logging.FileLogManager(typeof(HeroVirtualTabletopMainViewModel));
        System.Threading.Timer gameInitializeTimer;

        #endregion

        #region Events

        #endregion

        #region Public Properties

        public IPopupService PopupService
        {
            get { return this.Container.Resolve<IPopupService>(); }
        }

        #endregion

        #region Commands

        #endregion

        #region Constructor

        public HeroVirtualTabletopMainViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator) 
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            gameInitializeTimer = new System.Threading.Timer(gameInitializeTimer_Callback, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            LaunchGame();

            this.eventAggregator.GetEvent<ActivateCharacterEvent>().Subscribe(this.LoadActiveCharacterWidget);
            this.eventAggregator.GetEvent<ActivateGangEvent>().Subscribe(this.LoadActiveGangWidget);
            this.eventAggregator.GetEvent<DeactivateCharacterEvent>().Subscribe(this.CloseActiveCharacterWidget);
            this.eventAggregator.GetEvent<DeactivateGangEvent>().Subscribe(this.CloseActiveGangWidget);
            this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Subscribe(this.ConfigureAttack);
            this.eventAggregator.GetEvent<CloseAttackConfigurationWidgetEvent>().Subscribe(this.CloseAttackConfigurationWidget);
            this.eventAggregator.GetEvent<LoadAttackTargetsSelectionWidgetEvent>().Subscribe(this.LoadTargetSelectionWidget);
            this.eventAggregator.GetEvent<AttackTargetsConfirmedEvent>().Subscribe(this.CloseAttackTargetSelectionWidget);
            this.eventAggregator.GetEvent<ConfigureSweepAttackEvent>().Subscribe(this.ConfigureAttacks);
            this.eventAggregator.GetEvent<LoadAutoFireAttackShotSelectionWidgetEvent>().Subscribe(this.LoadAutoFireAttackConfigurationWidget);
            this.eventAggregator.GetEvent<AutoFireAttackShotsAssignedEvent>().Subscribe(this.CloseAutoFireAttackConfigurationWidget);
            this.eventAggregator.GetEvent<CloseAutoFireAttackShotSelectionWidgetEvent>().Subscribe(this.CloseAutoFireAttackConfigurationWidget);
        }

        #endregion

        #region Methods

        private void LoadMainView()
        {
            System.Windows.Style style = Helper.GetCustomWindowStyle();
            double minheight = 400, minwidth = 80;
            style.Setters.Add(new Setter(Window.MinHeightProperty, minheight));
            style.Setters.Add(new Setter(Window.MinWidthProperty, minwidth));
            CharacterCrowdMainViewModel characterCrowdMainViewModel = this.Container.Resolve<CharacterCrowdMainViewModel>();
            PopupService.ShowDialog("CharacterCrowdMainView", characterCrowdMainViewModel, "", false, ReleaseAllSoundResource, new SolidColorBrush(Colors.Transparent), style);
        }

        private void ReleaseAllSoundResource(CancelEventArgs e)
        {
            this.eventAggregator.GetEvent<StopAllActiveAbilitiesEvent>().Publish(null);
        }
        
        private void LoadActiveCharacterWidget(Tuple<Character, string, string> tuple)
        {
            Character character = tuple.Item1;
            string optionGroupName = tuple.Item2;
            string optionName = tuple.Item3;
            if (character != null && PopupService.IsOpen("ActiveCharacterWidgetView") == false)
            {
                System.Windows.Style style = Helper.GetCustomWindowStyle();
                double minwidth = 80;
                style.Setters.Add(new Setter(Window.MinWidthProperty, minwidth));
                var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
                double left = desktopWorkingArea.Right - 500;
                double top = desktopWorkingArea.Bottom - 80 * character.OptionGroups.Count;
                object savedPos = PopupService.GetPosition("ActiveCharacterWidgetView", character.Name);
                if(savedPos != null)
                {
                    double[] posArray = (double[])savedPos;
                    left = posArray[0];
                    top = posArray[1];
                }
                style.Setters.Add(new Setter(Window.LeftProperty, left));
                style.Setters.Add(new Setter(Window.TopProperty, top));
                ActiveCharacterWidgetViewModel viewModel = this.Container.Resolve<ActiveCharacterWidgetViewModel>();
                PopupService.ShowDialog("ActiveCharacterWidgetView", viewModel, "", false, null, new SolidColorBrush(Colors.Transparent), style, WindowStartupLocation.Manual);
                this.eventAggregator.GetEvent<ActivateCharacterEvent>().Publish(tuple);
                Helper.GlobalVariables_CurrentActiveWindowName = Constants.ACTIVE_CHARACTER_WIDGET;
            }
            else if (character == null && PopupService.IsOpen("ActiveCharacterWidgetView"))
            {
                this.CloseActiveCharacterWidget(null);
            }
        }

        private void LoadActiveGangWidget(List<Character> gangMembers)
        {
            if (gangMembers != null && PopupService.IsOpen("ActiveCharacterWidgetView") == false)
            {
                Character gangLeader = gangMembers.FirstOrDefault(m => m.IsGangLeader);

                System.Windows.Style style = Helper.GetCustomWindowStyle();
                double minwidth = 80;
                style.Setters.Add(new Setter(Window.MinWidthProperty, minwidth));
                var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
                double left = desktopWorkingArea.Right - 500;
                double top = desktopWorkingArea.Bottom - 80 * gangLeader.OptionGroups.Count;
                object savedPos = PopupService.GetPosition("ActiveCharacterWidgetView", gangLeader.Name);
                if (savedPos != null)
                {
                    double[] posArray = (double[])savedPos;
                    left = posArray[0];
                    top = posArray[1];
                }
                style.Setters.Add(new Setter(Window.LeftProperty, left));
                style.Setters.Add(new Setter(Window.TopProperty, top));
                ActiveCharacterWidgetViewModel viewModel = this.Container.Resolve<ActiveCharacterWidgetViewModel>();
                PopupService.ShowDialog("ActiveCharacterWidgetView", viewModel, "", false, null, new SolidColorBrush(Colors.Transparent), style, WindowStartupLocation.Manual);
                this.eventAggregator.GetEvent<ActivateGangEvent>().Publish(gangMembers);
                Helper.GlobalVariables_CurrentActiveWindowName = Constants.ACTIVE_CHARACTER_WIDGET;
            }
        }

        private void CloseActiveGangWidget(object state)
        {
            this.CloseActiveCharacterWidget(null);
        }
        private void ConfigureAttack(Tuple<Attack, List<Character>, Guid> attackTuple)
        {
            if(attackTuple.Item2 != null && attackTuple.Item2.Count > 0)
            {
                ConfigureAttacks(new List<Tuple<Attack, List<Character>, Guid>> { attackTuple });
            }
            else // blank target
            {
                this.eventAggregator.GetEvent<LaunchAttacksEvent>().Publish(null);
            }
        }
        private void ConfigureAttacks(List<Tuple<Attack, List<Character>, Guid>> attacksToConfigure)
        {
            this.LoadAttackConfigurationWidget(attacksToConfigure);
            IntPtr foregroundWindow = WindowsUtilities.FindWindow(null, "MainWindow");
            WindowsUtilities.SetForegroundWindow(foregroundWindow);
        }
        private void LoadAttackConfigurationWidget(List<Tuple<Attack, List<Character>, Guid>> attackList)
        {
            if (attackList != null)
            {
                if (!PopupService.IsOpen("AttackConfigurationView"))
                {
                    System.Windows.Style style = Helper.GetCustomWindowStyle();
                    AttackConfigurationViewModel viewModel = this.Container.Resolve<AttackConfigurationViewModel>();
                    Mouse.OverrideCursor = Cursors.Arrow;
                    PopupService.ShowDialog("AttackConfigurationView", viewModel, "", false, null, new SolidColorBrush(Colors.Transparent), style);
                    Helper.GlobalVariables_CurrentActiveWindowName = Constants.ACTIVE_ATTACK_WIDGET;
                }
                this.eventAggregator.GetEvent<ConfigureAttacksEvent>().Publish(attackList);
            }
            else if (attackList == null && PopupService.IsOpen("AttackConfigurationView"))
            {
                PopupService.CloseDialog("AttackConfigurationView");
                this.eventAggregator.GetEvent<PanelClosedEvent>().Publish(Constants.ACTIVE_ATTACK_WIDGET);
            }
        }

        private void CloseAttackConfigurationWidget(object state)
        {
            PopupService.CloseDialog("AttackConfigurationView");
            this.eventAggregator.GetEvent<PanelClosedEvent>().Publish(Constants.ACTIVE_ATTACK_WIDGET);
        }
        private void CloseActiveCharacterWidget(object state)
        {
            Character target = state as Character;
            PopupService.SavePosition("ActiveCharacterWidgetView", target != null ? target.Name : null);
            PopupService.CloseDialog("ActiveCharacterWidgetView");
            this.eventAggregator.GetEvent<PanelClosedEvent>().Publish(Constants.ACTIVE_CHARACTER_WIDGET);
        }

        private void LoadTargetSelectionWidget(List<Character> potentialTargets)
        {
            if (!PopupService.IsOpen("AttackTargetSelectionView"))
            {
                System.Windows.Style style = Helper.GetCustomWindowStyle();
                AttackTargetSelectionViewModel viewModel = this.Container.Resolve<AttackTargetSelectionViewModel>();
                PopupService.ShowDialog("AttackTargetSelectionView", viewModel, "", false, null, new SolidColorBrush(Colors.Transparent), style);
            }
            this.eventAggregator.GetEvent<AttackTargetsSelectionRequiredEvent>().Publish(potentialTargets);
        }

        private void CloseAttackTargetSelectionWidget(List<Character> confirmedTargets)
        {
            PopupService.CloseDialog("AttackTargetSelectionView");
        }

        private void LoadAutoFireAttackConfigurationWidget(Tuple<Attack, List<Character>, Guid> tuple)
        {
            if (!PopupService.IsOpen("AutoFireAttackConfigurationView"))
            {
                System.Windows.Style style = Helper.GetCustomWindowStyle();
                AutoFireAttackConfigurationViewModel viewModel = this.Container.Resolve<AutoFireAttackConfigurationViewModel>();
                PopupService.ShowDialog("AutoFireAttackConfigurationView", viewModel, "", false, null, new SolidColorBrush(Colors.Transparent), style);
            }
            this.eventAggregator.GetEvent<AssignAutoFireAttackShotsEvent>().Publish(tuple);
        }
        private void CloseAutoFireAttackConfigurationWidget(Object state)
        {
            PopupService.CloseDialog("AutoFireAttackConfigurationView");
        }

        private void CloseAutoFireAttackConfigurationWidget(List<Character> confirmedTargets)
        {
            PopupService.CloseDialog("AutoFireAttackConfigurationView");
        }


        private void LaunchGame()
        {
            bool directoryExists = CheckGameDirectory();
            if (!directoryExists)
                SetGameDirectory();

            LoadMainView();
            //logService.Info("Launching game...");
            //IconInteractionUtility.RunCOHAndLoadDLL(Module.Shared.Settings.Default.CityOfHeroesGameDirectory);
            IconInteractionUtility.InitializeGame(Module.Shared.Settings.Default.CityOfHeroesGameDirectory);
            gameInitializeTimer.Change(50, System.Threading.Timeout.Infinite);
        }

        private void DoPostGameLaunchOperations()
        {
            Action d = delegate(){
                //logService.Info("Game launched");
                LoadRequiredKeybinds();
                //logService.Info("Keybinds loaded");
                CreateCameraFilesIfNotExists();
                CreateAreaAttackPopupMenuIfNotExists();

                LoadModelsFile();  
                //logService.Info("Models loaded");

                LoadCostumeFiles();
                //logService.Info("Costumes loaded");

                LoadSoundFiles();
                //logService.Info("Sounds loaded");

                ClearTempFilesFromDataFolder();

                //LoadSoundFiles();

                // Load camera on start
                new Camera().Render();
                //logService.Info("Camera rendered");

                //LoadMainView();
                //logService.Info("MainView displayed");
                this.eventAggregator.GetEvent<GameLoadedEvent>().Publish(null);
            };
            Application.Current.Dispatcher.BeginInvoke(d);
        }

        private void gameInitializeTimer_Callback(object state)
        {
            bool gameLoaded = IconInteractionUtility.IsGameLoaded();
            if(gameLoaded)
            {
                System.Threading.Thread.Sleep(1000);
                IconInteractionUtility.DoPostInitialization();
                DoPostGameLaunchOperations();
                gameInitializeTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
            else
            {
                gameInitializeTimer.Change(100, System.Threading.Timeout.Infinite);
            }
        }

        
        private void LoadRequiredKeybinds()
        {
            CheckRequiredKeybindsFileExists();

            GameCommandExecution.ExecuteCmd("bindloadfile required_keybinds.txt");
        }

        private bool CheckGameDirectory()
        {
            bool directoryExists = false;
            string gameDir = Module.Shared.Settings.Default.CityOfHeroesGameDirectory;
            //logService.Info("Current Game Dir: " + gameDir);
            if (!string.IsNullOrEmpty(gameDir) && Directory.Exists(gameDir) && File.Exists(Path.Combine(gameDir, Constants.GAME_EXE_FILENAME)))
            {
                directoryExists = true;
            }
            //logService.Info("Dir Exists: " + directoryExists.ToString());
            return directoryExists;
        }

        private void SetGameDirectory()
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.ShowNewFolderButton = false;
            dialog.Description = Messages.SELECT_GAME_DIRECTORY_MESSAGE;
            while (true)
            {
                System.Windows.Forms.DialogResult dr = dialog.ShowDialog();
                if (dr == System.Windows.Forms.DialogResult.OK && Directory.Exists(dialog.SelectedPath))
                {
                    if (File.Exists(Path.Combine(dialog.SelectedPath, Constants.GAME_EXE_FILENAME)))
                    {
                        Module.Shared.Settings.Default.CityOfHeroesGameDirectory = dialog.SelectedPath;
                        Module.Shared.Settings.Default.Save();
                        break;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(Constants.INVALID_GAME_DIRECTORY_MESSAGE, Constants.INVALID_DIRECTORY_CAPTION, MessageBoxButton.OK);
                    }
                }
            }

        }

        private void CheckRequiredKeybindsFileExists()
        {
            bool directoryExists = CheckGameDirectory();
            if (!directoryExists)
                SetGameDirectory();

            string dataDir = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME);
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            string filePath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_KEYBINDS_FILENAME);
            string filePathAlt = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_KEYBINDS_ALT_FILENAME);
            if (!File.Exists(filePath) || !File.Exists(filePathAlt))
            {
                ExtractRequiredKeybindsFile();
            }
        }

        private void ExtractRequiredKeybindsFile()
        {
            File.AppendAllText(
                Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_KEYBINDS_FILENAME),
                Resources.required_keybinds
                );
            File.AppendAllText(
                Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_KEYBINDS_ALT_FILENAME),
                Resources.required_keybinds_alt
                );
        }

        private void LoadModelsFile()
        {
            string filePath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_MODELS_FILENAME);
            if (!File.Exists(filePath))
            {
                File.AppendAllText(
                filePath, Resources.Models
                );
            }
        }

        private void LoadSoundFiles()
        {
            string folderPath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_SOUND_FOLDERNAME);
            if (Directory.Exists(folderPath)) 
            {
                return;
            }
            else
            {
                //var resourceName = "Module.HeroVirtualTabletop.Resources.sound.zip";
                //var assembly = Assembly.GetExecutingAssembly();
                //using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                //using (StreamReader reader = new StreamReader(stream))
                //{
                //    ZipArchive archive = new ZipArchive(reader.BaseStream);
                //    archive.ExtractToDirectory(Module.Shared.Settings.Default.CityOfHeroesGameDirectory);
                //}
            }
        }

        private void LoadCostumeFiles()
        {
            string folderPath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME);
            if (Directory.Exists(folderPath))
            {
                return;
            }
            else
            {
                var resourceName = "Module.HeroVirtualTabletop.Resources.costumes.zip";
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    ZipArchive archive = new ZipArchive(reader.BaseStream);
                    archive.ExtractToDirectory(Module.Shared.Settings.Default.CityOfHeroesGameDirectory);
                }

                
            }
        }

        private void CreateAreaAttackPopupMenuIfNotExists()
        {
            string dirTexts = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_TEXTS_FOLDERNAME);
            if (!Directory.Exists(dirTexts))
                Directory.CreateDirectory(dirTexts);
            string dirLanguage = Path.Combine(dirTexts, Constants.GAME_LANGUAGE_FOLDERNAME);
            if (!Directory.Exists(dirLanguage))
                Directory.CreateDirectory(dirLanguage);
            string dirMenus = Path.Combine(dirLanguage, Constants.GAME_MENUS_FOLDERNAME);
            if (!Directory.Exists(dirMenus))
                Directory.CreateDirectory(dirMenus);
            string fileAreaAttackMenu = Path.Combine(dirMenus, Constants.GAME_ATTACK_MENU_FILENAME);
            var assembly = Assembly.GetExecutingAssembly();
            
            if (!File.Exists(fileAreaAttackMenu))
            {
                var resourceName = "Module.HeroVirtualTabletop.Resources.attack.mnu";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.AppendAllText(
                        fileAreaAttackMenu, result
                        );
                }
            }
        }

        private void CreateCameraFilesIfNotExists()
        {
            string dirData = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME);

            string enableCameraFile = Path.Combine(dirData, Constants.GAME_ENABLE_CAMERA_FILENAME);
            string disableCameraFile = Path.Combine(dirData, Constants.GAME_DISABLE_CAMERA_FILENAME);

            var assembly = Assembly.GetExecutingAssembly();

            if (!File.Exists(enableCameraFile))
            {
                var resourceName = "Module.HeroVirtualTabletop.Resources.enable_camera.txt";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.AppendAllText(
                        enableCameraFile, result
                        );
                }
            }

            if (!File.Exists(disableCameraFile))
            {
                var resourceName = "Module.HeroVirtualTabletop.Resources.disable_camera.txt";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.AppendAllText(
                        disableCameraFile, result
                        );
                }
            }
        }

        private void ClearTempFilesFromDataFolder()
        {
            string dirPath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME);
            System.IO.DirectoryInfo di = new DirectoryInfo(dirPath);

            foreach (FileInfo file in di.GetFiles())
            {
                if(file.Name.Contains(Constants.DEFAULT_DELIMITING_CHARACTER_TRANSLATION) || file.Name.Contains(Constants.DEFAULT_DELIMITING_CHARACTER_TRANSLATION))
                {
                    file.Delete();
                }
            }
        }

        #endregion
    }
}
