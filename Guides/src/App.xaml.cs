using InputHook;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Guides {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App {
		/// <summary>
		/// Whether we are listening to input (listening when false)
		/// </summary>
		public static bool paused;
		/// <summary>
		/// Whether to draw the guides to the screen
		/// </summary>
		public static bool hidden;
		public static bool shift, ctrl, alt;

		LowLevelnputHook inputHook; //Need to have this in a variable to keep it from being garbage collected
		readonly List<Overlay> windows = new List<Overlay>();

		const string pauseText = "Pause Input (CTRL+ALT+P)";
		const string resumeText = "Resume Input (CTRL+ALT+P)";
		const string hideText = "Hide Guides (CTRL+ALT+H)";
		const string showText = "Show Guides (CTRL+ALT+H)";
		const string blockText = "Block Clicks (CTRL+ALT+B)";
		const string unblockText = "Unblock Clicks (CTRL+ALT+B)";
		const string clearText = "Clear Guides (CTRL+ALT+C)";
		const string exitText = "Exit (CTRL+ALT+Q)";
		const string AppName = "Guides 1.4";

		NotifyIcon trayIcon;
		ContextMenu trayMenu;

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			trayMenu = new ContextMenu();

			trayMenu.MenuItems.Add(pauseText, MenuCallback);
			trayMenu.MenuItems.Add(hideText, MenuCallback);
			trayMenu.MenuItems.Add(blockText, MenuCallback);
			trayMenu.MenuItems.Add(clearText, MenuCallback);
			trayMenu.MenuItems.Add(exitText, MenuCallback);

			trayIcon = new NotifyIcon();
			trayIcon.Text = AppName;
			trayIcon.Icon = new Icon(Guides.Properties.Resources.TrayIcon, 40, 40);

			trayIcon.ContextMenu = trayMenu;
			trayIcon.Visible = true;

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

			for (int i = 0; i < Screen.AllScreens.Length; i++) {
				var window = new Overlay();
				if (i == 0) {
					MainWindow = window;
				}
				var screen = Screen.AllScreens[i];

				var workingArea = screen.WorkingArea;
				window.WindowStartupLocation = WindowStartupLocation.Manual;
				window.Top = workingArea.Top;
				window.Left = workingArea.Left;

				window.Width = workingArea.Width;
				window.Height = workingArea.Height;

				window.screenHeight = screen.Bounds.Height;
				window.screenWidth = screen.Bounds.Width;
				window.screenOffsetX = screen.Bounds.X;
				window.screenOffsetY = screen.Bounds.Y;
				if (resolutions.ContainsKey(screen.DeviceName)) {
					double oldScreenHeight = window.screenHeight;
					window.screenHeight = resolutions[screen.DeviceName].y;
					window.ResolutionScaleY = oldScreenHeight / window.screenHeight;
					double oldScreenWidth = window.screenWidth;
					window.screenWidth = resolutions[screen.DeviceName].x;
					window.ResolutionScaleX = oldScreenWidth / window.screenWidth;

					//NOTE: Sometimes monitors on the "extremes" show themselves as a monitor-width too far... can deal with this if I really need to
					window.screenOffsetX = resolutions[screen.DeviceName].offsetX;
					window.screenOffsetY = resolutions[screen.DeviceName].offsetY;

					window.Top = window.screenOffsetY;
					window.Left = window.screenOffsetX;
					window.Height = window.screenHeight;
					window.Width = window.screenWidth;
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
				shift = true;
			}
			if (key == Keys.LControlKey || key == Keys.RControlKey) {
				//controlWatch.Start();
				ctrl = true;
			}
			if (key == Keys.LMenu || key == Keys.RMenu) {	//Not sure why menu here
				alt = true;
			}
			if (ctrl && alt && key == Keys.C) {				//CTRL+ALT+C clears guides
				ClearGuides();
			}
			if (ctrl && alt && key == Keys.P) {				//CTRL+ALT+P pauses
				PauseToggle();
			}
			if (ctrl && alt && key == Keys.B) {				//CTRL+ALT+B blocks
				BlockToggle();
			}
			if (ctrl && alt && key == Keys.H) {				//CTRL+ALT+H Show/hides
				ShowToggle();
			}
			if (ctrl && alt && key == Keys.Q) {				//CTRL+ALT+Q Quits
				OnExit();
			}
			foreach (var form in windows)
				form.OnKeyDown(key);
		}
		void OnKeyUp(Keys key) {
			if (key == Keys.LShiftKey || key == Keys.RShiftKey) {
				shift = false;
			}
			if (key == Keys.LControlKey || key == Keys.RControlKey) {
				ctrl = false;
			}
			if (key == Keys.LMenu || key == Keys.RMenu) {
				alt = false;
			}
		}
		private void MenuCallback(object sender, EventArgs e) {
			switch (((MenuItem)sender).Text) {
				case pauseText:
				case resumeText:
					PauseToggle();
					break;
				case showText:
				case hideText:
					ShowToggle();
					break;
				case blockText:
				case unblockText:
					BlockToggle();
					break;
				case clearText:
					ClearGuides();
					break;
				case exitText:
					OnExit();
					break;
			}
		}
		void ClearGuides() {
			foreach (var window in windows)
				window.ClearGuides();
		}
		void PauseToggle() {
			paused = !paused;
			if (paused) {
				trayIcon.Icon = Guides.Properties.Resources.TrayIconPause;
			} else {
				trayIcon.Icon = Guides.Properties.Resources.TrayIcon;
			}
			if (trayMenu.MenuItems.Count > 0)
				trayMenu.MenuItems[0].Text = paused ? resumeText : pauseText;
			//foreach (var form in windows)
			//	form.PauseToggle();
		}
		void ShowToggle() {
			hidden = !hidden;
			paused = hidden;

			if (trayMenu.MenuItems.Count > 0)
				trayMenu.MenuItems[1].Text = hidden ? showText : hideText;

			foreach (var window in windows)
				window.ShowToggle();
		}
		void BlockToggle() {
			foreach (var window in windows) {
				var background = window.Background as SolidColorBrush;
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
