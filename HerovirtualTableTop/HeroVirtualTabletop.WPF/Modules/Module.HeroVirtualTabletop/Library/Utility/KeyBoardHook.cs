using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Prism.Commands;
using System.Timers;

namespace Module.HeroVirtualTabletop.Library.Utility
{
    
    
    public static class HookCodes
    {
        public const int HC_ACTION = 0;
        public const int HC_GETNEXT = 1;
        public const int HC_SKIP = 2;
        public const int HC_NOREMOVE = 3;
        public const int HC_NOREM = HC_NOREMOVE;
        public const int HC_SYSMODALON = 4;
        public const int HC_SYSMODALOFF = 5;
    }

    public enum HookType
    {
        WH_KEYBOARD = 2,
        WH_MOUSE = 7,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }

    [StructLayout(LayoutKind.Sequential)]
    public class POINT
    {
        public int x;
        public int y;
    }

    /// <summary>
    /// The MSLLHOOKSTRUCT structure contains information about a low-level keyboard 
    /// input event. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEHOOKSTRUCT
    {
        public POINT pt;        // The x and y coordinates in screen coordinates
        public int hwnd;        // Handle to the window that'll receive the mouse message
        public int wHitTestCode;
        public int dwExtraInfo;
    }

    /// <summary>
    /// The MOUSEHOOKSTRUCT structure contains information about a mouse event passed 
    /// to a WH_MOUSE hook procedure, MouseProc. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;        // The x and y coordinates in screen coordinates. 
        public int mouseData;   // The mouse wheel and button info.
        public int flags;
        public int time;        // Specifies the time stamp for this message. 
        public IntPtr dwExtraInfo;
    }

    public enum MouseMessage
    {
        WM_MOUSEMOVE = 0x0200,
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_LBUTTONDBLCLK = 0x0203,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,
        WM_RBUTTONDBLCLK = 0x0206,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
        WM_MBUTTONDBLCLK = 0x0209,

        WM_MOUSEWHEEL = 0x020A,
        WM_MOUSEHWHEEL = 0x020E,

        WM_NCMOUSEMOVE = 0x00A0,
        WM_NCLBUTTONDOWN = 0x00A1,
        WM_NCLBUTTONUP = 0x00A2,
        WM_NCLBUTTONDBLCLK = 0x00A3,
        WM_NCRBUTTONDOWN = 0x00A4,
        WM_NCRBUTTONUP = 0x00A5,
        WM_NCRBUTTONDBLCLK = 0x00A6,
        WM_NCMBUTTONDOWN = 0x00A7,
        WM_NCMBUTTONUP = 0x00A8,
        WM_NCMBUTTONDBLCLK = 0x00A9
    }

    /// <summary>
    /// The structure contains information about a low-level keyboard input event. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public int vkCode;      // Specifies a virtual-key code
        public int scanCode;    // Specifies a hardware scan code for the key
        public int flags;
        public int time;        // Specifies the time stamp for this message
        public int dwExtraInfo;
    }

    public enum KeyboardMessage
    {
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_SYSKEYDOWN = 0x0104,
        WM_SYSKEYUP = 0x0105
    }

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    public static class KeyBoardHook
    {
        #region Imports

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        static KeyBoardHook()
        {
            AppDomain.CurrentDomain.ProcessExit += UnsetHooks;
        }
        private static Dictionary<IntPtr, LowLevelKeyboardProc> hookedProcs = new Dictionary<IntPtr, LowLevelKeyboardProc>();
        private static List<IntPtr> hookedProcIDs = new List<IntPtr>();
        public static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hookID = SetWindowsHookEx((int)HookType.WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                hookedProcIDs.Add(hookID);
                hookedProcs.Add(hookID, proc);
                return hookID;
            }
        }
  
        public static void UnsetHook(IntPtr hookID)
        {
            if (hookedProcIDs.Contains(hookID))
            {
                UnhookWindowsHookEx(hookID);
                hookedProcIDs.Remove(hookID);
                hookedProcs.Remove(hookID);
            }
        }
                
        private static void UnsetHooks(object sender, EventArgs e)
        {
            foreach (IntPtr hookID in hookedProcIDs)
            {
                UnhookWindowsHookEx(hookID);
            }
        }
    }

    public static class MouseHook
    {
        #region Imports

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        static MouseHook()
        {
            AppDomain.CurrentDomain.ProcessExit += UnsetHooks;
        }
        
        private static Dictionary<IntPtr, LowLevelMouseProc> hookedProcs = new Dictionary<IntPtr, LowLevelMouseProc>();

        private static List<IntPtr> hookedProcIDs = new List<IntPtr>();

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// This function lets hook a function with LowLevelMouseProc signature to Windows key processing queue.
        /// </summary>
        /// <param name="proc">The function to be executed. Must end with "return CallNextHookEx(hookID, nCode, wParam, lParam);" to let the processing continue correctly.</param>
        /// <returns>Return the hook identifier assigned in Windows hooks queue</returns>
        public static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hookID = SetWindowsHookEx((int)HookType.WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                hookedProcIDs.Add(hookID);
                hookedProcs.Add(hookID, proc);
                return hookID;
            }
        }

        public static void UnsetHook(IntPtr hookID)
        {
            if (hookedProcIDs.Contains(hookID))
            {
                UnhookWindowsHookEx(hookID);
                hookedProcIDs.Remove(hookID);
                hookedProcs.Remove(hookID);
            }
        }

        private static void UnsetHooks(object sender, EventArgs e)
        {
            foreach (IntPtr hookID in hookedProcIDs)
            {
                UnhookWindowsHookEx(hookID);
            }
        }

    }
}
