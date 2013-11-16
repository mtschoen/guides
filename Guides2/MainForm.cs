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

	public partial class MainForm : Form {
		private NotifyIcon trayIcon;
		private ContextMenu trayMenu;

		public static int ScreenHeight, ScreenWidth;

		public static bool paused;

#if CONSOLE
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();
#endif

		public MainForm() {

			InitializeComponent();
			// Create a simple tray menu with only one item.
			trayMenu = new ContextMenu();
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
			inputHook.LeftButtonDown += new LowLevelnputHook.MouseHookCallback(OnLeftMouseDown);
			inputHook.LeftButtonUp += new LowLevelnputHook.MouseHookCallback(OnLeftMouseUp);
			inputHook.RightButtonDown += new LowLevelnputHook.MouseHookCallback(OnRightMouseDown);
			inputHook.MiddleButtonDown += new LowLevelnputHook.MouseHookCallback(OnRightMouseUp);
			inputHook.RightButtonUp += new LowLevelnputHook.MouseHookCallback(OnMiddleMouseDown);
			inputHook.MouseWheel += new LowLevelnputHook.MouseHookCallback(OnMouseWheel);

			inputHook.KeyDown += new LowLevelnputHook.KeyBoardHookCallback(MyKeyDown);
			inputHook.KeyUp += new LowLevelnputHook.KeyBoardHookCallback(MyKeyUp);

			// install hooks
			inputHook.Install();

			ScreenHeight = Screen.FromControl(this).Bounds.Height;
			ScreenWidth = Screen.FromControl(this).Bounds.Width;
		}

		List<Guide> guides = new List<Guide>();

		protected override void OnPaint(PaintEventArgs e) {
			base.OnPaint(e);
			e.Graphics.Clear(BackColor);
			List<Guide> tmp = new List<Guide>(guides);
			foreach (Guide guide in tmp)
				guide.Draw(e.Graphics);
		}

		int tickCount = 0;
		public void OnMouseMove(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			List<Guide> tmp = new List<Guide>(guides);
			foreach (Guide guide in tmp)
				guide.OnMouseMove(mouseStruct);
			if (tickCount++ > 5) {					//Don't invalidate every time or we slow the computer down
				Invalidate();
				tickCount = 0;
			}
		}
		public void OnLeftMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			Guide hit = null;
			foreach (Guide guide in guides) {
				if (guide.OnLeftMouseDown(mouseStruct)) {
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
		public void OnLeftMouseUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			List<Guide> tmp = new List<Guide>(guides);
			foreach (Guide guide in tmp)
				guide.OnLeftMouseUp(mouseStruct);
			Invalidate();
		}
		public void OnRightMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			foreach (Guide guide in guides)
				guide.OnRightMouseDown(mouseStruct);
			Invalidate();
		}
		public void OnRightMouseUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			Console.WriteLine("Add Guide");
			Guide hit = null;
			foreach (Guide guide in guides) {
				if (guide.OnLeftMouseDown(mouseStruct)) {
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
		public void OnMiddleMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			foreach (Guide guide in guides)
				guide.OnMiddleMouseDown(mouseStruct);
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

		public void Draw(Graphics g) {
			Pen b = new Pen(Color.Red);
			if (lastActive)
				b = new Pen(Color.Cyan);
			if (horiz)
				g.DrawLine(b, 0, location, MainForm.ScreenWidth, location);
			else
				g.DrawLine(b, location, 0, location, MainForm.ScreenHeight);
		}
		public void OnMouseMove(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (dragging) {
				if (horiz) {
					location = mouseStruct.pt.y;
				} else {
					location = mouseStruct.pt.x;
				}
			}
		}
		public bool OnLeftMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (horiz) {
				if (Math.Abs(location - mouseStruct.pt.y) < clickMargin) {
					lastActive = dragging = true;
					return true;
				}
			} else {
				if (Math.Abs(location - mouseStruct.pt.x) < clickMargin) {
					lastActive = dragging = true;
					return true;
				}
			}
			return false;
		}
		public bool OnRightMouseUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (horiz) {
				if (Math.Abs(location - mouseStruct.pt.y) < clickMargin) {
					return true;
				}
			} else {
				if (Math.Abs(location - mouseStruct.pt.x) < clickMargin) {
					return true;
				}
			}
			return false;
		}
		public void OnRightMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
		
		}
		public void OnMiddleMouseDown(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			if (horiz) {
				if (Math.Abs(location - mouseStruct.pt.y) < clickMargin) {
					location = mouseStruct.pt.x;
					horiz = false;
				}
			} else {
				if (Math.Abs(location - mouseStruct.pt.x) < clickMargin) {
					location = mouseStruct.pt.y;
					horiz = true;
				}
			}
		}
		public void OnMouseWheel(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct, int delta) {
			if (lastActive) {
				if (mouseStruct.mouseData > 7864320)
					location += delta;
				else
					location -= delta;
			}
		}
		public void OnLeftMouseUp(LowLevelnputHook.MSLLHOOKSTRUCT mouseStruct) {
			dragging = false;
		}
	}
}
