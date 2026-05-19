using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Module.HeroVirtualTabletop.Crowds;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Library.Events;
using Microsoft.Xna.Framework;

using Module.Shared;
using Module.Shared.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.Shared.Events;
using Module.HeroVirtualTabletop.Library.Utility;

namespace Module.HeroVirtualTabletop.Desktop
{
    public enum ContextMenuEvent
    {
        AreaAttackContextMenuDisplayed,
        DefaultContextMenuDisplayed,
        AttackTargetMenuItemSelected,
        AttackTargetAndExecuteMenuItemSelected,
        AttackTargetAndExecuteCrowdMenuItemSelected,
        AttackExecuteSweepMenuItemSelected,
        AbortMenuItemSelected,
        SpawnMenuItemSelected,
        PlaceMenuItemSelected,
        SavePositionMenuItemSelected,
        MoveCameraToTargetMenuItemSelected,
        MoveTargetToCameraMenuItemSelected,
        ResetOrientationMenuItemSelected,
        ManueverWithCameraMenuItemSelected,
        ActivateMenuItemSelected,
        ActivateCrowdAsGangMenuItemSelected,
        ClearFromDesktopMenuItemSelected,
        CloneAndLinkMenuItemSelected,
        MoveTargetToCharacterMenuItemSelected,
        ActivateCharacterOptionMenuItemSelected,
        SpreadNumberSelected
    }
    public class DesktopContextMenu
    {
        public static FileSystemWatcher ContextCommandFileWatcher;
        public CrowdMemberModel Character = null;

        public bool IsDisplayed { get; set; }

        public bool ShowAttackMenu { get; set; }

        public bool IsSequenceViewActive { get; set; }
        public bool IsSweepAttackInProgress { get; set; }
        public bool ShowAttackSpreadMenu { get; set; }
        public List<string> AttackingCharacterNames { get; set; }

        public event EventHandler<CustomEventArgs<Object>> AttackContextMenuDisplayed;
        public event EventHandler<CustomEventArgs<Object>> DefaultContextMenuDisplayed;
        public event EventHandler<CustomEventArgs<Object>> AttackTargetMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> AttackTargetAndExecuteMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> AttackTargetAndExecuteCrowdMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> AttackExecuteSweepMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> AbortMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> SpawnMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> PlaceMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> SavePositionMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> MoveCameraToTargetMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> MoveTargetToCameraMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> ResetOrientationMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> ManueverWithCameraMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> ActivateMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> ActivateCrowdAsGangMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> ClearFromDesktopMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> CloneAndLinkMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> MoveTargetToCharacterMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> ActivateCharacterOptionMenuItemSelected;
        public event EventHandler<CustomEventArgs<Object>> SpreadNumberSelected;
        private void FireContextMenuEvent(ContextMenuEvent contextMenuEvent, object sender, CustomEventArgs<Object> e)
        {
            switch (contextMenuEvent)
            {
                case ContextMenuEvent.AreaAttackContextMenuDisplayed:
                    if (AttackContextMenuDisplayed != null)
                        AttackContextMenuDisplayed(sender, e);
                    break;
                case ContextMenuEvent.DefaultContextMenuDisplayed:
                    if (DefaultContextMenuDisplayed != null)
                        DefaultContextMenuDisplayed(sender, e);
                    break;
                case ContextMenuEvent.AttackTargetMenuItemSelected:
                    if (AttackTargetMenuItemSelected != null)
                        AttackTargetMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.AttackTargetAndExecuteMenuItemSelected:
                    if (AttackTargetAndExecuteMenuItemSelected != null)
                        AttackTargetAndExecuteMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.AttackTargetAndExecuteCrowdMenuItemSelected:
                    if (AttackTargetAndExecuteCrowdMenuItemSelected != null)
                        AttackTargetAndExecuteCrowdMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.AttackExecuteSweepMenuItemSelected:
                    if (AttackExecuteSweepMenuItemSelected != null)
                        AttackExecuteSweepMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.AbortMenuItemSelected:
                    if (AbortMenuItemSelected != null)
                        AbortMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.SpawnMenuItemSelected:
                    if (SpawnMenuItemSelected != null)
                        SpawnMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.PlaceMenuItemSelected:
                    if (PlaceMenuItemSelected != null)
                        PlaceMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.SavePositionMenuItemSelected:
                    if (SavePositionMenuItemSelected != null)
                        SavePositionMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.MoveCameraToTargetMenuItemSelected:
                    if (MoveCameraToTargetMenuItemSelected != null)
                        MoveCameraToTargetMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.MoveTargetToCameraMenuItemSelected:
                    if (MoveTargetToCameraMenuItemSelected != null)
                        MoveTargetToCameraMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.ResetOrientationMenuItemSelected:
                    if (ResetOrientationMenuItemSelected != null)
                        ResetOrientationMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.ManueverWithCameraMenuItemSelected:
                    if (ManueverWithCameraMenuItemSelected != null)
                        ManueverWithCameraMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.ActivateMenuItemSelected:
                    if (ActivateMenuItemSelected != null)
                        ActivateMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.ActivateCrowdAsGangMenuItemSelected:
                    if (ActivateCrowdAsGangMenuItemSelected != null)
                        ActivateCrowdAsGangMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.ClearFromDesktopMenuItemSelected:
                    if (ClearFromDesktopMenuItemSelected != null)
                        ClearFromDesktopMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.CloneAndLinkMenuItemSelected:
                    if (CloneAndLinkMenuItemSelected != null)
                        CloneAndLinkMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.MoveTargetToCharacterMenuItemSelected:
                    if (MoveTargetToCharacterMenuItemSelected != null)
                        MoveTargetToCharacterMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.ActivateCharacterOptionMenuItemSelected:
                    if (ActivateCharacterOptionMenuItemSelected != null)
                        ActivateCharacterOptionMenuItemSelected(sender, e);
                    break;
                case ContextMenuEvent.SpreadNumberSelected:
                    if (SpreadNumberSelected != null)
                        SpreadNumberSelected(sender, e);
                    break;
            }
        }

