using System;
using InputHook;
using System.Windows;
using System.Windows.Forms;

namespace Guides {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App {
		LowLevelnputHook inputHook; //Need to have this in a variable to keep it from being garbage collected
		Overlay[] windows;

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);
			inputHook = new LowLevelnputHook();
			inputHook.LeftButtonDown += OnLeftMouseDown;
			inputHook.Install();

			windows = new Overlay[Screen.AllScreens.Length];
			//For testing just one screen
			//windows = new Overlay[1];

			var resolutions = Resolution.GetResolutions();

			for (int i = 0; i < windows.Length; i++) {
				windows[i] = new Overlay();
				if (i == 0) {
					MainWindow = windows[i];
				}
				var screen = Screen.AllScreens[i];
				//windows[i].StartPosition = FormStartPosition.Manual;
				//windows[i].Location = screen.WorkingArea.Location;
				//windows[i].Size = new Size(screen.WorkingArea.Width, screen.WorkingArea.Height);

				windows[i].WindowStartupLocation = WindowStartupLocation.Manual;
				var workingArea = screen.WorkingArea;
				windows[i].Top = workingArea.Top;
				windows[i].Left = workingArea.Left;

				windows[i].Width = workingArea.Width;
				windows[i].Height = workingArea.Height;

				//Console.WriteLine(windows[i].Top + ", " + windows[i].Left + ", " + windows[i].Width + ", " + windows[i].Height);

				windows[i].ScreenHeight = screen.Bounds.Height;
				windows[i].ScreenWidth = screen.Bounds.Width;
				windows[i].ScreenOffsetX = screen.Bounds.X;
				windows[i].ScreenOffsetY = screen.Bounds.Y;
				if (resolutions.ContainsKey(screen.DeviceName)) {
					//windows[i].resolutionScale = (float)windows[i].Size.Width / resolutions[screen.DeviceName].x;
					windows[i].ScreenHeight = resolutions[screen.DeviceName].y;
					windows[i].ScreenWidth = resolutions[screen.DeviceName].x;

					//NOTE: Sometimes monitors on the "extremes" show themselves as a monitor-width too far... can deal with this if I really need to
					windows[i].ScreenOffsetX = resolutions[screen.DeviceName].offsetX;
					windows[i].ScreenOffsetY = resolutions[screen.DeviceName].offsetY;
				}
				windows[i].Show();
			}
		}

		private void OnLeftMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			foreach (var window in windows)
				window.OnLeftMouseDown(mouseStruct);
		}
	}
}
