//#define DEBUG_OVERLAY
#define DEBUG_CROSS
//#define UPDATE_BLOCK

#if DEBUG_OVERLAY
using System;
#endif

#if DEBUG_CROSS
using System.Windows.Shapes;
#endif

using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using InputHook;
using Cursors = System.Windows.Input.Cursors;

namespace Guides {
	//TODO: Add opacity setting
	//TODO: Change cursor for horizontal/vertical lines
	//TODO: Save/load guide sets

	/// <summary>
	/// Interaction logic for Overlay.xaml
	/// </summary>
	public partial class Overlay {
		/// <summary>
		/// Which screen this is
		/// </summary>
		public int screenIndex { get; set; }

		public double ResolutionScaleX = 1;
		public double ResolutionScaleY = 1;

#if UPDATE_BLOCK
		Stopwatch updateWatch;
		const int UpdateSleep = 15; //Time between invalidates on mouse move.  Lower for smoother animation, higher for better performance
#endif

#if DEBUG_OVERLAY || DEBUG_CROSS
		Brush screenColor;
#endif

#if DEBUG_CROSS
		LineGuide horiz, vert;
#endif

#if DEBUG_OVERLAY
		double debugBoxStartHeight;
#endif

		ImageSource pausedIcon, normalIcon;

		public Overlay() {
			InitializeComponent();
		}

		void Window_Loaded(object sender, RoutedEventArgs e) {
			Topmost = true;
			Cursor = Cursors.Hand;

			pausedIcon = Imaging.CreateBitmapSourceFromHIcon(Properties.Resources.MainIconPause.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			normalIcon = Imaging.CreateBitmapSourceFromHIcon(Properties.Resources.MainIcon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

#if UPDATE_BLOCK
			updateWatch = new Stopwatch();
			updateWatch.Start();
#endif

#if DEBUG_OVERLAY
			screenColor = PickRandomBrush(new Random());
			debugBoxStartHeight = DebugBox.Height;
#else
			DebugBox.Visibility = Visibility.Hidden;
#endif

#if DEBUG_CROSS
			Canvas.Children.Add(new Rectangle()
			{
				StrokeThickness = 5,
				Stroke = screenColor,
				Height = Height,
				Width = Width
			});

			horiz = new LineGuide(this, 0);
			Canvas.Children.Add(horiz);
			vert = new LineGuide(this, 0);
			Canvas.Children.Add(vert);
			vert.horiz = false;
#endif
		}

#if DEBUG_OVERLAY
		static Brush PickRandomBrush(Random rnd){
			var properties = typeof(Brushes).GetProperties();
			var result = (SolidColorBrush)properties[rnd.Next(properties.Length)].GetValue(null, null);
			return result;
		}
#endif

		//Stores mouse point from mouse move, since we can't trust the values we get in onmousedown
		bool onScreen;
		Point mousePoint;
		/// <summary>
		/// Mouse Move event
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnMouseMove(MSLLHOOKSTRUCT mouseStruct) {
			onScreen = PointInScreen(mouseStruct.pt);

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

			if (App.Ctrl) {
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
		}
		/// <summary>
		/// Mouse Up event for Right mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnRightMouseUp(MSLLHOOKSTRUCT mouseStruct) {
			OnMouse(mouseStruct);

			if (!onScreen) return;

			var children = Canvas.Children.OfType<Guide>().ToList();
			foreach (var guide in children) {
				if (guide.OnRightMouseUp(mousePoint))
					break;
			}
		}
		/// <summary>
		/// Mouse Wheel response
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnMouseWheel(MSLLHOOKSTRUCT mouseStruct) {
			OnMouse(mouseStruct);

			if (!onScreen) return;
			if (!App.Shift) return;

			var delta = 5;
			if (App.Ctrl)
				delta = 10;
			if (App.Alt)
				delta = 1;
			foreach (var guide in Canvas.Children.OfType<Guide>()) {
				guide.OnMouseWheel(mousePoint, mouseStruct.mouseData, delta);
			}
		}

		void OnMouse(MSLLHOOKSTRUCT mouseStruct) {
#if DEBUG_OVERLAY
#if DEBUG_CROSS
			horiz.location = rawMousePoint.Y;
			vert.location = rawMousePoint.X;
#endif
			ScreenIndexLabel.Content = $"Screen {screenIndex}";
			ScreenIndexLabel.Foreground = screenColor;
			ScreenSizeLabel.Content = $"Screen size {Width} x {Height}";
			ScreenOffsetLabel.Content = $"Screen offset {Left} x {Top}";
			WindowSizeLabel.Content = $"Window Size {Canvas.Width:f1} x {Canvas.Height:f1}";
			ResolutionScaleLabel.Content = $"Resolution Scale {ResolutionScaleX:f6} x {ResolutionScaleY:f6}";
			RawMouseLabel.Content = $"Raw mouse {mouseStruct.pt.x:f1} x {mouseStruct.pt.y:f1}";
			ScreenMouseLabel.Content = $"Screen mouse {mousePoint.X:f1} x {mousePoint.Y:f1}";
			OnScreenLabel.Content = onScreen ? "On Screen" : "Off Screen";
			OnScreenLabel.Foreground = onScreen ? Brushes.ForestGreen : Brushes.Red;
			var guidesList = Canvas.Children.OfType<Guide>().ToArray();
			var guidesCount = guidesList.Length;
			GuidesLabel.Content = $"Guides ({guidesCount}):";

			const double lineHeight = 18;
			DebugBox.Height = debugBoxStartHeight + guidesCount * lineHeight;
			GuidesBox.Text = string.Empty;
			if (Guide.ActiveGuide != null)
				GuidesBox.Text += $"Active Guide {Guide.ActiveGuide}\n";
			foreach (var guide in guidesList) {
				GuidesBox.Text += $"{guide}\n";
			}
#endif
		}

		/// <summary>
		/// KeyDown response
		/// </summary>
		/// <param name="key">What key is pressed</param>
		public void OnKeyDown(Keys key) {
			var children = Canvas.Children.OfType<Guide>().ToList();
			foreach (var guide in children) {
				guide.OnKeyDown(key);
			}
		}
		/// <summary>
		/// Clear the guides array
		/// </summary>
		public void ClearGuides() {
			Canvas.Children.Clear();
		}
		/// <summary>
		/// Toggling pause state
		/// </summary>
		public void PauseToggle(bool paused) {
			Cursor = paused ? Cursors.Arrow : Cursors.Hand;
			Icon = paused ? pausedIcon : normalIcon;
		}
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

		/// <summary>6
		/// Returns true if rawMousePoint is within this window's screen.  Also converts global screen coordinates to 
		/// window coordinates.  The out parameter point will contain the converted coordinates
		/// </summary>
		/// <param name="rawMousePoint">Input point for check and conversion</param>
		/// <returns></returns>
		public bool PointInScreen(LowLevelPoint rawMousePoint) {
			mousePoint = new Point(rawMousePoint.x / ResolutionScaleX - Left, rawMousePoint.y / ResolutionScaleY - Top);

			return mousePoint.X >= 0 && mousePoint.X <= Width
				&& mousePoint.Y >= 0 && mousePoint.Y <= Height;
		}
	}
}