        public DesktopContextMenu()
        {
            if (ContextCommandFileWatcher == null)
            {
                ContextCommandFileWatcher = new FileSystemWatcher();
                ContextCommandFileWatcher.Path = string.Format("{0}\\", Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME));
                ContextCommandFileWatcher.IncludeSubdirectories = false;
                ContextCommandFileWatcher.Filter = "*.txt";
                ContextCommandFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                ContextCommandFileWatcher.Changed += fileSystemWatcher_Changed;
            }
            ContextCommandFileWatcher.EnableRaisingEvents = false;
            CreateBindSaveFilesForContextCommands();
            ContextCommandFileWatcher.EnableRaisingEvents = true;
        }

        public void GenerateAndDisplay(CrowdMemberModel character, List<string> attackingCharacterNames, bool showAttackMenu, bool isSequenceView = false, bool isSweepAttackInProgress = false, bool showAttackSpreadMenu = false)
        {
            Character = character;
            AttackingCharacterNames = attackingCharacterNames;
            ShowAttackMenu = showAttackMenu;
            IsSequenceViewActive = isSequenceView;
            IsSweepAttackInProgress = isSweepAttackInProgress;
            ShowAttackSpreadMenu = showAttackSpreadMenu;
            GenerateAndDisplay();
            ContextCommandFileWatcher.EnableRaisingEvents = true;
        }

        public void GenerateMenu()
        {
            CrowdMemberModel character = Character;
            string fileCharacterMenu = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_TEXTS_FOLDERNAME, Constants.GAME_LANGUAGE_FOLDERNAME, Constants.GAME_MENUS_FOLDERNAME, Constants.GAME_CHARACTER_MENU_FILENAME);
            var assembly = Assembly.GetExecutingAssembly();

