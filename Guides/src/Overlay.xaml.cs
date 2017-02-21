//#define DEBUG_OVERLAY
//#define UPDATE_BLOCK

using InputHook;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Cursors = System.Windows.Input.Cursors;

namespace Guides{
	//TODO: Add opacity setting
	//TODO: Change cursor for horizontal/vertical lines
	//TODO: Save/load guide sets
	//TODO: Color settings

	/// <summary>
	/// Interaction logic for Overlay.xaml
	/// </summary>
	public partial class Overlay {
		/// <summary>
		/// Which screen this is
		/// </summary>
		public int screenIndex { get; set; }

		/// <summary>
		/// The Width of the screen
		/// </summary>
		public int screenWidth { get; set; }

		/// <summary>
		/// The Height of the screen
		/// </summary>
		public int screenHeight { get; set; }

		/// <summary>
		/// The X position of the screen
		/// </summary>
		public int screenOffsetX { get; set; }

		/// <summary>
		/// The Y position of the screen
		/// </summary>
		public int screenOffsetY { get; set; }

		public double ResolutionScaleX = 1;
		public double ResolutionScaleY = 1;

#if UPDATE_BLOCK
		Stopwatch updateWatch;
		const int UpdateSleep = 15; //Time between invalidates on mouse move.  Lower for smoother animation, higher for better performance
#endif

#if DEBUG_OVERLAY
		LineGuide horiz, vert;
#endif

		public Overlay() {
			InitializeComponent();
		}

		void Window_Loaded(object sender, RoutedEventArgs e) {
			WindowState = WindowState.Maximized;
			Topmost = true;
			Cursor = Cursors.Hand;

			ResolutionScaleX = screenWidth / ActualWidth;
			ResolutionScaleY = screenHeight / ActualHeight;

#if UPDATE_BLOCK
			updateWatch = new Stopwatch();
			updateWatch.Start();
#endif

#if DEBUG_OVERLAY
			horiz = new LineGuide(this, 0);
			Canvas.Children.Add(horiz);
			vert = new LineGuide(this, 0);
			Canvas.Children.Add(vert);
			vert.horiz = false;
#endif
		}

		//Stores mouse point from mouse move, since we can't trust the values we get in onmousedown
		bool onScreen;
		Point mousePoint;
		/// <summary>
		/// Mouse Move event
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnMouseMove(MSLLHOOKSTRUCT mouseStruct) {
			onScreen = PointInScreen(mouseStruct.pt, out mousePoint);

#if UPDATE_BLOCK
			if (updateWatch.ElapsedMilliseconds <= UpdateSleep)
				return;
#endif

			OnMouse(mouseStruct);

			if (!onScreen)
				return;
			if (Canvas.Children.Count <= 0)
				return;

#if UPDATE_BLOCK
			updateWatch.Restart();
#endif

			foreach (var guide in Canvas.Children.OfType<Guide>()) {
				if (guide.OnMouseMove(mousePoint)) {
					break;
				}
			}
		}
		/// <summary>
		/// Mouse Down event for Left mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnLeftMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			OnMouse(mouseStruct);

			if (!onScreen) return;

			foreach (var guide in Canvas.Children.OfType<Guide>()) {
				if (guide.OnLeftMouseDown(mousePoint)) {
					ResetAllGuidesActive(guide);
					break;
				}
			}
		}

		/// <summary>
		/// Mouse Up event for Left mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnLeftMouseUp(MSLLHOOKSTRUCT mouseStruct) {
			OnMouse(mouseStruct);

			if (!onScreen) return;

