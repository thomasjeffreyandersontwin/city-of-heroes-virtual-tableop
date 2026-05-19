using Module.Shared;
using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly:InternalsVisibleTo("Module.UnitTest")]
namespace Module.HeroVirtualTabletop.Library.GameCommunicator
{
    public class KeyBindsGenerator
    {
        #region KeyBinds Strings
        internal Dictionary<GameEvent, string> keyBindsStrings = new Dictionary<GameEvent, string>()
        {
            { GameEvent.TargetName , "target_name"},
            { GameEvent.PrevSpawn , "prev_spawn"},
            { GameEvent.NextSpawn , "next_spawn"},
            { GameEvent.RandomSpawn , "random_spawn"},
            { GameEvent.Fly , "fly"},
            { GameEvent.EditPos , "editpos"},
            { GameEvent.DetachCamera , "detach_camera"},
            { GameEvent.NoClip , "noclip"},
            { GameEvent.AccessLevel , "access_level"},
            { GameEvent.Command , "~"},
            { GameEvent.SpawnNpc , "spawn_npc"},
            { GameEvent.Rename , "rename"},
            { GameEvent.LoadCostume , "load_costume"},
            { GameEvent.MoveNPC , "move_npc"},
            { GameEvent.DeleteNPC , "delete_npc"},
            { GameEvent.ClearNPC , "clear_npc"},
            { GameEvent.Move , "mov"},
            { GameEvent.TargetEnemyNear , "target_enemy_near"},
            { GameEvent.LoadBind , "load_bind"},
            { GameEvent.BeNPC , "benpc"},
            { GameEvent.SaveBind , "save_bind"},
            { GameEvent.GetPos , "getpos"},
            { GameEvent.CamDist , "camdist"},
            { GameEvent.Follow , "follow"},
            { GameEvent.LoadMap , "load_map"},
            { GameEvent.BindLoadFile , "bind_load_file"},
            { GameEvent.Macro , "macro"},
            { GameEvent.PopMenu , "popmenu" },
            { GameEvent.NOP , "nop" }
        };
        #endregion

        private string directory;

        private readonly IGameCommandExecutor _gameCommandExecutor;

        public KeyBindsGenerator()
            : this(GameCommandExecution.ActiveExecutor)
        {
        }

        public KeyBindsGenerator(IGameCommandExecutor gameCommandExecutor)
        {
            _gameCommandExecutor = gameCommandExecutor ?? new HookCostumeGameCommandExecutor();

            directory = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bindFile = Path.Combine(directory, loaderKey + ".txt");
        }

        private string bindFile;

        public string BindFile
        {
            get
            {
                return bindFile;
            }
        }

        private string triggerKey = "Y";

        public string TriggerKey
        {
            get
            {
                return triggerKey;
            }
        }

        private string loaderKey = "B";


        private static GameEvent lastEvent;
        private static string generatedKeybindText;

        private static string lastKeyBindGenerated;
        public static string LastKeyBindsGenerated
        {
            get
            {
                return lastKeyBindGenerated;
            }
        }

        protected internal static List<string> generatedKeybinds = new List<string>();

        public string GenerateKeyBindsForEvent(GameEvent gameEvent, params string[] parameters)
        {
            lastEvent = gameEvent;

            string GeneratedKeybindText = string.Empty;
            string command = keyBindsStrings[gameEvent];
            string generatedKeybind = "";
            foreach (string p in parameters)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    GeneratedKeybindText = string.Format("{0} {1}", GeneratedKeybindText, p.Trim());
                    GeneratedKeybindText = GeneratedKeybindText.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(GeneratedKeybindText))
            {
                if (!string.IsNullOrEmpty(KeyBindsGenerator.generatedKeybindText))
                {
                    KeyBindsGenerator.generatedKeybindText += string.Format("$${0} {1}", command, GeneratedKeybindText);
                }
                else
                {
                    KeyBindsGenerator.generatedKeybindText = string.Format("{0} {1}", command, GeneratedKeybindText);
                }
                
                generatedKeybind = string.Format("{0} {1}", command, GeneratedKeybindText);
            }
            else
            {
                if (!string.IsNullOrEmpty(KeyBindsGenerator.generatedKeybindText))
                {
                    KeyBindsGenerator.generatedKeybindText += string.Format("$${0}", command);
                }
                else
                {
                    KeyBindsGenerator.generatedKeybindText = command;
                }

                generatedKeybind = command;
            }

            return generatedKeybind;
        }

        public string PopEvents()
        {
            string raw = KeyBindsGenerator.generatedKeybindText ?? string.Empty;
            lastKeyBindGenerated = raw;
            generatedKeybinds.Add(lastKeyBindGenerated);
            KeyBindsGenerator.generatedKeybindText = string.Empty;
            if (raw.Contains("$$"))
                return string.Format("\"{0}\"", raw);
            return raw;
        }

        /// <summary>
        /// Runs pending bind commands through the game executor without writing B.txt or recording into generatedKeybinds.
        /// Used when probing game state (e.g. in-view targeting) must not clobber the bind file or maneuver keybind history.
        /// </summary>
        public void ExecutePendingWithoutPersistingToBindFile()
        {
            string raw = KeyBindsGenerator.generatedKeybindText ?? string.Empty;
            KeyBindsGenerator.generatedKeybindText = string.Empty;
            if (string.IsNullOrEmpty(raw))
                return;
            _gameCommandExecutor.ExecuteCmd(raw);
        }

        public string GetEvent()
        {
            return KeyBindsGenerator.generatedKeybindText;
        }


        public string CompleteEvent(bool preventLoadCostumeWithoutTarget = true)
        {
            string command = string.Empty;

            command = PopEvents();

            // raw = command without any outer quotes (for ExecuteCmd)
            string raw = command.Length > 0 && command[0] == '"' 
                ? command.Substring(1, command.Length - 2) 
                : command;

            if (!string.IsNullOrEmpty(raw))
            {
                string bindContent = string.Format("{0} \"{1}\"", triggerKey, raw);
                try
                {
                    File.WriteAllText(bindFile, bindContent);
                }
                catch { }
            }

            _gameCommandExecutor.ExecuteCmd(raw ?? string.Empty);

            return command ?? string.Empty;
        }
    }
}
