using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using System.Runtime.InteropServices;
using Module.HeroVirtualTabletop.Library.Utility;

namespace Module.HeroVirtualTabletop.Desktop
{
    class DesktopMouseEventHandler
    {
        public delegate void MouseSubscriber();
        public List<MouseSubscriber> OnMouseLeftClick = new List<MouseSubscriber>();
        public List<MouseSubscriber> OnMouseRightClick = new List<MouseSubscriber>();
        public List<MouseSubscriber> OnMouseRightClickUp = new List<MouseSubscriber>();
        public List<MouseSubscriber> OnMouseLeftClickUp = new List<MouseSubscriber>();
        public List<MouseSubscriber> OnMouseDoubleClick = new List<MouseSubscriber>();
        public List<MouseSubscriber> OnMouseTripleClick = new List<MouseSubscriber>();
        public List<MouseSubscriber> OnMouseMove = new List<MouseSubscriber>();

        public IntPtr mouseHookID;
        public System.Windows.Input.Key _inputKey;
        private int maxClickTime = (int)(System.Windows.Forms.SystemInformation.DoubleClickTime * 2);
        public System.Timers.Timer DoubleTripleQuadMouseClicksTracker = new System.Timers.Timer();
        public enum DesktopMouseState { LEFT_CLICK = 1, DOUBLE_CLICK = 2, RIGHT_CLICK = 3, MOUSE_MOVE = 4, RIGHT_CLICK_UP = 5, LEFT_CLICK_UP = 6, TRIPLE_CLICK = 7, QUAD_CLICK = 8 };
        public void stuff() { }

        public bool IsDesktopActive
        {
            get
            {
                IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                return foregroundWindow == WindowsUtilities.FindWindow("CrypticWindow", null);
            }
        }
        public DesktopMouseEventHandler()
        {
            DoubleTripleQuadMouseClicksTracker.AutoReset = false;
            DoubleTripleQuadMouseClicksTracker.Interval = maxClickTime;
            DoubleTripleQuadMouseClicksTracker.Elapsed += new ElapsedEventHandler(DoubleTripleQuadMouseClicksTrackerElapsed);
            ActivateMouseHook();
        }
        public void ActivateMouseHook()
        {

            mouseHookID = MouseHook.SetHook(this.HandleMouseEvent);
        }

        public void FireMouseLeftClick()
        {
            needToHandleSingleClick = false;
            fireEvent(OnMouseLeftClick);
        }
        public void FireMouseRightClick()
        {
            fireEvent(OnMouseRightClick);
        }
        public void FireMouseLeftClickUp()
        {
            fireEvent(OnMouseLeftClickUp);
        }
        public void FireMouseMoveEvent()
        {
            fireEvent(OnMouseMove);
        }
        public void FireMouseDoubleClick()
        {
            needToHanldeDoubleClick = false;
            fireEvent(OnMouseDoubleClick);
        }
        public void FireMouseTripleCLick()
        {
            fireEvent(OnMouseTripleClick);
        }
        public void FireMouseRightClickUp()
        {
            fireEvent(OnMouseRightClickUp);
        }
        private void fireEvent(List<MouseSubscriber> subs)
        {
            try
            {
                foreach (MouseSubscriber m in subs)
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(m);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private bool needToHanldeDoubleClick = false;
        private bool needToHandleSingleClick = false;
        public DesktopMouseState MouseState = DesktopMouseState.MOUSE_MOVE;
        int MouseClickCount = 0;
        internal IntPtr HandleMouseEvent(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (MouseMessage.WM_LBUTTONDOWN == (MouseMessage)wParam)
                {
                    MouseClickCount++;
                    switch (MouseClickCount)
                    {
                        case 1:
                            MouseState = DesktopMouseState.LEFT_CLICK;
                            if(IsDesktopActive)
                                needToHandleSingleClick = true;
                            Action action = delegate()
                            {
                                DoubleTripleQuadMouseClicksTracker.Start();
                            };
                            System.Windows.Application.Current.Dispatcher.Invoke(action);
                            break;
                        case 2:
                            MouseState = DesktopMouseState.DOUBLE_CLICK;
                            needToHanldeDoubleClick = true;
                            needToHandleSingleClick = false;
                            break;
                        //case 3: MouseState = DesktopMouseState.TRIPLE_CLICK; break;
                        //case 4: MouseState = DesktopMouseState.QUAD_CLICK; break;
                    }
                }
                else if (MouseMessage.WM_RBUTTONDOWN == (MouseMessage)wParam)
                {
                    MouseState = DesktopMouseState.RIGHT_CLICK;
                }
                else if (MouseMessage.WM_RBUTTONUP == (MouseMessage)wParam)
                {   
                    MouseState = DesktopMouseState.RIGHT_CLICK_UP;
                }
                //else if (MouseMessage.WM_LBUTTONUP == (MouseMessage)wParam)
                //{
                //    if (!needToHanldeDoubleClick)
                //    {
                //        MouseState = DesktopMouseState.LEFT_CLICK_UP;
                //    }
                //}
                else if (MouseMessage.WM_MOUSEMOVE == (MouseMessage)wParam)
                {
                    MouseState = DesktopMouseState.MOUSE_MOVE;
                }
                Action d = delegate () 
                {
                    IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                    if (foregroundWindow == WindowsUtilities.FindWindow("CrypticWindow", null))
                    {
                        if (MouseState == DesktopMouseState.MOUSE_MOVE)
                            FireMouseMoveEvent();
                        else if (MouseState == DesktopMouseState.RIGHT_CLICK)
                            FireMouseRightClick();
                        else if (MouseState == DesktopMouseState.LEFT_CLICK)
                        {
                            if (needToHandleSingleClick)
                            {
                                FireMouseLeftClick();
                            }
                        }
                        //else if (MouseState == DesktopMouseState.LEFT_CLICK_UP)
                        //{
                        //    FireMouseLeftClickUp();
                        //}
                        else if (MouseState == DesktopMouseState.RIGHT_CLICK_UP)
                            FireMouseRightClickUp();
                        else if (MouseState == DesktopMouseState.DOUBLE_CLICK)
                        {
                            if (needToHanldeDoubleClick)
                            {
                                FireMouseDoubleClick();
                            }
                        }

                    }
                };

                AsyncDelegateExecuter adex = new AsyncDelegateExecuter(d, 50);
                adex.ExecuteAsyncDelegate();
            }
            return MouseHook.CallNextHookEx(mouseHookID, nCode, wParam, lParam);
        }
        void DoubleTripleQuadMouseClicksTrackerElapsed(object sender, ElapsedEventArgs e)
        {
            DoubleTripleQuadMouseClicksTracker.Stop();
            MouseClickCount = 0;
        }

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

