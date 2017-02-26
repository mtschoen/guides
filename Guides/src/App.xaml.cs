using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using InputHook;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Guides
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App {
		/// <summary>
		/// Whether we are listening to input (listening when false)
		/// </summary>
		public static bool Paused;
		/// <summary>
		/// Whether to draw the guides to the screen
		/// </summary>
		public static bool Hidden;
		public static bool Shift, Ctrl, Alt;

		LowLevelnputHook inputHook; //Need to have this in a variable to keep it from being garbage collected
		readonly List<Overlay> windows = new List<Overlay>();

		const string PauseText = "Pause Input (CTRL+ALT+P)";
		const string ResumeText = "Resume Input (CTRL+ALT+P)";
		const string HideText = "Hide Guides (CTRL+ALT+H)";
		const string ShowText = "Show Guides (CTRL+ALT+H)";
		const string BlockText = "Block Clicks (CTRL+ALT+B)";
		const string UnblockText = "Unblock Clicks (CTRL+ALT+B)";
		const string ClearText = "Clear Guides (CTRL+ALT+C)";
		const string ColorsText = "Colors Window";
		const string ExitText = "Exit (CTRL+ALT+Q)";
		const string AppName = "Guides 2.0";

		NotifyIcon trayIcon;
		ContextMenu trayMenu;

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			trayMenu = new ContextMenu();

			trayMenu.MenuItems.Add(PauseText, MenuCallback);
			trayMenu.MenuItems.Add(HideText, MenuCallback);
			trayMenu.MenuItems.Add(BlockText, MenuCallback);
			trayMenu.MenuItems.Add(ClearText, MenuCallback);
			trayMenu.MenuItems.Add(ColorsText, MenuCallback);
			trayMenu.MenuItems.Add(ExitText, MenuCallback);

			trayIcon = new NotifyIcon {
				Text = AppName,
				Icon = new Icon(Guides.Properties.Resources.TrayIcon, 40, 40),
				ContextMenu = trayMenu,
				Visible = true
			};

			inputHook = new LowLevelnputHook();
			inputHook.MouseMove += OnMouseMove;
			inputHook.LeftButtonDown += OnLeftMouseDown;
			inputHook.LeftButtonUp += OnLeftMouseUp;
			inputHook.RightButtonDown += OnRightMouseDown;
			inputHook.RightButtonUp += OnRightMouseUp;
			inputHook.MiddleButtonDown += OnMiddleMouseDown;
			inputHook.MouseWheel += OnMouseWheel;

			inputHook.KeyDown += OnKeyDown;
			inputHook.KeyUp += OnKeyUp;

			inputHook.Install();

			var resolutions = Resolution.GetResolutions();

			for (var i = 0; i < Screen.AllScreens.Length; i++) {
				var window = new Overlay();
				if (i == 0) {
					MainWindow = window;
				}
				var screen = Screen.AllScreens[i];

				var workingArea = screen.WorkingArea;
				window.Top = workingArea.Top;
				window.Left = workingArea.Left;

				window.Width = workingArea.Width;
				window.Height = workingArea.Height;

				if (resolutions.ContainsKey(screen.DeviceName)) {
					window.ResolutionScaleY = (double) resolutions[screen.DeviceName].y/screen.Bounds.Height;
					window.ResolutionScaleX = (double) resolutions[screen.DeviceName].x/screen.Bounds.Width;
				}

				window.screenIndex = i;
				window.Show();
				windows.Add(window);
			}
		}

		void OnMouseMove(MSLLHOOKSTRUCT mouseStruct) {
			foreach (var window in windows)
				window.OnMouseMove(mouseStruct);
		}
		void OnLeftMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			foreach (var window in windows)
				window.OnLeftMouseDown(mouseStruct);
		}
		void OnLeftMouseUp(MSLLHOOKSTRUCT mouseStruct) {
			foreach (var window in windows)
				window.OnLeftMouseUp(mouseStruct);
		}
		void OnMiddleMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			foreach (var window in windows)
				window.OnMiddleMousedown(mouseStruct);
		}
		void OnRightMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			foreach (var window in windows)
				window.OnRightMouseDown(mouseStruct);
		}
		void OnRightMouseUp(MSLLHOOKSTRUCT mouseStruct) {
			foreach (var window in windows)
				window.OnRightMouseUp(mouseStruct);
		}
		void OnMouseWheel(MSLLHOOKSTRUCT mouseStruct) {
			foreach (var window in windows)
				window.OnMouseWheel(mouseStruct);
		}
		void OnKeyDown(Keys key) {
			if (key == Keys.LShiftKey || key == Keys.RShiftKey) {
				Shift = true;
			}
			if (key == Keys.LControlKey || key == Keys.RControlKey) {
				//controlWatch.Start();
				Ctrl = true;
			}
			if (key == Keys.LMenu || key == Keys.RMenu) {	//Not sure why menu here
				Alt = true;
			}
			if (Ctrl && Alt && key == Keys.C) {				//CTRL+ALT+C clears guides
				ClearGuides();
			}
			if (Ctrl && Alt && key == Keys.P) {				//CTRL+ALT+P pauses
				PauseToggle();
			}
			if (Ctrl && Alt && key == Keys.B) {				//CTRL+ALT+B blocks
				BlockToggle();
			}
			if (Ctrl && Alt && key == Keys.H) {				//CTRL+ALT+H Show/hides
				ShowToggle();
			}
			if (Ctrl && Alt && key == Keys.Q) {				//CTRL+ALT+Q Quits
				OnExit();
			}
			foreach (var window in windows)
				window.OnKeyDown(key);
		}
		void OnKeyUp(Keys key) {
			if (key == Keys.LShiftKey || key == Keys.RShiftKey) {
				Shift = false;
			}
			if (key == Keys.LControlKey || key == Keys.RControlKey) {
				Ctrl = false;
			}
			if (key == Keys.LMenu || key == Keys.RMenu) {
				Alt = false;
			}
		}
		void MenuCallback(object sender, EventArgs e) {
			switch (((MenuItem)sender).Text) {
				case PauseText:
				case ResumeText:
					PauseToggle();
					break;
				case ShowText:
				case HideText:
					ShowToggle();
					break;
				case BlockText:
				case UnblockText:
					BlockToggle();
					break;
				case ClearText:
					ClearGuides();
					break;
				case ColorsText:
					new Colors().Show();
					break;
				case ExitText:
					OnExit();
					break;
			}
		}
		void ClearGuides() {
			foreach (var window in windows)
				window.ClearGuides();
		}
		void PauseToggle() {
			Paused = !Paused;
			trayIcon.Icon = Paused ? Guides.Properties.Resources.TrayIconPause : Guides.Properties.Resources.TrayIcon;
			if (trayMenu.MenuItems.Count > 0)
				trayMenu.MenuItems[0].Text = Paused ? ResumeText : PauseText;
			foreach (var form in windows)
				form.PauseToggle(Paused);
		}
		void ShowToggle() {
			Hidden = !Hidden;
			Paused = Hidden;

			if (trayMenu.MenuItems.Count > 0)
				trayMenu.MenuItems[1].Text = Hidden ? ShowText : HideText;

			foreach (var window in windows)
				window.ShowToggle();
		}
		void BlockToggle() {
			foreach (var window in windows) {
				var background = (SolidColorBrush)window.Background;
				if (background.Color.A == 0) {
					window.Background = (Brush) new BrushConverter().ConvertFromString("#01000000");
				} else {
					window.Background = Brushes.Transparent;
				}
			}
		}
		static void OnExit() {
			Current.Shutdown();
		}
	}
}
