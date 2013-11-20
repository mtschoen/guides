//#define CONSOLE

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using RamGecTools;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Guides {

	//TODO: Add opacity setting
	//TODO: Change cursor for horizontal/vertical lines
	//TODO: Save/load guide sets
	//TODO: Color settings

	/// <summary>
	/// The main form for the app
	/// </summary>
	public partial class MainForm : Form {

		const string pauseText = "Pause Input";
		const string resumeText = "Resume Input";
		const string hideText = "Hide Guides";
		const string showText = "Show Guides";
		const string clearText = "Clear Guides";
		const string exitText = "Exit";
		const string AppName = "Guides";

		private NotifyIcon trayIcon;
		private ContextMenu trayMenu;

		/// <summary>
		/// The Height of the screen
		/// </summary>
		public static int ScreenHeight;
		/// <summary>
		/// The Width of the screen
		/// </summary>
		public static int ScreenWidth;
		/// <summary>
		/// Whether we are listening to input (listening when false)
		/// </summary>
		public static bool paused;
		/// <summary>
		/// Whether to draw the guides to the screen
		/// </summary>
		public static bool hidden;

		Stopwatch controlWatch;
		int controlResetTime = 10000;						//10000 ms before control auto resets
		Stopwatch updateWatch;
		int updateSleep = 25;								//Time between invalidates on mouse move.  Lower for smoother animation, higher for better performance

		List<Guide> guides = new List<Guide>();
		LowLevelnputHook inputHook;							//Need to have this in a variable to keep it from being garbage collected


		public static bool shift, ctrl, alt;
#if CONSOLE
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();
#endif

		/// <summary>
		/// Form constructor
		/// </summary>
		public MainForm() {

			InitializeComponent();
			trayMenu = new ContextMenu();

			//TODO: Change tray menu item for pause state

			trayMenu.MenuItems.Add("Guides 1.1.1", MenuCallback);
			trayMenu.MenuItems.Add(pauseText, MenuCallback);
			trayMenu.MenuItems.Add(hideText, MenuCallback);
			trayMenu.MenuItems.Add(clearText, MenuCallback);
			trayMenu.MenuItems.Add(exitText, MenuCallback);

			trayIcon = new NotifyIcon();
			trayIcon.Text = AppName;
			trayIcon.Icon = new Icon(Icon, 40, 40);

			trayIcon.ContextMenu = trayMenu;
			trayIcon.Visible = true;

#if CONSOLE
			AllocConsole();
#endif

		}

		private void Form1_Load(object sender, EventArgs e) {

			inputHook = new LowLevelnputHook();
			inputHook.MouseMove += new LowLevelnputHook.MouseHookCallback(OnMouseMove);
			inputHook.LeftButtonDown += new LowLevelnputHook.MouseHookCallback(OnLeftMouseDown);
			inputHook.LeftButtonUp += new LowLevelnputHook.MouseHookCallback(OnLeftMouseUp);
			inputHook.RightButtonDown += new LowLevelnputHook.MouseHookCallback(OnRightMouseDown);
			inputHook.RightButtonUp += new LowLevelnputHook.MouseHookCallback(OnRightMouseUp);
			inputHook.MiddleButtonDown += new LowLevelnputHook.MouseHookCallback(OnMiddleMousedown);
			inputHook.MouseWheel += new LowLevelnputHook.MouseHookCallback(OnMouseWheel);

			inputHook.KeyDown += new LowLevelnputHook.KeyBoardHookCallback(OnKeyDown);
			inputHook.KeyUp += new LowLevelnputHook.KeyBoardHookCallback(OnKeyUp);

			inputHook.Install();

			ScreenHeight = Screen.FromControl(this).Bounds.Height;
			ScreenWidth = Screen.FromControl(this).Bounds.Width;

			updateWatch = new Stopwatch();
			updateWatch.Start();

			controlWatch = new Stopwatch();
		}

		/// <summary>
		/// The paint function
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e) {
			//HACK: Not sure why ctrl gets stuck on.  Here's a bandaid.
			if (controlWatch.ElapsedMilliseconds > controlResetTime) {
				controlWatch.Reset();
				ctrl = false;
			}
			base.OnPaint(e);
			e.Graphics.Clear(BackColor);
			if (!hidden) {
				foreach (Guide guide in guides)
					guide.Draw(e.Graphics);
			}
		}
		private void OnMouseMove(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (guides.Count > 0) {
				bool hit = false;
				foreach (Guide guide in guides) {
					if (guide.OnMouseMove(mouseStruct)) {
						hit = true;
						break;
					}
				}

				if (hit && updateWatch.ElapsedMilliseconds > updateSleep) {					//Don't invalidate every time or we slow the computer down
					Invalidate();
					updateWatch.Restart();
				}
			}
		}
		private void OnLeftMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			Guide hit = null;
			foreach (Guide guide in guides) {
				if (guide.OnLeftMouseDown(mouseStruct)) {
					hit = guide;
					break;
				}
			}
			if (hit != null) {
				ClearActiveGuides(hit);
			}
			Invalidate();
		}

		private void OnLeftMouseUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			foreach (Guide guide in guides)
				guide.OnLeftMouseUp(mouseStruct);
			Invalidate();
		}

		private void OnMiddleMousedown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			Console.WriteLine("Add Guide");
			Guide hit = null;
			foreach (Guide guide in guides) {
				if (guide.OnLeftMouseDown(mouseStruct)) {	//Use left button down to do the same as "select"
					hit = guide;
					break;
				}
			}
			if (hit != null) {
				guides.Remove(hit);
				Invalidate();
				return;
			}
			ClearActiveGuides();
			if (ctrl) {
				guides.Add(new CircleGuide { center = LowLevelnputHook.POINTToPoint(mouseStruct.pt) });
			} else {
				guides.Add(new LineGuide { location = mouseStruct.pt.x });
			}
			Invalidate();
		}
		private void OnRightMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			Guide hit = null;
			foreach (Guide guide in guides) {
				if(guide.OnRightMouseDown(mouseStruct)){
					hit = guide;
					break;
				}
			}
			if (hit != null) {
				ClearActiveGuides(hit);
			}
			Invalidate();
		}
		private void OnRightMouseUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			foreach (Guide guide in guides)
				guide.OnRightMouseUp(mouseStruct);
			Invalidate();
		}
		private void OnMouseWheel(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (shift) {
				int delta = 5;
				if (ctrl)
					delta = 10;
				if (alt)
					delta = 1;
				foreach (Guide guide in guides)
					guide.OnMouseWheel(mouseStruct, delta);
				Invalidate();
			}
		}
		private void OnKeyDown(Keys key) {
			if (key == Keys.LShiftKey || key == Keys.RShiftKey) {
				shift = true;
			}
			if (key == Keys.LControlKey || key == Keys.RControlKey) {
				controlWatch.Start();
				ctrl = true;
			}
			if (key == Keys.LMenu || key == Keys.RMenu) {				//Not sure why menu here
				alt = true;
			}
			if (ctrl && alt && key == Keys.C) {							//CTRL+ALT+C clears guides
				ClearGuides();
			}
			if (ctrl && alt && key == Keys.P) {							//CTRL+ALT+P pauses
				PauseToggle();
			}
			if (ctrl && alt && key == Keys.H) {							//CTRL+ALT+H Show/hides
				ShowToggle();
			}
			if (ctrl && alt && key == Keys.Q) {							//CTRL+ALT+Q Quits
				OnExit();
			}
			foreach (Guide guide in guides) {
				guide.OnKeyDown(key);
			}
			Invalidate();
		}
		private void OnKeyUp(Keys key) {
			if (key == Keys.LShiftKey || key == Keys.RShiftKey) {
				shift = false;
			}
			if (key == Keys.LControlKey || key == Keys.RControlKey) {
				ctrl = false;
			}
			if (key == Keys.LMenu || key == Keys.RMenu) {
				alt = false;
			}
			Invalidate();
		}

		private void MenuCallback(object sender, EventArgs e) {
			switch(((MenuItem)sender).Text){
				case pauseText: case resumeText:
					PauseToggle();
					break;
				case showText: case hideText:
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
			guides.Clear();
			Invalidate();
		}
		private void PauseToggle() {
			paused = !paused;
			if(trayMenu.MenuItems.Count > 0)
				trayMenu.MenuItems[0].Text = paused ? resumeText : pauseText;
		}
		private void ShowToggle() {
			hidden = !hidden;
			paused = hidden;
			if (trayMenu.MenuItems.Count > 0)
				trayMenu.MenuItems[1].Text = hidden ? showText : hideText;
			Invalidate();
		}
		private void ClearActiveGuides(Guide except = null) {
			foreach (Guide guide in guides)
				if(guide != except)
					guide.lastActive = false;
		}
		
		private void OnExit() {
			Application.Exit();
		}
	}
	/// <summary>
	/// A class to represent individual on-screen guides.  These objects draw themselves and interpret input thorugh a series of callbacks
	/// </summary>
	public abstract class Guide {
		/// <summary>
		/// How far from an intersection is considered a "hit"
		/// </summary>
		public static int clickMargin = 6;
		/// <summary>
		/// Whether this guide is being dragged
		/// </summary>
		public bool dragging;
		/// <summary>
		/// Whether this was the last active guide (colored cyan)
		/// </summary>
		public bool lastActive = true;
							
		/// <summary>
		/// The point where dragging started
		/// </summary>
		protected Point dragStart;

		/// <summary>
		/// Shared pen for drawing
		/// </summary>
		protected Pen pen;
		/// <summary>
		/// Draw function for each individual guide
		/// </summary>
		/// <param name="g"></param>
		public virtual void Draw(Graphics g) {
			pen = new Pen(Color.Red);
			if (lastActive)
				pen = new Pen(Color.Cyan);
			pen.Width = 2;								//Make it a little wider so you can click it
		}
		/// <summary>
		/// Respond to mouse motion
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		/// <returns>True if this guide did anything</returns>
		public abstract bool OnMouseMove(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct);

		/// <summary>
		/// The Down event for the select button
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		/// <returns>True if the mouse is over this guide</returns>
		public virtual bool OnLeftMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (Intersects(mouseStruct.pt)) {
				lastActive = dragging = true;
				dragStart = LowLevelnputHook.POINTToPoint(mouseStruct.pt);
				return true;
			}
			return false;
		}
		/// <summary>
		/// The Down event for the rotate button
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		/// <returns>True if the mouse is over this guide</returns>
		public virtual bool OnRightMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (Intersects(mouseStruct.pt)) {
				lastActive = true;
				return true;
			}
			return false;
		}
		/// <summary>
		/// The Up event for the rotate button
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		public abstract void OnRightMouseUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct);
		public bool Intersects(LowLevelnputHook.POINT pt) { return Intersects(LowLevelnputHook.POINTToPoint(pt)); }
		public abstract bool Intersects(Point pt);
		/// <summary>
		/// The Mouse wheel event
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		/// <param name="delta">How far to move the guide per click</param>
		public abstract void OnMouseWheel(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct, int delta);
		/// <summary>
		/// The Up event for the select button
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		public virtual void OnLeftMouseUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			dragging = false;
		}

		public abstract void OnKeyDown(Keys key);
	}
	/// <summary>
	/// An extension of the Guide class that draws a line
	/// </summary>
	public class LineGuide : Guide{
		/// <summary>
		/// Whether this guide is horizontal
		/// </summary>
		public bool horiz;
		/// <summary>
		/// The screen location of this guide (from left if horiz, from top if vert)
		/// </summary>
		public int location;

		double slope, intercept, interceptHold;
		Point rotateCenter, a, b;
		bool rotating;
		bool rotated, showRotated;						//Showrotated is separated out so that the OnRotateDown doesn't cancel rotation prematurely

		/// <summary>
		/// Draws the guide
		/// </summary>
		/// <param name="g">Graphics context from form</param>
		public override void Draw(Graphics g) {
			base.Draw(g);
			if (showRotated) {
				SolidBrush br = new SolidBrush(Color.Red);
				g.FillEllipse(br, rotateCenter.X - 5, rotateCenter.Y - 5, 10, 10);
				g.DrawLine(pen, a, b);
			} else {
				if (horiz)
					g.DrawLine(pen, 0, location, MainForm.ScreenWidth, location);
				else
					g.DrawLine(pen, location, 0, location, MainForm.ScreenHeight);
			}
		}
		/// <summary>
		/// Respond to mouse motion
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		/// <returns>True if this guide did anything</returns>
		public override bool OnMouseMove(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (dragging) {
				if (rotated) {
					intercept = interceptHold + mouseStruct.pt.y - dragStart.Y
						- (mouseStruct.pt.x - dragStart.X) * slope;
					CalcPosition();
				} else {
					if (horiz) {
						location = mouseStruct.pt.y;
					} else {
						location = mouseStruct.pt.x;
					}
				}
				return true;
			}
			if (rotating) {
				showRotated = rotated = true;
				if (rotateCenter.X == mouseStruct.pt.x){
					rotated = false;
					horiz = false;
					location = mouseStruct.pt.x;
					return true;
				}
				if(rotateCenter.Y == mouseStruct.pt.y) {
					rotated = false;
					horiz = true;
					location = mouseStruct.pt.y;
					return true;
				}
				slope = (double)(rotateCenter.Y - mouseStruct.pt.y) / (rotateCenter.X - mouseStruct.pt.x);
				intercept = rotateCenter.Y - (slope * rotateCenter.X);
				CalcPosition();
				return true;
			}
			return false;
		}

		private void CalcPosition() {
			a = new Point((int)Math.Round((-intercept / slope)), 0);
			b = new Point((int)Math.Round(((MainForm.ScreenHeight - intercept) / slope)), MainForm.ScreenHeight);
		}
		/// <summary>
		/// The Down event for the select button
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		/// <returns>True if the mouse is over this guide</returns>
		public override bool OnLeftMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if(base.OnLeftMouseDown(mouseStruct)){
				interceptHold = intercept;
				return true;
			}
			return false;
		}
		/// <summary>
		/// The Down event for the rotate button
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		/// <returns>True if the mouse is over this guide</returns>
		public override bool OnRightMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (base.OnRightMouseDown(mouseStruct)) {
				rotating = true;
				rotated = false;
				rotateCenter = LowLevelnputHook.POINTToPoint(mouseStruct.pt);
				return true;
			}
			return false;
		}
		/// <summary>
		/// The Up event for the rotate button
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		public override void OnRightMouseUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			rotating = false;
			showRotated = rotated;
			if (horiz) {
				if (Intersects(mouseStruct.pt)) {
					location = mouseStruct.pt.x;
					horiz = false;
				}
			} else {
				if (Intersects(mouseStruct.pt)) {
					location = mouseStruct.pt.y;
					horiz = true;
				}
			}
		}
		/// <summary>
		/// Checks if pointer intersects within clickMargin of line
		/// </summary>
		/// <param name="pt"></param>
		/// <returns></returns>
		public override bool Intersects(Point pt) {
			if (rotated) {
				if (Math.Abs(pt.Y - Math.Abs((pt.X * slope) + intercept)) < clickMargin) {
					return true;
				}
			} else {
				if (horiz) {
					if (Math.Abs(location - pt.Y) < clickMargin) {
						return true;
					}
				} else {
					if (Math.Abs(location - pt.X) < clickMargin) {
						return true;
					}
				}
			}
			return false;
		}
		/// <summary>
		/// The Mouse wheel event
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		/// <param name="delta">How far to move the guide per click</param>
		public override void OnMouseWheel(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct, int delta) {
			if (lastActive) {
				if (mouseStruct.mouseData > 7864320)		//This is some internally defined value that I can't find
					location += delta;
				else
					location -= delta;
			}
		}
		public override void OnKeyDown(Keys key) {	}
	}
	/// <summary>
	/// Extension of Guide class to draw circular guides
	/// </summary>
	public class CircleGuide : Guide {

		/// <summary>
		/// The center of the circle
		/// </summary>
		public Point center, centerHold;
		/// <summary>
		/// The radius of the circle
		/// </summary>
		public int radius = 50;
		int reticuleLength = 7;
		int radHold;
		double centerDist, scaleDist, scaleAngle;

		bool scaling, anchorScaling, wheelScaling;
		/// <summary>
		/// Whether to draw reticule lines perpendicular to the horizontal/vertical tangents
		/// </summary>
		public bool reticule;
		Rectangle circRect {
			get {
				return new Rectangle(center.X - radius, center.Y - radius, radius + radius, radius + radius);
			}
		}

		/// <summary>
		/// Draws the guide to the screen
		/// </summary>
		/// <param name="g"></param>
		public override void Draw(Graphics g) {
			base.Draw(g);
			if (anchorScaling) {
				SolidBrush br = new SolidBrush(Color.Red);
				g.FillEllipse(br, dragStart.X - 5, dragStart.Y - 5, 10, 10);
			}
			g.DrawEllipse(pen, circRect);
			pen.Color = Color.Black;
			pen.Width = 1;
			if (reticule) {
				g.DrawLine(pen, center.X, center.Y + radius + reticuleLength, center.X, center.Y + radius - reticuleLength);
				g.DrawLine(pen, center.X + radius + reticuleLength, center.Y, center.X + radius - reticuleLength, center.Y);
				g.DrawLine(pen, center.X, center.Y - radius + reticuleLength, center.X, center.Y - radius - reticuleLength);
				g.DrawLine(pen, center.X - radius + reticuleLength, center.Y, center.X - radius - reticuleLength, center.Y);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mouseStruct"></param>
		/// <returns></returns>
		public override bool OnMouseMove(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			Point mousePoint = LowLevelnputHook.POINTToPoint(mouseStruct.pt);
			if (dragging) {
				center.X = centerHold.X + (mousePoint.X - dragStart.X);
				center.Y = centerHold.Y + (mousePoint.Y - dragStart.Y);
				return true;
			}
			if (scaling) {
				if (anchorScaling) {
					if (!wheelScaling)
						scaleDist = Utility.Distance(dragStart, mousePoint);
					AnchorScale(mousePoint, scaleDist);
				} else {
					radius = (int)Utility.Distance(center, mousePoint);
				}
				return true;
			}
			return false;
		}

		private void AnchorScale(Point mousePoint, double dist) {
			radius = radHold + (int)Math.Round(dist);
			double dx = dragStart.X - mousePoint.X;
			double dy = dragStart.Y - mousePoint.Y;
			if (dx != 0) {
				double tmpDist = centerDist + dist;
				if(!MainForm.alt)
					scaleAngle = Math.Atan(dy / dx);
				if (dx < 0) {
					dx = Math.Cos(scaleAngle) * (centerDist + dist);
					dy = Math.Sin(scaleAngle) * (centerDist + dist);
				} else {
					dx = -Math.Cos(scaleAngle) * (centerDist + dist);
					dy = -Math.Sin(scaleAngle) * (centerDist + dist);
				}
				center.X = dragStart.X + (int)Math.Round(dx);
				center.Y = dragStart.Y + (int)Math.Round(dy);
			}
		}  
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mouseStruct"></param>
		/// <returns></returns>
		public override bool OnLeftMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (base.OnLeftMouseDown(mouseStruct)) {
				centerHold = center;
				return true;
			}
			return false;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mouseStruct"></param>
		/// <returns></returns>
		public override bool OnRightMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (base.OnRightMouseDown(mouseStruct)) {
				if (MainForm.shift) {
					Point mousePoint = LowLevelnputHook.POINTToPoint(mouseStruct.pt);
					anchorScaling = true;
					centerDist = Utility.Distance(center, mousePoint);
					radHold = radius;
					dragStart = mousePoint;
				}
				wheelScaling = false;
				scaling = true;
				return true;
			}
			return false;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mouseStruct"></param>
		public override void OnRightMouseUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			scaling = false;
			anchorScaling = false;
		}
		/// <summary>
		/// Check if a point is on the circle itself.  Does this by checking if distance from point to radius is within clickMargin of the circle's radius
		/// </summary>
		/// <param name="pt">The point</param>
		/// <returns></returns>
		public override bool Intersects(Point pt) {
			double dist = Utility.Distance(center, pt);
			return dist > (radius - clickMargin) && dist < (radius + clickMargin);
		}

		/// <summary>
		/// MouseWheel delegate
		/// </summary>
		/// <param name="mouseStruct"></param>
		/// <param name="delta"></param>
		public override void OnMouseWheel(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct, int delta) {
			if (lastActive) {
				wheelScaling = true;
				if (mouseStruct.mouseData > 7864320)		//This is some internally defined value that I can't find
					delta = -delta;
				if (anchorScaling) {
					scaleDist += delta;
					AnchorScale(LowLevelnputHook.POINTToPoint(mouseStruct.pt), scaleDist);
				} else
					radius += delta;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		public override void OnKeyDown(Keys key) {
			if (lastActive) {
				if (MainForm.ctrl && MainForm.alt && key == Keys.R)
					reticule = !reticule;
				Console.WriteLine(reticule);
			}
		}
	}
	/// <summary>
	/// 
	/// </summary>
	public static class Utility {
		/// <summary>
		/// Returns the distance between two points
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static double Distance(Point a, Point b) {
			int dx = a.X - b.X;
			int dy = a.Y - b.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}
	}
}
