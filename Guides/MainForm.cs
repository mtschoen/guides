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

		System.Diagnostics.Stopwatch updateWatch;
		int updateSleep = 25;								//Time between invalidates on mouse move.  Lower for smoother animation, higher for better performance

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

		LowLevelnputHook inputHook;
		private void Form1_Load(object sender, EventArgs e) {

			inputHook = new LowLevelnputHook();
			inputHook.MouseMove += new LowLevelnputHook.MouseHookCallback(OnMouseMove);
			inputHook.LeftButtonDown += new LowLevelnputHook.MouseHookCallback(OnSelectDown);
			inputHook.LeftButtonUp += new LowLevelnputHook.MouseHookCallback(OnSelectUp);
			inputHook.RightButtonDown += new LowLevelnputHook.MouseHookCallback(OnRotateDown);
			inputHook.MiddleButtonDown += new LowLevelnputHook.MouseHookCallback(OnCreateDest);
			inputHook.RightButtonUp += new LowLevelnputHook.MouseHookCallback(OnRotateUp);
			inputHook.MouseWheel += new LowLevelnputHook.MouseHookCallback(OnMouseWheel);

			inputHook.KeyDown += new LowLevelnputHook.KeyBoardHookCallback(OnKeyDown);
			inputHook.KeyUp += new LowLevelnputHook.KeyBoardHookCallback(OnKeyUp);

			inputHook.Install();

			ScreenHeight = Screen.FromControl(this).Bounds.Height;
			ScreenWidth = Screen.FromControl(this).Bounds.Width;

			updateWatch = new System.Diagnostics.Stopwatch();
			updateWatch.Start();
		}

		List<Guide> guides = new List<Guide>();

		/// <summary>
		/// The paint function
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e) {
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
		private void OnSelectDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			Guide hit = null;
			foreach (Guide guide in guides) {
				if (guide.OnSelectDown(mouseStruct)) {
					hit = guide;
					break;
				}
			}
			if (hit != null) {
				ClearActiveGuides(hit);
			}
			Invalidate();
		}

		private void OnSelectUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			foreach (Guide guide in guides)
				guide.OnSelectUp(mouseStruct);
			Invalidate();
		}

		private void OnCreateDest(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			Console.WriteLine("Add Guide");
			Guide hit = null;
			foreach (Guide guide in guides) {
				if (guide.OnSelectDown(mouseStruct)) {
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
			guides.Add(new Guide { location = mouseStruct.pt.x, horiz = false});
			Invalidate();
		}
		private void OnRotateDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			Guide hit = null;
			foreach (Guide guide in guides) {
				if(guide.OnRotateDown(mouseStruct)){
					hit = guide;
					break;
				}
			}
			if (hit != null) {
				ClearActiveGuides(hit);
			}
			Invalidate();
		}
		private void OnRotateUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			foreach (Guide guide in guides)
				guide.OnRotateUp(mouseStruct);
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
		bool shift, ctrl, alt;
		private void OnKeyDown(Keys key) {
			if (key == Keys.LShiftKey || key == Keys.RShiftKey) {
				shift = true;
			}
			if (key == Keys.LControlKey || key == Keys.RControlKey) {
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
	public class Guide {
		static int clickMargin = 6;
		/// <summary>
		/// Whether this guide is horizontal
		/// </summary>
		public bool horiz;
		/// <summary>
		/// Whether this guide is being dragged
		/// </summary>
		public bool dragging;
		/// <summary>
		/// The screen location of this guide (from left if horiz, from top if vert)
		/// </summary>
		public int location;

		/// <summary>
		/// Whether this was the last active guide (colored cyan)
		/// </summary>
		public bool lastActive = true;

		double slope, intercept, interceptHold;
		Point dragStart, rotateCenter, a, b;
		bool rotating;
		bool rotated, showRotated;						//Showrotated is separated out so that the OnRotateDown doesn't cancel rotation prematurely

		/// <summary>
		/// Draws the guide
		/// </summary>
		/// <param name="g">Graphics context from form</param>
		public void Draw(Graphics g) {
			Pen pen = new Pen(Color.Red);
			if (lastActive)
				pen = new Pen(Color.Cyan);
			pen.Width = 2;								//Make it a little wider so you can click it
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
		public bool OnMouseMove(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
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
			a = new Point((int)(-intercept / slope), 0);
			b = new Point((int)((MainForm.ScreenHeight - intercept) / slope), MainForm.ScreenHeight);
		}
		/// <summary>
		/// The Down event for the select button
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		/// <returns>True if the mouse is over this guide</returns>
		public bool OnSelectDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (Intersects(mouseStruct.pt)) {
				lastActive = dragging = true;
				dragStart = LowLevelnputHook.POINTToPoint(mouseStruct.pt);
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
		public bool OnRotateDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (Intersects(mouseStruct.pt)) {
				rotating = true;
				rotated = false;
				lastActive = true;
				rotateCenter = LowLevelnputHook.POINTToPoint(mouseStruct.pt);
				return true;
			}
			return false;
		}
		/// <summary>
		/// The Up event for the rotate button
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		public void OnRotateUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
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
		bool Intersects(LowLevelnputHook.POINT pt) {
			if (rotated) {
				if (Math.Abs(pt.y - Math.Abs((pt.x * slope) + intercept)) < clickMargin) {
					return true;
				}
			} else {
				if (horiz) {
					if (Math.Abs(location - pt.y) < clickMargin) {
						return true;
					}
				} else {
					if (Math.Abs(location - pt.x) < clickMargin) {
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
		public void OnMouseWheel(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct, int delta) {
			if (lastActive) {
				if (mouseStruct.mouseData > 7864320)		//This is some internally defined value that I can't find
					location += delta;
				else
					location -= delta;
			}
		}
		/// <summary>
		/// The Up event for the select button
		/// </summary>
		/// <param name="mouseStruct">Mouse parameters</param>
		public void OnSelectUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			dragging = false;
		}
	}
}
