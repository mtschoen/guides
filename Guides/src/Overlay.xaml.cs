using InputHook;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;
using Cursors = System.Windows.Input.Cursors;

namespace Guides {
	//TODO: Add opacity setting
	//TODO: Change cursor for horizontal/vertical lines
	//TODO: Save/load guide sets
	//TODO: Color settings

	/// <summary>
	/// Interaction logic for Overlay.xaml
	/// </summary>
	public partial class Overlay {
		/// <summary>
		/// The Height of the screen
		/// </summary>
		public int ScreenHeight { get; set; }

		/// <summary>
		/// The Width of the screen
		/// </summary>
		public int ScreenWidth { get; set; }

		/// <summary>
		/// The X position of the screen
		/// </summary>
		public int ScreenOffsetX { get; set; }

		/// <summary>
		/// The Y position of the screen
		/// </summary>
		public int ScreenOffsetY { get; set; }

		Stopwatch updateWatch;
		int updateSleep = 25; //Time between invalidates on mouse move.  Lower for smoother animation, higher for better performance
		public float resolutionScale = 1; //Scaling parameter for global DPI scaling

		//readonly List<Guide> guides = new List<Guide>();

		public Overlay() {
			InitializeComponent();
		}

		void Window_Loaded(object sender, RoutedEventArgs e) {
			WindowState = WindowState.Maximized;
			Topmost = true;
			Cursor = Cursors.Hand;

			updateWatch = new Stopwatch();
			updateWatch.Start();
		}

		//Stores mouse point from mouse move, since we can't trust the values we get in onmousedown
		bool onScreen;
		Point mousePoint;
		/// <summary>
		/// Mouse Move event
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnMouseMove(MSLLHOOKSTRUCT mouseStruct) {
			Point offset;
			onScreen = ScreenInit(mouseStruct.pt, out offset);

			if (canvas.Children.Count > 0 && updateWatch.ElapsedMilliseconds > updateSleep && onScreen) {
				mousePoint = offset;
				var hit = false;
				foreach (var child in canvas.Children) {
					var guide = child as Guide;
					if (guide == null)
						continue;

					if (guide.OnMouseMove(offset)) {
						hit = true;
						break;
					}
				}
				if (hit) {
					//Don't invalidate every time or we slow the computer down
					//Invalidate();
					updateWatch.Restart();
				}
			}
		}
		/// <summary>
		/// Mouse Down event for Left mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnLeftMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			//Point offset;
			//if (ScreenInit(mouseStruct.pt, out offset)) {
			if (onScreen) {
				Guide hit = null;
				foreach (var child in canvas.Children) {
					var guide = child as Guide;
					if (guide == null)
						continue;

					if (guide.OnLeftMouseDown(mousePoint)) {
						hit = guide;
						break;
					}
				}
				if (hit != null) {
					ResetAllGuidesActive(hit);
				}
			}
		}

