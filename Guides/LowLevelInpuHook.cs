#pragma warning disable 1587
#region Copyright
/// <copyright>
/// Copyright (c) 2011 Ramunas Geciauskas, http://geciauskas.com
///
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>
/// <author>Ramunas Geciauskas</author>
/// <summary>Contains a MouseHook class for setting up low level Windows mouse hooks.</summary>
#endregion
#pragma warning restore 1587

//Updates made by Matt Schoen on 11/16/2013 to add keyboard hook

using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;

namespace RamGecTools
{   
    /// <summary>
    /// Class for intercepting low level Windows mouse hooks.
    /// </summary>
    public class LowLevelnputHook
    {
        /// <summary>
        /// Internal callback processing function
        /// </summary>
        private delegate IntPtr MouseHookHandler(int nCode, IntPtr wParam, IntPtr lParam);
        private MouseHookHandler hookHandler;
		private MouseHookHandler keyHookHandler;

        /// <summary>
        /// Function to be called when defined event occurs
        /// </summary>
        /// <param name="mouseStruct">MSLLHOOKSTRUCT mouse structure</param>
        public delegate void MouseHookCallback(MSLLHOOKSTRUCT mouseStruct);

       /// <summary>
       /// Function to be called on keyboard input
       /// </summary>
       /// <param name="key">What key was pressed</param>
        public delegate void KeyBoardHookCallback(Keys key);

        #region Events
#pragma warning disable 1591  
        public event MouseHookCallback LeftButtonDown;
        public event MouseHookCallback LeftButtonUp;
        public event MouseHookCallback RightButtonDown;
        public event MouseHookCallback RightButtonUp;
        public event MouseHookCallback MouseMove;
        public event MouseHookCallback MouseWheel;
        public event MouseHookCallback DoubleClick;
        public event MouseHookCallback MiddleButtonDown;
        public event MouseHookCallback MiddleButtonUp;

		public event KeyBoardHookCallback KeyDown;
		public event KeyBoardHookCallback KeyUp;
#pragma warning restore 1591
        #endregion

        /// <summary>
        /// Low level mouse hook's ID
        /// </summary>
        private IntPtr mouseHookID = IntPtr.Zero;
		private IntPtr keyBoardHookID = IntPtr.Zero;

        /// <summary>
        /// Install low level mouse hook
        /// </summary>
        public void Install()
        {
			hookHandler = HookFunc;
			mouseHookID = SetHook(hookHandler, WH_MOUSE_LL);
			keyHookHandler = KeyBoardHookFunc;
			keyBoardHookID = SetHook(keyHookHandler, WH_KEYBOARD_LL);
        }

        /// <summary>
        /// Remove low level mouse hook
        /// </summary>
        public void Uninstall()
        {
			if (keyBoardHookID != IntPtr.Zero) {

				UnhookWindowsHookEx(keyBoardHookID);
				keyBoardHookID = IntPtr.Zero;
			}
            if (mouseHookID == IntPtr.Zero)
                return;

            UnhookWindowsHookEx(mouseHookID);
            mouseHookID = IntPtr.Zero;
        }

        /// <summary>
        /// Destructor. Unhook current hook
        /// </summary>
        ~LowLevelnputHook()
        {
			Console.WriteLine(System.Environment.StackTrace);
            Uninstall();
        }

        /// <summary>
        /// Sets hook and assigns its ID for tracking
        /// </summary>
        /// <param name="proc">Internal callback function</param>
		/// <param name="handle">Handle for resource (keyboard or mouse?)</param>
        /// <returns>Hook ID</returns>
        private IntPtr SetHook(MouseHookHandler proc, int handle)
        {   
            using (ProcessModule module = Process.GetCurrentProcess().MainModule)
                return SetWindowsHookEx(handle, proc, GetModuleHandle(module.ModuleName), 0);
        }        