			foreach (var guide in Canvas.Children.OfType<Guide>()) {
				guide.OnLeftMouseUp(mousePoint);
			}
		}

		/// <summary>
		/// Mouse Down event for Middle mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnMiddleMousedown(MSLLHOOKSTRUCT mouseStruct) {
			OnMouse(mouseStruct);

			if (!onScreen) return;

			foreach (var guide in Canvas.Children.OfType<Guide>()) {
				if (guide.OnLeftMouseDown(mousePoint)) {
					Canvas.Children.Remove(guide);
					return;
				}
			}

			ResetAllGuidesActive();

			if (App.ctrl) {
				var guide = new CircleGuide(this, mousePoint);
				Canvas.Children.Add(guide);
			} else {
				var guide = new LineGuide(this, mousePoint.Y);
				Canvas.Children.Add(guide);
			}
		}
		/// <summary>
		/// Mouse Down event for Rigth mosue button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnRightMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			OnMouse(mouseStruct);

			if (!onScreen) return;

			foreach (var guide in Canvas.Children.OfType<Guide>()) {
				if (guide.OnRightMouseDown(mousePoint)) {
					ResetAllGuidesActive(guide);
					break;
				}
			}
			//Invalidate();
		}
		/// <summary>
		/// Mouse Up event for Right mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnRightMouseUp(MSLLHOOKSTRUCT mouseStruct) {
			OnMouse(mouseStruct);

			if (!onScreen) return;

			foreach (var child in Canvas.Children) {
				var guide = child as Guide;
				guide?.OnRightMouseUp(mousePoint);
			}
			//Invalidate();
		}
		/// <summary>
		/// Mouse Wheel response
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnMouseWheel(MSLLHOOKSTRUCT mouseStruct) {
			OnMouse(mouseStruct);

			if (!onScreen) return;
			if (!App.shift) return;

			var delta = 5;
			if (App.ctrl)
				delta = 10;
			if (App.alt)
				delta = 1;
			foreach (var child in Canvas.Children) {
				var guide = child as Guide;
				guide?.OnMouseWheel(mousePoint, mouseStruct.mouseData, delta);
			}
			//Invalidate();
		}

		void OnMouse(MSLLHOOKSTRUCT mouseStruct) {
#if DEBUG
#if DEBUG_OVERLAY
			horiz.location = mousePoint.Y;
			vert.location = mousePoint.X;
#endif
			ScreenIndexLabel.Content = $"Screen {screenIndex}";
			ScreenSizeLabel.Content = $"Screen size {screenWidth} x {screenHeight}";
			ScreenOffsetLabel.Content = $"Screen offset {screenOffsetX} x {screenOffsetY}";
			WindowSizeLabel.Content = $"Window Size {ActualWidth:f1} x {ActualHeight:f1}";
			ResolutionScaleLabel.Content = $"Resolution Scale {ResolutionScaleX:f6} x {ResolutionScaleY:f6}";
			RawMouseLabel.Content = $"Raw mouse {mouseStruct.pt.x:f1} x {mouseStruct.pt.y:f1}";
			ScreenMouseLabel.Content = $"Screen mouse {mousePoint.X:f1} x {mousePoint.Y:f1}";
			OnScreenLabel.Content = onScreen ? "On Screen" : "Off Screen";
			OnScreenLabel.Foreground = onScreen ? Brushes.ForestGreen : Brushes.Red;
#endif
		}

		/// <summary>
		/// KeyDown response
		/// </summary>
		/// <param name="key">What key is pressed</param>
		public void OnKeyDown(Keys key) {
			foreach (var guide in Canvas.Children.OfType<Guide>()) {
				guide.OnKeyDown(key);
			}
		}
		/// <summary>
		/// Clear the guides array
		/// </summary>
		public void ClearGuides() {
			Canvas.Children.Clear();
			//Invalidate();
		}
		/// <summary>
		/// Toggling pause state
		/// </summary>
		//public void PauseToggle() {
		//	if (App.paused) {
		//		Icon = Properties.Resources.MainIconPause;
		//	} else {
		//		Icon = Properties.Resources.MainIcon;
		//	}
		//}
		/// <summary>
		/// Called when Show Guides is toggled.  This just calls Invalidate to update drawing
		/// </summary>
		public void ShowToggle()
		{
			foreach (var guide in Canvas.Children.OfType<Guide>()) {
				guide.Visibility = guide.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
			}
		}
		/// <summary>
		/// Clear the 
		/// </summary>
		public void ResetAllGuidesActive() { ResetAllGuidesActive(null); }
		/// <summary>
		/// Clears the lastActive state on all guides except the optional parameter.  If no parameter is set, all guides are cleared
		/// </summary>
		/// <param name="except">The guide to ignore</param>
		public void ResetAllGuidesActive(Guide except) {
			foreach (var guide in Canvas.Children.OfType<Guide>()) {
				if (!Equals(guide, except))
					guide.active = false;
			}
		}

		/// <summary>
		/// Returns true if mousePoint is within this form's screen.  Also converts global screen coordinates to 
		/// window coordinates.  The out parameter point will contain the converted coordinates
		/// </summary>
		/// <param name="mousePoint">Input point for check and conversion</param>
		/// <param name="point">Converted point in window coordinates</param>
		/// <returns></returns>
		public bool PointInScreen(LowLevelPoint mousePoint, out Point point) {
			point = new Point((mousePoint.x - screenOffsetX) / ResolutionScaleX,
				(mousePoint.y - screenOffsetY) / ResolutionScaleY);
			var normalized = point.Y / Height;
			normalized -= 0.5;
			normalized /= ResolutionScaleY;
			point.Y -= normalized * Height * 0.005;
			normalized = point.X / Width;
			normalized -= 0.5;
			normalized /= ResolutionScaleX;
			point.X -= normalized * Width / ResolutionScaleX * 0.005;

			return mousePoint.x >= screenOffsetX && mousePoint.x < screenOffsetX + screenWidth
				&& mousePoint.y >= screenOffsetY && mousePoint.y < screenOffsetY + screenHeight;
		}
	}
}
