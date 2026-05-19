using Module.Shared;
using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Module.HeroVirtualTabletop.Library.Utility
{
    public static class IconInteractionUtility
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //private delegate bool InitGame(IntPtr hWnd);
        private delegate bool InitGame(int x, [MarshalAs(UnmanagedType.LPStr)]string gamePath);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool CloseGame(IntPtr hWnd);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool SetUserHWND(IntPtr hWnd);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ExecuteCommand([MarshalAs(UnmanagedType.LPStr)]string commandline);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SetCOHPath([MarshalAs(UnmanagedType.LPStr)]string path);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Ansi)]
        private delegate IntPtr GetHoveredNPCInfo();

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Ansi)]
        private delegate IntPtr GetMouseXYZInGame();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool CheckIfGameLoaded();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate IntPtr DetectCollision(float sourceX, float sourceY, float sourceZ, float destX, float destY, float destZ);

        private static IntPtr dllHandle;
        private static InitGame initGame;
        private static CloseGame closeGame;
        private static SetUserHWND setUserHWnd;
        private static ExecuteCommand executeCmd;
        private static GetHoveredNPCInfo getHoveredNPCInfo;
        private static GetMouseXYZInGame getMouseXYZInGame;
        private static CheckIfGameLoaded checkIfGameLoaded;
        private static DetectCollision detectCollision;


        static IconInteractionUtility()
        {
            try { InitHookCostume(); } catch { /* HookCostume.dll unavailable (e.g. test environment) */ }
        }

        private static void InitHookCostume()
        {
            string hookCostumePath = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, "HookCostume.dll");
            //Shared.Logging.FileLogManager.ForceLog(hookCostumePath);
            dllHandle = WindowsUtilities.LoadLibrary(hookCostumePath);
            if (dllHandle != null)
            {
                IntPtr initGameAddress = WindowsUtilities.GetProcAddress(dllHandle, "InitGame");
                if (initGameAddress != IntPtr.Zero)
                {
                    initGame = (InitGame)(Marshal.GetDelegateForFunctionPointer(initGameAddress, typeof(InitGame)));
                    //Shared.Logging.FileLogManager.ForceLog("Init Game Located");
                }

                IntPtr closeGameAddress = WindowsUtilities.GetProcAddress(dllHandle, "CloseGame");
                if (closeGameAddress != IntPtr.Zero)
                {
                    closeGame = (CloseGame)(Marshal.GetDelegateForFunctionPointer(closeGameAddress, typeof(CloseGame)));
                    //Shared.Logging.FileLogManager.ForceLog("Close Game Located");
                }

                IntPtr setUserHWndAddress = WindowsUtilities.GetProcAddress(dllHandle, "SetUserHWND");
                if (setUserHWndAddress != IntPtr.Zero)
                {
                    setUserHWnd = (SetUserHWND)(Marshal.GetDelegateForFunctionPointer(setUserHWndAddress, typeof(SetUserHWND)));
                    //Shared.Logging.FileLogManager.ForceLog("Set User HWND Located");
                }

                IntPtr executeCmdAddress = WindowsUtilities.GetProcAddress(dllHandle, "ExecuteCommand");
                if (executeCmdAddress != IntPtr.Zero)
                {
                    executeCmd = (ExecuteCommand)(Marshal.GetDelegateForFunctionPointer(executeCmdAddress, typeof(ExecuteCommand)));
                    //Shared.Logging.FileLogManager.ForceLog("Execute Command Located");
                }

                IntPtr getHoveredNPCInfoAddress = WindowsUtilities.GetProcAddress(dllHandle, "GetHoveredNPCInfo");
                if (getHoveredNPCInfoAddress != IntPtr.Zero)
                {
                    getHoveredNPCInfo = (GetHoveredNPCInfo)(Marshal.GetDelegateForFunctionPointer(getHoveredNPCInfoAddress, typeof(GetHoveredNPCInfo)));
                    //Shared.Logging.FileLogManager.ForceLog("Get Hovered NPC Info Located");
                }

                IntPtr getMouseXYZInGameAddress = WindowsUtilities.GetProcAddress(dllHandle, "GetMouseXYZInGame");
                if (getMouseXYZInGameAddress != IntPtr.Zero)
                {
                    getMouseXYZInGame = (GetMouseXYZInGame)(Marshal.GetDelegateForFunctionPointer(getMouseXYZInGameAddress, typeof(GetMouseXYZInGame)));
                    //Shared.Logging.FileLogManager.ForceLog("Get Mouse XYZ Located");
                }

                IntPtr checkGameDoneAddress = WindowsUtilities.GetProcAddress(dllHandle, "CheckGameDone");
                if (checkGameDoneAddress != IntPtr.Zero)
                {
                    checkIfGameLoaded = (CheckIfGameLoaded)(Marshal.GetDelegateForFunctionPointer(checkGameDoneAddress, typeof(CheckIfGameLoaded)));
                    //Shared.Logging.FileLogManager.ForceLog("Check Game Done Located");
                }

                IntPtr detectCollisionAddress = WindowsUtilities.GetProcAddress(dllHandle, "CollisionDetection");
                if (detectCollisionAddress != IntPtr.Zero)
                {
                    detectCollision = (DetectCollision)(Marshal.GetDelegateForFunctionPointer(detectCollisionAddress, typeof(DetectCollision)));
                    //Shared.Logging.FileLogManager.ForceLog("Collision Detection Located");
                }
            }
        }

        public static void InitializeGame(string path)
        {
            BridgeLog("InitializeGame path=" + path + " dllLoaded=" + (dllHandle != IntPtr.Zero) + " initGame_null=" + (initGame == null));
            if (initGame == null) { BridgeLog("InitializeGame SKIPPED - initGame is null"); return; }
            initGame(1, path);
            //Shared.Logging.FileLogManager.ForceLog("Init Game Completed");
        }

        public static bool IsGameLoaded()
        {
            if (checkIfGameLoaded == null) { BridgeLog("IsGameLoaded: checkIfGameLoaded is null"); return false; }
            bool gameLoaded = checkIfGameLoaded();
            if (gameLoaded)
            {
                //Shared.Logging.FileLogManager.ForceLog("Game Loaded");
            }
            else
            {
                //Shared.Logging.FileLogManager.ForceLog("Game not loaded yet...");
                //System.Threading.Thread.Sleep(1000);
            }
            return gameLoaded;
        }

        public static void DoPostInitialization()
        {
            System.Threading.Thread.Sleep(1500);
            setUserHWnd(IntPtr.Zero);
        }

        public static void CloseCOH()
        {
            closeGame(IntPtr.Zero);
        }

        private static void BridgeLog(string msg)
        {
            try { System.IO.File.AppendAllText(@"C:\temp\spawn_log.txt", System.DateTime.Now.ToString("HH:mm:ss.fff") + " BRIDGE." + msg + "\r\n"); } catch { }
        }

        public static void ExecuteCmd(string command)
        {
            BridgeLog("ExecuteCmd dllLoaded=" + (dllHandle != IntPtr.Zero) + " executeCmd_null=" + (executeCmd == null) + " cmd=" + (command != null ? command.Substring(0, Math.Min(120, command.Length)) : "NULL"));
            if (string.IsNullOrEmpty(command))
                return;

            if (command.Length > 254)
            {
                string parsedCmd = command;
                int position = 0;
                while (parsedCmd.Length > 254)
                {
                    parsedCmd = parsedCmd.Substring(0, parsedCmd.LastIndexOf("$$", 254));
                    //executeCmd("/" + parsedCmd);
                    ParseDirectionalFXAndExecuteCommand(parsedCmd);
                    System.Threading.Thread.Sleep(500);// Sleep a while after executing a command
                    position += parsedCmd.Length + 2;
                    parsedCmd = command.Substring(position);
                }
                //executeCmd("/" + parsedCmd);
                ParseDirectionalFXAndExecuteCommand(parsedCmd);
            }
            else
            {
                //executeCmd("/" + command);
                ParseDirectionalFXAndExecuteCommand(command);
            }
        }

        private static void ParseDirectionalFXAndExecuteCommand(string command)
        {
            // No-op when HookCostume.dll is not loaded (e.g. unit tests)
            if (executeCmd == null)
                return;

            // Commands like this has to be split:
            /*/target_name Fire Blast [Primary]$$load_costume Gehenna\Gehenna_RainOfFire.fx x=131.93 y=0.25 z=-107.23
                     * $$target_name Fire Blast [Primary]$$load_costume Gehenna\Gehenna_RainOfFireHands.fx x=131.93 y=0.25 z=-107.23*/
            int loadCostumeCount = Regex.Matches(command, "load_costume", RegexOptions.IgnoreCase).Count;
            int directionalCostumeCount = Regex.Matches(command, "fx x=", RegexOptions.IgnoreCase).Count;
            string parseCommand = command;
            bool multipleDirectionalFXExist = loadCostumeCount > 1 && directionalCostumeCount > 1;
            int position = 0;
            while(multipleDirectionalFXExist)
            {
                int firstIndexOfdirectionalCostume = parseCommand.IndexOf(".fx x=");
                int secondIndexOfDirectionalCostume = parseCommand.IndexOf(".fx x=", firstIndexOfdirectionalCostume + 1);
                if(firstIndexOfdirectionalCostume < 0  || secondIndexOfDirectionalCostume < 0)
                    break;
                parseCommand = parseCommand.Substring(0, parseCommand.LastIndexOf("$$target_name", secondIndexOfDirectionalCostume));
                executeCmd("/" + parseCommand);
                System.Threading.Thread.Sleep(500); // Sleep a while after executing a command
                position += parseCommand.Length + 2;
                parseCommand = command.Substring(position);
                multipleDirectionalFXExist = Regex.Matches(parseCommand, "load_costume").Count > 1 && Regex.Matches(parseCommand, ".fx x=").Count > 1;
            }
            executeCmd("/" + parseCommand);
        }
        public static string GeInfoFromNpcMouseIsHoveringOver()
        {
            return Marshal.PtrToStringAnsi(getHoveredNPCInfo());
        }
        

        public static string GetMouseXYZString()
        {
            System.Threading.Thread.Sleep(100);
            return Marshal.PtrToStringAnsi(getMouseXYZInGame());
        }

        
       
        public static string GetCollisionInfo(float sourceX, float sourceY, float sourceZ, float destX, float destY, float destZ)
        {
            return Marshal.PtrToStringAnsi(detectCollision(sourceX, sourceY, sourceZ, destX, destY, destZ));
        }
    }
}