            var resourceName = "Module.HeroVirtualTabletop.Resources.character.mnu";
            List<string> menuFileLines = new List<string>();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    menuFileLines.Add(line);
                }

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < menuFileLines.Count - 1; i++)
                {
                    if (menuFileLines[i].StartsWith("Option \"Abort\"") && !IsSequenceViewActive)
                        continue;
                    if (menuFileLines[i].StartsWith("Option \"Execute Sweep\"") && !IsSweepAttackInProgress)
                        continue;
                    sb.AppendLine(menuFileLines[i]);
                }
                if (character.OptionGroups != null && character.OptionGroups.Count > 0)
                {
                    foreach (var optionGroup in character.OptionGroups)
                    {
                        sb.AppendLine(string.Format("Menu \"{0}\"", optionGroup.Name));
                        sb.AppendLine("{");
                        foreach (ICharacterOption option in optionGroup.Options)
                        {
                            string whiteSpaceReplacedOptionGroupName = optionGroup.Name.Replace(" ", Constants.SPACE_REPLACEMENT_CHARACTER);
                            string whiteSpaceReplacedOptionName = option.Name.Replace(" ", Constants.SPACE_REPLACEMENT_CHARACTER);
                            sb.AppendLine(string.Format("Option \"{0}\" \"bind_save_file {1}{2}{3}.txt\"", option.Name, whiteSpaceReplacedOptionGroupName, Constants.DEFAULT_DELIMITING_CHARACTER, whiteSpaceReplacedOptionName));
                        }
                        sb.AppendLine("}");
                    }
                }
                sb.AppendLine(menuFileLines[menuFileLines.Count - 1]);

                File.WriteAllText(
                    fileCharacterMenu, sb.ToString()
                    );
                System.Threading.Thread.Sleep(200); // Delay so that the file write completes before calling the pop menu
            }
        }
        public void DisplayMenu()
        {
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.PopMenu, "character");
            keyBindsGenerator.CompleteEvent();

        }

        public void GenerateAndDisplay()
        {

            if (Character != null)
            {

                if (ShowAttackMenu)
                {
                    if (!AttackingCharacterNames.Contains(Character.Name))
                    {
                        System.Threading.Thread.Sleep(200); // Delay so that the file write completes before calling the pop menu
                        GenerateAttackMenu();
                        DisplayAttackMenu();
                        IsDisplayed = true;
                        FireContextMenuEvent(ContextMenuEvent.AreaAttackContextMenuDisplayed, null, new CustomEventArgs<object> { Value = Character });
                    }
                }
                else
                {
                    GenerateMenu();
                    DisplayMenu();
                    IsDisplayed = true;
                    FireContextMenuEvent(ContextMenuEvent.DefaultContextMenuDisplayed, null, new CustomEventArgs<object> { Value = Character });
                }
            }
        }

        private void GenerateAttackMenu()
        {
            string fileAreaAtackMenu = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_TEXTS_FOLDERNAME, Constants.GAME_LANGUAGE_FOLDERNAME, Constants.GAME_MENUS_FOLDERNAME, Constants.GAME_ATTACK_MENU_FILENAME);
            var assembly = Assembly.GetExecutingAssembly();

            var resourceName = "Module.HeroVirtualTabletop.Resources.attack.mnu";
            List<string> menuFileLines = new List<string>();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    menuFileLines.Add(line);
                }

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < menuFileLines.Count - 1; i++)
                {
                    if (menuFileLines[i].StartsWith("Option \"Abort\"") && !IsSequenceViewActive)
                        continue;
                    else if (menuFileLines[i].StartsWith("Option \"Execute Sweep\"") && !Helper.GlobalDefaultSweepAbility.IsActive)
                        continue;
                    sb.AppendLine(menuFileLines[i]);
                }
                if (ShowAttackSpreadMenu)
                {
                    sb.AppendLine(string.Format("Menu \"Spread to Improve Hitting Chance\""));
                    sb.AppendLine("{");
                    for(int i = 1; i < 11; i ++)
                    {
                        sb.AppendLine(string.Format("Option \"{0}\" \"bind_save_file {1}{2}{3}.txt\"", i, Constants.SPREAD_NUMBER, Constants.DEFAULT_DELIMITING_CHARACTER, i));
                    }
                    sb.AppendLine("}");
                }
                sb.AppendLine(menuFileLines[menuFileLines.Count - 1]);

                File.WriteAllText(
                    fileAreaAtackMenu, sb.ToString()
                    );
                System.Threading.Thread.Sleep(200); // Delay so that the file write completes before calling the pop menu
            }
        }

        private void DisplayAttackMenu()
        {
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.PopMenu, "attack");
            keyBindsGenerator.CompleteEvent();
        }

        private void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Action action = delegate()
            {
                //IsDisplayed = false;

                ContextCommandFileWatcher.EnableRaisingEvents = false;
                switch (e.Name)
                {
                    case Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.AttackTargetMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_EXECUTE_FILENAME: 
                        ContextCommandFileWatcher.EnableRaisingEvents = false;
                        FireContextMenuEvent(ContextMenuEvent.AttackTargetAndExecuteMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_EXECUTE_CROWD_FILENAME:
                        ContextCommandFileWatcher.EnableRaisingEvents = false;
                        FireContextMenuEvent(ContextMenuEvent.AttackTargetAndExecuteCrowdMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_SWEEP_ATTACK_EXECUTE_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.AttackExecuteSweepMenuItemSelected, null, new CustomEventArgs<object> { Value = null});
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_ABORT_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.AbortMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_SPAWN_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.SpawnMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_PLACE_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.PlaceMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_SAVEPOSITION_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.SavePositionMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_MOVECAMERATOTARGET_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.MoveCameraToTargetMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_MOVETARGETTOCAMERA_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.MoveTargetToCameraMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_RESETORIENTATION_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.ResetOrientationMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_MANUEVERWITHCAMERA_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.ManueverWithCameraMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_ACTIVATE_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.ActivateMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_ACTIVATE_CROWD_AS_GANG_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.ActivateCrowdAsGangMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_CLEARFROMDESKTOP_FILENAME:
                        FireContextMenuEvent(ContextMenuEvent.ClearFromDesktopMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                        break;
                    case Constants.GAME_CHARACTER_BINDSAVE_CLONEANDLINK_FILENAME:
                        {
                            FireContextMenuEvent(ContextMenuEvent.CloneAndLinkMenuItemSelected, null, new CustomEventArgs<object> { Value = Character });
                            break;
                        }
                    default:
                        {
                            if (e.Name.StartsWith(Constants.GAME_CHARACTER_BINDSAVE_MOVETARGETTOCHARACTER_FILENAME))
                            {
                                int index = e.Name.IndexOf(Constants.DEFAULT_DELIMITING_CHARACTER);
                                if (index > 0)
                                {
                                    string whiteSpceReplacedCharacterName = e.Name.Substring(index + 1, e.Name.Length - index - 5); // to get rid of the .txt part
                                    string characterName = whiteSpceReplacedCharacterName.Replace(Constants.SPACE_REPLACEMENT_CHARACTER_TRANSLATION, " ");
                                    FireContextMenuEvent(ContextMenuEvent.MoveTargetToCharacterMenuItemSelected, null, new CustomEventArgs<object> { Value = characterName });
                                }
                            }
                            else
                            {
                                int index = e.Name.IndexOf(Constants.DEFAULT_DELIMITING_CHARACTER);
                                if (index > 0)
                                {
                                    if (e.Name.Contains(Constants.SPREAD_NUMBER))
                                    {
                                        string spreadNumberString = e.Name.Substring(index + 1, e.Name.Length - index - 5); // to get rid of the .txt part
                                        int spreadNumber;
                                        if(Int32.TryParse(spreadNumberString, out spreadNumber))
                                        {
                                            FireContextMenuEvent(ContextMenuEvent.SpreadNumberSelected, null, new CustomEventArgs<object> { Value = new object[] { Character, spreadNumber } });
                                        }
                                    }
                                    else
                                    {
                                        string whiteSpaceReplacedOptionGroupName = e.Name.Substring(0, index - 1); // The special characters are translated to two characters, so need to subtract one additional character
                                        string whiteSpceReplacedOptionName = e.Name.Substring(index + 1, e.Name.Length - index - 5); // to get rid of the .txt part
                                        string optionGroupName = whiteSpaceReplacedOptionGroupName.Replace(Constants.SPACE_REPLACEMENT_CHARACTER_TRANSLATION, " ");
                                        string optionName = whiteSpceReplacedOptionName.Replace(Constants.SPACE_REPLACEMENT_CHARACTER_TRANSLATION, " ");
                                        FireContextMenuEvent(ContextMenuEvent.ActivateCharacterOptionMenuItemSelected, null, new CustomEventArgs<object> { Value = new object[] { Character, optionGroupName, optionName } });
                                    }
                                }
                            }

                            break;
                        }

                }
            };
            if (Application.Current != null)
                Application.Current.Dispatcher.BeginInvoke(action);
        }

        public void CreateBindSaveFilesForContextCommands()
        {
            string filePath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_FILENAME);
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
            filePath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_EXECUTE_FILENAME);
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
        }
    }
}
