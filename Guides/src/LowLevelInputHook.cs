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
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;

namespace InputHook {
	/// <summary>
	/// Class for intercepting low level Windows mouse hooks.
	/// </summary>
	public class LowLevelnputHook {
		public static bool enabled = true;

		/// <summary>
		/// Internal callback processing function
		/// </summary>
		internal delegate IntPtr MouseHookHandler(int nCode, IntPtr wParam, IntPtr lParam);

		private MouseHookHandler hookHandler;
		private MouseHookHandler keyHookHandler;

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

		public event KeyboardHookCallback KeyDown;
		public event KeyboardHookCallback KeyUp;
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
		public void Install() {
			hookHandler = HookFunc;
			mouseHookID = SetHook(hookHandler, WH_MOUSE_LL);
			keyHookHandler = KeyBoardHookFunc;
			keyBoardHookID = SetHook(keyHookHandler, WH_KEYBOARD_LL);
		}

		/// <summary>
		/// Remove low level mouse hook
		/// </summary>
		public void Uninstall() {
			if (keyBoardHookID != IntPtr.Zero) {

				NativeMethods.UnhookWindowsHookEx(keyBoardHookID);
				keyBoardHookID = IntPtr.Zero;
			}
			if (mouseHookID == IntPtr.Zero)
				return;

			NativeMethods.UnhookWindowsHookEx(mouseHookID);
			mouseHookID = IntPtr.Zero;
		}

		/// <summary>
		/// Destructor. Unhook current hook
		/// </summary>
		~LowLevelnputHook() {
			Console.WriteLine(System.Environment.StackTrace);
			Uninstall();
		}

		/// <summary>
		/// Sets hook and assigns its ID for tracking
		/// </summary>
		/// <param name="proc">Internal callback function</param>
		/// <param name="handle">Handle for resource (keyboard or mouse?)</param>
		/// <returns>Hook ID</returns>
		private static IntPtr SetHook(MouseHookHandler proc, int handle) {
			using (ProcessModule module = Process.GetCurrentProcess().MainModule)
				return NativeMethods.SetWindowsHookEx(handle, proc, NativeMethods.GetModuleHandle(module.ModuleName), 0);
		}