        /// <summary>
        /// Callback function
        /// </summary>
        private IntPtr HookFunc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // parse system messages
            if (nCode >= 0 && !Guides.MainForm.paused)
            {
                if (MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
                    if (LeftButtonDown != null)
                        LeftButtonDown((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_LBUTTONUP == (MouseMessages)wParam)
                    if (LeftButtonUp != null)
                        LeftButtonUp((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_RBUTTONDOWN == (MouseMessages)wParam)
                    if (RightButtonDown != null)
                        RightButtonDown((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_RBUTTONUP == (MouseMessages)wParam)
                    if (RightButtonUp != null)
                        RightButtonUp((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MOUSEMOVE == (MouseMessages)wParam)
                    if (MouseMove != null)
                        MouseMove((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MOUSEWHEEL == (MouseMessages)wParam)
                    if (MouseWheel != null)
                        MouseWheel((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_LBUTTONDBLCLK == (MouseMessages)wParam)
                    if (DoubleClick != null)
                        DoubleClick((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MBUTTONDOWN == (MouseMessages)wParam)
                    if (MiddleButtonDown != null)
                        MiddleButtonDown((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MBUTTONUP == (MouseMessages)wParam)
                    if (MiddleButtonUp != null)
                        MiddleButtonUp((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
            }
            return CallNextHookEx(mouseHookID, nCode, wParam, lParam);
        }
		private IntPtr KeyBoardHookFunc(int nCode, IntPtr wParam, IntPtr lParam) {
			// parse system messages
			if (nCode >= 0) {
				if (KeyBoardMessages.WM_KEYDOWN == (KeyBoardMessages)wParam) {
					if (KeyDown != null)
						KeyDown((Keys)Marshal.ReadInt32(lParam));
				}
				if (KeyBoardMessages.WM_SYSKEYDOWN == (KeyBoardMessages)wParam) {		//Need to catch SYSKEYDOWN for alt key
					if (KeyDown != null)
						KeyDown((Keys)Marshal.ReadInt32(lParam));
				}
				if (KeyBoardMessages.WM_KEYUP == (KeyBoardMessages)wParam)
					if (KeyUp != null)
						KeyUp((Keys)Marshal.ReadInt32(lParam));
			}
			return CallNextHookEx(keyBoardHookID, nCode, wParam, lParam);
		}
		/// <summary>
		/// Convert a POINT to a Point
		/// </summary>
		/// <param name="p">POINT to be converted</param>
		/// <returns>The POINT in Point form</returns>
		public static Point POINTToPoint(POINT p) {
			return new Point(p.x, p.y);
		}

        #region WinAPI
        private const int WH_MOUSE_LL = 14;
		private const int WH_KEYBOARD_LL = 13;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_LBUTTONDBLCLK = 0x0203,
            WM_MBUTTONDOWN = 0x0207,
            WM_MBUTTONUP = 0x0208
        }
		private enum KeyBoardMessages {
			WM_KEYDOWN = 0x0100,
			WM_KEYUP = 0x0101,
			WM_SYSKEYDOWN = 0x0104
		}

		/// <summary>
		/// Ad-hoc Point structure
		/// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT {

#pragma warning disable 1591
			public int x;
            public int y;
#pragma warning restore 1591
        }

		/// <summary>
		/// Mouse parameters
		/// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
			/// <summary>
			/// Mouse position in screen coordinates
			/// </summary>
            public POINT pt;
			/// <summary>
			/// Extra mouse data (contains mouse wheel ticks)
			/// </summary>
            public uint mouseData;
			/// <summary>
			/// Mouse flags
			/// </summary>
            public uint flags;
			/// <summary>
			/// Time of event
			/// </summary>
            public uint time;
			/// <summary>
			/// Extra info
			/// </summary>
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            MouseHookHandler lpfn, IntPtr hMod, uint dwThreadId);

		/// <summary>
		/// Unhook the input hook
		/// </summary>
		/// <param name="hhk">The IntPtr to the hook we are unhooking</param>
		/// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion
    }
}
