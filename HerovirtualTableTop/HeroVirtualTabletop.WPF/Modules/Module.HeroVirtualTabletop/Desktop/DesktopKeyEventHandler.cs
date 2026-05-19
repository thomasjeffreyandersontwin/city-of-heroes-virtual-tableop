using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Module.HeroVirtualTabletop.Library.Utility;

namespace Module.HeroVirtualTabletop.Desktop
{
    public delegate void EventMethod();
    public delegate EventMethod HandleKeyEvent(Keys vkCode, System.Windows.Input.Key inputKey);
    public interface IDesktopKeyEventHandler
    {
        void AddKeyEventHandler(HandleKeyEvent handleKeyEvent);
        void RemoveKeyEventHandler(HandleKeyEvent handleKeyEvent);
    }
    public class DesktopKeyEventHandler : IDesktopKeyEventHandler
    {
        private object lockObj = new object();
        public IntPtr hookID;
        public Keys vkCode;
        public System.Windows.Input.Key _inputKey;
        List<HandleKeyEvent> _handleKeyEvents;
        public DesktopKeyEventHandler()
        {
            _handleKeyEvents = new List<Desktop.HandleKeyEvent>();
            ActivateKeyboardHook();
        }

        public void ActivateKeyboardHook()
        {

            hookID = KeyBoardHook.SetHook(this.HandleKeyboardEvent);
        }

        public void AddKeyEventHandler(HandleKeyEvent handleKeyEvent)
        {
            lock (lockObj)
            {
                if (!_handleKeyEvents.Contains(handleKeyEvent))
                {
                    _handleKeyEvents.Add(handleKeyEvent);
                } 
            }
        }

        public void RemoveKeyEventHandler(HandleKeyEvent handleKeyEvent)
        {
            lock (lockObj)
            {
                if (_handleKeyEvents.Contains(handleKeyEvent))
                {
                    _handleKeyEvents.Remove(handleKeyEvent);
                } 
            }
        }

        internal void DeactivateKeyboardHook()
        {
            KeyBoardHook.UnsetHook(hookID);
        }
        internal IntPtr CallNextHook(IntPtr hookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            return KeyBoardHook.CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        internal System.Windows.Input.Key InputKey
        {
            get
            {
                return KeyInterop.KeyFromVirtualKey((int)this.vkCode);
            }
        }
        internal Boolean ApplicationIsActiveWindow
        {
            get
            {
                uint wndProcId;
                IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                uint wndProcThread = WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
                var currentProcId = Process.GetCurrentProcess().Id;
                return currentProcId == wndProcId;
            }
        }

        internal IntPtr HandleKeyboardEvent(int nCode, IntPtr wParam, IntPtr lParam)
        {

            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT keyboardLLHookStruct = (KBDLLHOOKSTRUCT)(Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
                this.vkCode = (Keys)keyboardLLHookStruct.vkCode;
                KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;
                if ((wmKeyboard == KeyboardMessage.WM_KEYDOWN || wmKeyboard == KeyboardMessage.WM_SYSKEYDOWN))
                {
                    IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                    uint wndProcId;
                    uint wndProcThread = WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
                    if (foregroundWindow == WindowsUtilities.FindWindow("CrypticWindow", null)
                        || Process.GetCurrentProcess().Id == wndProcId)
                    {
                        System.Windows.Input.Key inputKey = InputKey;
                        if ((inputKey == Key.Left || inputKey == Key.Right) && Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            IntPtr winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                            WindowsUtilities.SetForegroundWindow(winHandle);
                        }
                        else
                        {
                            lock (lockObj)
                            {
                                try
                                {
                                    foreach (HandleKeyEvent _handleKeyEvent in _handleKeyEvents)
                                    {
                                        EventMethod handler = _handleKeyEvent(vkCode, inputKey);
                                        if (handler != null)
                                        {
                                            handler();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                        }
                    }
                }
            }
            return CallNextHook(hookID, nCode, wParam, lParam);
        }
    }

}
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