		/// <summary>
		/// Mouse Up event for Left mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnLeftMouseUp(MSLLHOOKSTRUCT mouseStruct) {
			//Point offset;
			//if(ScreenInit(mouseStruct.pt, out offset)) {
			if (onScreen) {
				foreach (var child in canvas.Children) {
					var guide = child as Guide;

					guide?.OnLeftMouseUp(mousePoint);
				}
			}
		}

		/// <summary>
		/// Mouse Down event for Middle mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnMiddleMousedown(MSLLHOOKSTRUCT mouseStruct) {
			//Console.WriteLine(Left + ", " + Top + ", " + Width + ", " + Height);
			//Console.WriteLine(mouseStruct.pt.x + ", " + mouseStruct.pt.y);
			//return;
			Point offset = mousePoint;
			bool localOnScreen = onScreen;
			if (mousePoint.X == 0 && mousePoint.Y == 0) {
				localOnScreen = ScreenInit(mouseStruct.pt, out offset);
			}
			if (localOnScreen) {
				//if (onScreen) {
				Guide hit = null;
				foreach (var child in canvas.Children) {
					var guide = child as Guide;
					if (guide == null)
						continue;

					if (guide.OnLeftMouseDown(offset)) { //Use left button down to do the same as "select"
						hit = guide;
						break;
					}
				}
				if (hit != null) {
					canvas.Children.Remove(hit);
					return;
				}
				ResetAllGuidesActive();
				if (App.ctrl) {
					var guide = new CircleGuide(this, offset);
					canvas.Children.Add(guide);
				} else {
					var guide = new LineGuide(this, offset.X);
					canvas.Children.Add(guide);
					Debug.WriteLine(offset.X);
				}
			}
		}
		/// <summary>
		/// Mouse Down event for Rigth mosue button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnRightMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			//Point offset;
			//if(ScreenInit(mouseStruct.pt, out offset)) {
			if (onScreen) {
				Guide hit = null;
				foreach (var child in canvas.Children) {
					var guide = child as Guide;
					if (guide == null)
						continue;

					if (guide.OnRightMouseDown(mousePoint)) {
						hit = guide;
						break;
					}
				}
				if (hit != null) {
					ResetAllGuidesActive(hit);
				}
				//Invalidate();
			}
		}
		/// <summary>
		/// Mouse Up event for Right mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnRightMouseUp(MSLLHOOKSTRUCT mouseStruct) {
			//Point offset;
			//if(ScreenInit(mouseStruct.pt, out offset)) {
			if (onScreen) {
				foreach (var child in canvas.Children) {
					var guide = child as Guide;
					guide?.OnRightMouseUp(mousePoint);
				}
				//Invalidate();
			}
		}
		/// <summary>
		/// Mouse Wheel response
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnMouseWheel(MSLLHOOKSTRUCT mouseStruct) {
			//Point offset;
			//if(ScreenInit(mouseStruct.pt, out offset)) {
			if (onScreen) {
				if (App.shift) {
					int delta = 5;
					if (App.ctrl)
						delta = 10;
					if (App.alt)
						delta = 1;
					foreach (var child in canvas.Children) {
						var guide = child as Guide;
						guide?.OnMouseWheel(mousePoint, mouseStruct.mouseData, delta);
					}
					//Invalidate();
				}
			}
		}
		/// <summary>
		/// KeyDown response
		/// </summary>
		/// <param name="key">What key is pressed</param>
		public void OnKeyDown(Keys key) {
			if (canvas.Children.Count > 0) {
				bool invalidate = false;
				foreach (var child in canvas.Children) {
					var guide = child as Guide;
					if (guide == null)
						continue;

					if (guide.OnKeyDown(key))
						invalidate = true;
				}
				//Invalidate();
			}
		}
		/// <summary>
		/// Clear the guides array
		/// </summary>
		public void ClearGuides() {
			canvas.Children.Clear();
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
		//public void ShowToggle() {
		//	Invalidate();
		//}
		/// <summary>
		/// Clear the 
		/// </summary>
		public void ResetAllGuidesActive() { ResetAllGuidesActive(null); }
		/// <summary>
		/// Clears the lastActive state on all guides except the optional parameter.  If no parameter is set, all guides are cleared
		/// </summary>
		/// <param name="except">The guide to ignore</param>
		public void ResetAllGuidesActive(Guide except) {
			foreach (var child in canvas.Children) {
				var guide = child as Guide;
				if (guide == null)
					continue;

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
		public bool ScreenInit(LowLevelPoint mousePoint, out Point point) {
			point = new Point();
			//if (mousePoint.x > Left && mousePoint.x < Left + Width &&
			//	mousePoint.y > Top && mousePoint.y < Top + Height) {
			//	point.X = mousePoint.x - Left;
			//	point.Y = mousePoint.y - Top;
			//	return true;
			//}
			if (mousePoint.x > ScreenOffsetX && mousePoint.x < ScreenOffsetX + ScreenWidth &&
				mousePoint.y > ScreenOffsetY && mousePoint.y < ScreenOffsetY + ScreenHeight) {
				point.X = mousePoint.x - ScreenOffsetX;
				point.Y = mousePoint.y - ScreenOffsetY;
				return true;
			}
			return false;
		}
	}
}