		/// <summary>
		/// Callback function
		/// </summary>
		private IntPtr HookFunc(int nCode, IntPtr wParam, IntPtr lParam) {
			// parse system messages
			if (nCode >= 0 && enabled) {
				if (MouseMessages.WM_LBUTTONDOWN == (MouseMessages) wParam)
					LeftButtonDown?.Invoke((MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
				if (MouseMessages.WM_LBUTTONUP == (MouseMessages) wParam)
					LeftButtonUp?.Invoke((MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
				if (MouseMessages.WM_RBUTTONDOWN == (MouseMessages) wParam)
					RightButtonDown?.Invoke((MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
				if (MouseMessages.WM_RBUTTONUP == (MouseMessages) wParam)
					RightButtonUp?.Invoke((MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
				if (MouseMessages.WM_MOUSEMOVE == (MouseMessages) wParam) 
					MouseMove?.Invoke((MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
				if (MouseMessages.WM_MOUSEWHEEL == (MouseMessages) wParam)
					MouseWheel?.Invoke((MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
				if (MouseMessages.WM_LBUTTONDBLCLK == (MouseMessages) wParam)
					DoubleClick?.Invoke((MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
				if (MouseMessages.WM_MBUTTONDOWN == (MouseMessages) wParam)
					MiddleButtonDown?.Invoke((MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
				if (MouseMessages.WM_MBUTTONUP == (MouseMessages) wParam)
					MiddleButtonUp?.Invoke((MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
			}
			return NativeMethods.CallNextHookEx(mouseHookID, nCode, wParam, lParam);
		}

		private IntPtr KeyBoardHookFunc(int nCode, IntPtr wParam, IntPtr lParam) {
			// parse system messages
			if (nCode >= 0) {
				if (KeyBoardMessages.WM_KEYDOWN == (KeyBoardMessages) wParam)
					KeyDown?.Invoke((Keys) Marshal.ReadInt32(lParam));

				//Need to catch SYSKEYDOWN for alt key
				if (KeyBoardMessages.WM_SYSKEYDOWN == (KeyBoardMessages) wParam)
					KeyDown?.Invoke((Keys) Marshal.ReadInt32(lParam));

				if (KeyBoardMessages.WM_KEYUP == (KeyBoardMessages) wParam)
					KeyUp?.Invoke((Keys) Marshal.ReadInt32(lParam));
			}

			return NativeMethods.CallNextHookEx(keyBoardHookID, nCode, wParam, lParam);
		}

		/// <summary>
		/// Convert a POINT to a Point
		/// </summary>
		/// <param name="p">POINT to be converted</param>
		/// <returns>The POINT in Point form</returns>
		//public static Point POINTToPoint(LowLevelPoint p) {
		//	return new Point(p.x, p.y);
		//}

		#region WinAPI

		private const int WH_MOUSE_LL = 14;
		private const int WH_KEYBOARD_LL = 13;

		private enum MouseMessages {
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

		#endregion
	}

	/// <summary>
	/// Function to be called when defined event occurs
	/// </summary>
	/// <param name="mouseStruct">MSLLHOOKSTRUCT mouse structure</param>
	public delegate void MouseHookCallback(MSLLHOOKSTRUCT mouseStruct);

	/// <summary>
	/// Function to be called on keyboard input
	/// </summary>
	/// <param name="key">What key was pressed</param>
	public delegate void KeyboardHookCallback(Keys key);

	/// <summary>
	/// Ad-hoc Point structure
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct LowLevelPoint {

#pragma warning disable 1591
		public int x { get; set; }
		public int y { get; set; }
#pragma warning restore 1591
		/// <summary>
		/// Equals override
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(System.Object obj) {
			// If parameter is null return false.
			if (obj == null) {
				return false;
			}
#pragma warning disable 168
			LowLevelPoint p = new LowLevelPoint();
			try {
				p = (LowLevelPoint) obj;
			}
			catch (InvalidCastException e) {
				return false;
			}
#pragma warning restore 168

			// Return true if the fields match:
			return (x == p.x) && (y == p.y);
		}

		/// <summary>
		/// Equality function
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public bool Equals(LowLevelPoint p) {
			// If parameter is null return false:
			if ((object) p == null) {
				return false;
			}

			// Return true if the fields match:
			return (x == p.x) && (y == p.y);
		}

		/// <summary>
		/// == Override
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(LowLevelPoint a, LowLevelPoint b) {
			return (a.x == b.x) && (a.y == b.y);
		}

		/// <summary>
		/// != Override
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(LowLevelPoint a, LowLevelPoint b) {
			return !(a == b);
		}

		/// <summary>
		/// Returns x ^ y
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			return x ^ y;
		}
	}

	/// <summary>
	/// Mouse parameters
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct MSLLHOOKSTRUCT {
		/// <summary>
		/// Mouse position in screen coordinates
		/// </summary>
		public LowLevelPoint pt { get; set; }

		/// <summary>
		/// Extra mouse data (contains mouse wheel ticks)
		/// </summary>
		public uint mouseData { get; set; }

		/// <summary>
		/// Mouse flags
		/// </summary>
		public uint flags { get; set; }

		/// <summary>
		/// Time of event
		/// </summary>
		public uint time { get; set; }

		/// <summary>
		/// Extra info
		/// </summary>
		public IntPtr dwExtraInfo { get; set; }

		/// <summary>
		/// Equals override
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(System.Object obj) {
			// If parameter is null return false.
			if (obj == null) {
				return false;
			}
#pragma warning disable 168 
			MSLLHOOKSTRUCT p = new MSLLHOOKSTRUCT();
			try {
				p = (MSLLHOOKSTRUCT) obj;
			}
			catch (InvalidCastException e) {
				return false;
			}
#pragma warning restore 168

			// Return true if the fields match:
			return Equality(this, p);
		}

		/// <summary>
		/// Equality function
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public bool Equals(MSLLHOOKSTRUCT p) {
			// If parameter is null return false:
			if ((object) p == null) {
				return false;
			}

			// Return true if the fields match:
			return Equality(this, p);
		}

		/// <summary>
		/// == Override
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(MSLLHOOKSTRUCT a, MSLLHOOKSTRUCT b) {
			return Equality(a, b);
		}

		/// <summary>
		/// != Override
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(MSLLHOOKSTRUCT a, MSLLHOOKSTRUCT b) {
			return !Equality(a, b);
		}

		static bool Equality(MSLLHOOKSTRUCT a, MSLLHOOKSTRUCT b) {
			return (a.pt == b.pt) && (a.mouseData == b.mouseData) && (a.flags == b.flags) && (a.time == b.time) &&
			       (a.dwExtraInfo == b.dwExtraInfo);
		}

		/// <summary>
		/// Gets Hash Code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			return (int) (pt.GetHashCode() ^ mouseData ^ flags ^ time);
		}
	}

	internal static class NativeMethods {
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern IntPtr SetWindowsHookEx(int idHook,
			LowLevelnputHook.MouseHookHandler lpfn, IntPtr hMod, uint dwThreadId);

		/// <summary>
		/// Unhook the input hook
		/// </summary>
		/// <param name="hhk">The IntPtr to the hook we are unhooking</param>
		/// <returns></returns>
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern IntPtr GetModuleHandle(string lpModuleName);
	}
}
