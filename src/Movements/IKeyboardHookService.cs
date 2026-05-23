using Module.HeroVirtualTabletop.Library.Utility;
using System;

namespace HeroVTT.Movements
{
    public interface IKeyboardHookService
    {
        IntPtr SetHook(KeyBoardHookCallback callback);
        void UnsetHook(IntPtr hookId);
        IntPtr CallNextHook(IntPtr hookId, int nCode, IntPtr wParam, IntPtr lParam);
    }

    public delegate IntPtr KeyBoardHookCallback(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>Production bridge to KeyBoardHook low-level keyboard hook.</summary>
    public sealed class KeyboardHookServiceBridge : IKeyboardHookService
    {
        public static readonly KeyboardHookServiceBridge Instance = new KeyboardHookServiceBridge();

        private KeyboardHookServiceBridge() { }

        public IntPtr SetHook(KeyBoardHookCallback callback)
        {
            LowLevelKeyboardProc proc = (nCode, wParam, lParam) => callback(nCode, wParam, lParam);
            return KeyBoardHook.SetHook(proc);
        }

        public void UnsetHook(IntPtr hookId)
        {
            KeyBoardHook.UnsetHook(hookId);
        }

        public IntPtr CallNextHook(IntPtr hookId, int nCode, IntPtr wParam, IntPtr lParam)
        {
            return KeyBoardHook.CallNextHookEx(hookId, nCode, wParam, lParam);
        }
    }
}
