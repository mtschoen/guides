#define CONSOLE

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

	public partial class MainForm : Form {
		private NotifyIcon trayIcon;
		private ContextMenu trayMenu;

		public static int ScreenHeight, ScreenWidth;
		public static bool paused;

		System.Diagnostics.Stopwatch updateWatch;
		int updateSleep = 50;								//Time between invalidates on mouse move.  Lower for smoother animation, higher for better performance

#if CONSOLE
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();
#endif

		public MainForm() {

			InitializeComponent();
			// Create a simple tray menu with only one item.
			trayMenu = new ContextMenu();

			trayMenu.MenuItems.Add("Pause Input", PauseToggle);
			trayMenu.MenuItems.Add("Clear Guides", ClearGuides);
			trayMenu.MenuItems.Add("Exit", OnExit);

			// Create a tray icon. In this example we use a
			// standard system icon for simplicity, but you
			// can of course use your own custom icon too.
			trayIcon = new NotifyIcon();
			trayIcon.Text = "MyTrayApp";
			trayIcon.Icon = new Icon(Icon, 40, 40);

			// Add menu to tray icon and show it.
			trayIcon.ContextMenu = trayMenu;
			trayIcon.Visible = true;

#if CONSOLE
			AllocConsole();
#endif

		}

		LowLevelnputHook inputHook;
		private void Form1_Load(object sender, EventArgs e) {
			//Set fullscreen and transparent
			MaximizeBox = false;
			MinimizeBox = false;
			TopMost = true;
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			WindowState = System.Windows.Forms.FormWindowState.Maximized;
			TransparencyKey = BackColor;

			DoubleBuffered = true;

			inputHook = new LowLevelnputHook();
			inputHook.MouseMove += new LowLevelnputHook.MouseHookCallback(OnMouseMove);
			inputHook.LeftButtonDown += new LowLevelnputHook.MouseHookCallback(OnSelectDown);
			inputHook.LeftButtonUp += new LowLevelnputHook.MouseHookCallback(OnSelectUp);
			inputHook.RightButtonDown += new LowLevelnputHook.MouseHookCallback(OnRotateDown);
			inputHook.MiddleButtonDown += new LowLevelnputHook.MouseHookCallback(OnCreateDest);
			inputHook.RightButtonUp += new LowLevelnputHook.MouseHookCallback(OnRotateUp);
			inputHook.MouseWheel += new LowLevelnputHook.MouseHookCallback(OnMouseWheel);

			inputHook.KeyDown += new LowLevelnputHook.KeyBoardHookCallback(MyKeyDown);
			inputHook.KeyUp += new LowLevelnputHook.KeyBoardHookCallback(MyKeyUp);

			// install hooks
			inputHook.Install();

			ScreenHeight = Screen.FromControl(this).Bounds.Height;
			ScreenWidth = Screen.FromControl(this).Bounds.Width;

			updateWatch = new System.Diagnostics.Stopwatch();
			updateWatch.Start();
		}

		List<Guide> guides = new List<Guide>();

		protected override void OnPaint(PaintEventArgs e) {
			base.OnPaint(e);
			e.Graphics.Clear(BackColor);
			List<Guide> tmp = new List<Guide>(guides);
			foreach (Guide guide in tmp)
				guide.Draw(e.Graphics);
		}
		public void OnMouseMove(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			List<Guide> tmp = new List<Guide>(guides);
			foreach (Guide guide in tmp)
				guide.OnMouseMove(mouseStruct);

			if (updateWatch.ElapsedMilliseconds > updateSleep) {					//Don't invalidate every time or we slow the computer down
				Invalidate();
				updateWatch.Restart();
			}
		}
		public void OnSelectDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			Guide hit = null;
			foreach (Guide guide in guides) {
				if (guide.OnSelectDown(mouseStruct)) {
					hit = guide;
					break;
				}
			}
			if (hit != null) {
				foreach (Guide guide in guides)
					if(guide != hit)
						guide.lastActive = false;
			}
			Invalidate();
		}

		public void OnSelectUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			List<Guide> tmp = new List<Guide>(guides);
			foreach (Guide guide in tmp)
				guide.OnSelectUp(mouseStruct);
			Invalidate();
		}
		public void OnRotateDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			foreach (Guide guide in guides)
				guide.OnRotateDown(mouseStruct);
			Invalidate();
		}
		public void OnCreateDest(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
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
			foreach (Guide guide in guides)
				guide.lastActive = false;
			guides.Add(new Guide { location = mouseStruct.pt.x, horiz = false});
			Invalidate();
		}
		public void OnRotateUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			foreach (Guide guide in guides)
				guide.OnRotateUp(mouseStruct);
			Invalidate();
		}
		public void OnMouseWheel(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
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
		public void MyKeyDown(Keys key) {
			//Console.WriteLine(key);
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
				guides.Clear();
			}
			if (ctrl && alt && key == Keys.P) {							//CTRL+ALT+P pauses
				paused = !paused;
			}
			Invalidate();
		}
		public void MyKeyUp(Keys key) {
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
		public void ClearGuides(object sender, EventArgs e) {
			guides.Clear();
		}
		public void PauseToggle(object sender, EventArgs e) {
			paused = !paused;
		}		
		
		private void OnExit(object sender, EventArgs e) {
			Application.Exit();
		}
	}
	public class Guide {
		public static int clickMargin = 6;
		public bool horiz;
		public bool dragging;
		public int location;

		public bool lastActive = true;

		public Graphics g;

		double slope, intercept, interceptHold;
		Point dragStart, rotateCenter, a, b;
		bool rotating;
		bool rotated, showRotated;						//Showrotated is separated out so that the OnRotateDown doesn't cancel rotation prematurely

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
		public void OnMouseMove(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
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
			}
			if (rotating) {
				showRotated = rotated = true;
				if (rotateCenter.X == mouseStruct.pt.x){
					rotated = false;
					horiz = false;
					location = mouseStruct.pt.x;
					return;
				}
				if(rotateCenter.Y == mouseStruct.pt.y) {
					rotated = false;
					horiz = true;
					location = mouseStruct.pt.y;
					return;
				}
				slope = (double)(rotateCenter.Y - mouseStruct.pt.y) / (rotateCenter.X - mouseStruct.pt.x);
				intercept = rotateCenter.Y - (slope * rotateCenter.X);
				CalcPosition();
			}
		}

		private void CalcPosition() {
			a = new Point((int)(-intercept / slope), 0);
			b = new Point((int)((MainForm.ScreenHeight - intercept) / slope), MainForm.ScreenHeight);
		}
		public bool OnSelectDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (Intersects(mouseStruct.pt)) {
				lastActive = dragging = true;
				dragStart = LowLevelnputHook.POINTToPoint(mouseStruct.pt);
				interceptHold = intercept;
				return true;
			}
			return false;
		}
		public void OnRotateDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (Intersects(mouseStruct.pt)) {
				rotating = true;
				rotated = false;
				rotateCenter = LowLevelnputHook.POINTToPoint(mouseStruct.pt);
			}
		}
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
		public void OnMouseWheel(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct, int delta) {
			if (lastActive) {
				if (mouseStruct.mouseData > 7864320)		//This is some internally defined value that I can't find
					location += delta;
				else
					location -= delta;
			}
		}
		public void OnSelectUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			dragging = false;
		}
	}
}
