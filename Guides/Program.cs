//#define CONSOLE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.ApplicationServices;
using InputHook;
using System.Drawing;
using System.Diagnostics;
using System.Management;

namespace Guides {
	class Program : WindowsFormsApplicationBase, IDisposable {
		/// <summary>
		/// Whether we are listening to input (listening when false)
		/// </summary>
		public static bool paused;
		/// <summary>
		/// Whether to draw the guides to the screen
		/// </summary>
		public static bool hidden;
		public static bool shift, ctrl, alt;
		public static Stopwatch controlWatch;
		public static int controlResetTime = 10000;						//10000 ms before control auto resets
		
		LowLevelnputHook inputHook;							//Need to have this in a variable to keep it from being garbage collected
		MainForm[] windows;

		const string pauseText = "Pause Input (CTRL+ALT+P)";
		const string resumeText = "Resume Input (CTRL+ALT+P)";
		const string hideText = "Hide Guides (CTRL+ALT+H)";
		const string showText = "Show Guides (CTRL+ALT+H)";
		const string clearText = "Clear Guides (CTRL+ALT+C)";
		const string exitText = "Exit (CTRL+ALT+Q)";
		const string AppName = "Guides 1.4";

		private NotifyIcon trayIcon;
		private ContextMenu trayMenu;

#if CONSOLE
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();
#endif
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
#if CONSOLE
			AllocConsole();
#endif
			new Program().Run(args);
		}
		public Program() {
			trayMenu = new ContextMenu();

			trayMenu.MenuItems.Add(pauseText, MenuCallback);
			trayMenu.MenuItems.Add(hideText, MenuCallback);
			trayMenu.MenuItems.Add(clearText, MenuCallback);
			trayMenu.MenuItems.Add(exitText, MenuCallback);

			trayIcon = new NotifyIcon();
			trayIcon.Text = AppName;
			trayIcon.Icon = new Icon(Guides.Properties.Resources.TrayIcon, 40, 40);

			trayIcon.ContextMenu = trayMenu;
			trayIcon.Visible = true;

			inputHook = new LowLevelnputHook();
			inputHook.MouseMove += new MouseHookCallback(OnMouseMove);
			inputHook.LeftButtonDown += new MouseHookCallback(OnLeftMouseDown);
			inputHook.LeftButtonUp += new MouseHookCallback(OnLeftMouseUp);
			inputHook.RightButtonDown += new MouseHookCallback(OnRightMouseDown);
			inputHook.RightButtonUp += new MouseHookCallback(OnRightMouseUp);
			inputHook.MiddleButtonDown += new MouseHookCallback(OnMiddleMousedown);
			inputHook.MouseWheel += new MouseHookCallback(OnMouseWheel);

			inputHook.KeyDown += new KeyboardHookCallback(OnKeyDown);
			inputHook.KeyUp += new KeyboardHookCallback(OnKeyUp);

			inputHook.Install();

			controlWatch = new Stopwatch();

			windows = new MainForm[Screen.AllScreens.Length];
			//For testing just one screen
			//windows = new MainForm[1];

			//try {				  
			//	ManagementObjectSearcher searcher =
			//		new ManagementObjectSearcher("root\\WMI",
			//		"SELECT * FROM WmiMonitorBasicDisplayParams");	  

			//	foreach (ManagementObject queryObj in searcher.Get()) {
			//		Debug.WriteLine("-----------------------------------");
			//		Debug.WriteLine("WmiMonitorBasicDisplayParams instance");
			//		Debug.WriteLine("-----------------------------------");
			//		Debug.WriteLine("Description: {0}", queryObj["SupportedDisplayFeatures"]);
			//	}
			//} catch (ManagementException e) {
			//	MessageBox.Show("An error occurred while querying for WMI data: " + e.Message);
			//}

			for (int i = 0; i < windows.Length; i++) {
				windows[i] = new MainForm();
				if(i == 0) {
					this.MainForm = windows[i];
				}
				Screen screen = Screen.AllScreens[i];
				windows[i].StartPosition = FormStartPosition.Manual;
				windows[i].Location = screen.WorkingArea.Location;
				windows[i].Size = new Size(screen.WorkingArea.Width, screen.WorkingArea.Height);
				Debug.WriteLine(screen.WorkingArea.Width + ", " + screen.WorkingArea.Height);

				
				uint x, y;
				screen.GetDpi(DpiType.Angular, out x, out y);
				Debug.WriteLine(screen.DeviceName + " - dpiX=" + x + ", dpiY=" + y);

				screen.GetDpi(DpiType.Effective, out x, out y);
				Debug.WriteLine(screen.DeviceName + " - dpiX=" + x + ", dpiY=" + y);

				screen.GetDpi(DpiType.Raw, out x, out y);
				Debug.WriteLine(screen.DeviceName + " - dpiX=" + x + ", dpiY=" + y);

				windows[i].Show();
			}
			//new MainForm()
		}
		private void OnMouseMove(MSLLHOOKSTRUCT mouseStruct) {
			foreach(MainForm form in windows)
				form.OnMouseMove(mouseStruct);
		}
		private void OnLeftMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			foreach(MainForm form in windows)
				form.OnLeftMouseDown(mouseStruct);
		}
		private void OnLeftMouseUp(MSLLHOOKSTRUCT mouseStruct) {
			foreach(MainForm form in windows)
				form.OnLeftMouseUp(mouseStruct);
		}
		private void OnMiddleMousedown(MSLLHOOKSTRUCT mouseStruct) {
			foreach(MainForm form in windows)
				form.OnMiddleMousedown(mouseStruct);
		}
		private void OnRightMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			foreach(MainForm form in windows)
				form.OnRightMouseDown(mouseStruct);
		}
		private void OnRightMouseUp(MSLLHOOKSTRUCT mouseStruct) {
			foreach(MainForm form in windows)
				form.OnRightMouseUp(mouseStruct);
		}
		private void OnMouseWheel(MSLLHOOKSTRUCT mouseStruct) {
			foreach(MainForm form in windows)
				form.OnMouseWheel(mouseStruct);
		}
		private void OnKeyDown(Keys key) {
			if(key == Keys.LShiftKey || key == Keys.RShiftKey) {
				shift = true;
			}
			if(key == Keys.LControlKey || key == Keys.RControlKey) {
				controlWatch.Start();
				ctrl = true;
			}
			if(key == Keys.LMenu || key == Keys.RMenu) {				//Not sure why menu here
				alt = true;
			}
			if(ctrl && alt && key == Keys.C) {							//CTRL+ALT+C clears guides
				ClearGuides();
			}
			if(ctrl && alt && key == Keys.P) {							//CTRL+ALT+P pauses
				PauseToggle();
			}
			if(ctrl && alt && key == Keys.H) {							//CTRL+ALT+H Show/hides
				ShowToggle();
			}
			if(ctrl && alt && key == Keys.Q) {							//CTRL+ALT+Q Quits
				OnExit();
			}
			foreach(MainForm form in windows)
				form.OnKeyDown(key);
		}
		private void OnKeyUp(Keys key) {
			if(key == Keys.LShiftKey || key == Keys.RShiftKey) {
				shift = false;
			}
			if(key == Keys.LControlKey || key == Keys.RControlKey) {
				ctrl = false;
			}
			if(key == Keys.LMenu || key == Keys.RMenu) {
				alt = false;
			}
		}
		private void MenuCallback(object sender, EventArgs e) {
			switch(((MenuItem)sender).Text) {
				case pauseText:
				case resumeText:
					PauseToggle();
					break;
				case showText:
				case hideText:
					ShowToggle();
					break;
				case clearText:
					ClearGuides();
					break;
				case exitText:
					OnExit();
					break;
			}
		}
		private void ClearGuides() {
			foreach(MainForm form in windows)
				form.ClearGuides();
		}
		private void PauseToggle() {
			paused = !paused;
			if(paused) {
				trayIcon.Icon = Guides.Properties.Resources.TrayIconPause;
			} else {
				trayIcon.Icon = Guides.Properties.Resources.TrayIcon;
			}
			if(trayMenu.MenuItems.Count > 0)
				trayMenu.MenuItems[0].Text = paused ? resumeText : pauseText;
			foreach(MainForm form in windows)
				form.PauseToggle();
		}
		private void ShowToggle() {
			hidden = !hidden;
			paused = hidden;
			if(trayMenu.MenuItems.Count > 0)
				trayMenu.MenuItems[1].Text = hidden ? showText : hideText;
			foreach(MainForm form in windows)
				form.ShowToggle();
		}
		private static void OnExit() {
			Application.Exit();
		}
		/// <summary>
		/// Dispose method (disposes pen if exists)
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing) {
			if(disposing) {
				if(trayIcon != null)
					trayIcon.Dispose();
				if(trayMenu != null)
					trayMenu.Dispose();
			}
		}
		/// <summary>
		/// Dispose method (disposes pen if exists)
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
