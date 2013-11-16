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

namespace Guides2 {

	public partial class MainForm : Form {
		private NotifyIcon trayIcon;
		private ContextMenu trayMenu;

		public static int ScreenHeight, ScreenWidth;

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();

		public MainForm() {
			// Create a simple tray menu with only one item.
			trayMenu = new ContextMenu();
			trayMenu.MenuItems.Add("Exit", OnExit);

			// Create a tray icon. In this example we use a
			// standard system icon for simplicity, but you
			// can of course use your own custom icon too.
			trayIcon = new NotifyIcon();
			trayIcon.Text = "MyTrayApp";
			trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);

			// Add menu to tray icon and show it.
			trayIcon.ContextMenu = trayMenu;
			trayIcon.Visible = true;

			AllocConsole();

			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e) {
			//Set fullscreen and transparent
			MaximizeBox = false;
			MinimizeBox = false;
			TopMost = true;
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			WindowState = System.Windows.Forms.FormWindowState.Maximized;
			TransparencyKey = BackColor;

			DoubleBuffered = true;

			MouseHook mouseHook = new MouseHook();
			mouseHook.MouseMove += new MouseHook.MouseHookCallback(OnMouseMove);
			mouseHook.LeftButtonDown += new MouseHook.MouseHookCallback(OnLeftMouseDown);
			mouseHook.LeftButtonUp += new MouseHook.MouseHookCallback(OnLeftMouseUp);
			mouseHook.RightButtonDown += new MouseHook.MouseHookCallback(OnRightMouseDown);
			mouseHook.RightButtonUp += new MouseHook.MouseHookCallback(OnRightMouseUp);
			mouseHook.MiddleButtonDown += new MouseHook.MouseHookCallback(OnMiddleMouseDown);
			mouseHook.MouseWheel += new MouseHook.MouseHookCallback(OnMouseWheel);

			KeyboardHook hook = new KeyboardHook();
			hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(KeyPressed);

			// install hooks
			mouseHook.Install();

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
		public void OnMouseMove(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			List<Guide> tmp = new List<Guide>(guides);
			foreach (Guide guide in tmp)
				guide.OnMouseMove(mouseStruct);
			if (tickCount++ > 5) {					//Don't invalidate every time or we slow the computer down
				Invalidate();
				tickCount = 0;
			}
		}
		public void OnLeftMouseDown(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			List<Guide> tmp = new List<Guide>(guides);
			foreach (Guide guide in tmp)
				guide.OnLeftMouseDown(mouseStruct);
			Invalidate();
		}
		public void OnLeftMouseUp(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			List<Guide> tmp = new List<Guide>(guides);
			foreach (Guide guide in tmp)
				guide.OnLeftMouseUp(mouseStruct);
			Invalidate();
		}
		public void OnRightMouseDown(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			foreach (Guide guide in guides)
				guide.OnRightMouseDown(mouseStruct);
			Invalidate();
		}
		public void OnRightMouseUp(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			Console.WriteLine("Add Guide");
			guides.Add(new Guide { location = mouseStruct.pt.x, horiz = false});
			Invalidate();
		}
		public void OnMiddleMouseDown(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			foreach (Guide guide in guides)
				guide.OnMiddleMouseDown(mouseStruct);
			Invalidate();
		}
		public void OnMouseWheel(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			Invalidate();
		}
		public void KeyPressed(object sender, KeyPressedEventArgs e) {
			if(e.Key ==
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

		bool lastActive = true;

		public Graphics g;

		public void Draw(Graphics g) {
			Pen b = new Pen(Color.Red);
			if (horiz)
				g.DrawLine(b, 0, location, MainForm.ScreenWidth, location);
			else
				g.DrawLine(b, location, 0, location, MainForm.ScreenHeight);
		}
		public void OnMouseMove(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			if (dragging) {
				if (horiz) {
					location = mouseStruct.pt.y;
				} else {
					location = mouseStruct.pt.x;
				}
			}
		}
		public void OnLeftMouseDown(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			lastActive = false;
			if (horiz) {
				if (Math.Abs(location - mouseStruct.pt.y) < clickMargin) {
					lastActive = dragging = true;
				}
			} else {
				if (Math.Abs(location - mouseStruct.pt.x) < clickMargin) {
					lastActive = dragging = true;
				}
			}
		}
		public void OnRightMouseDown(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			if (horiz) {
				if (Math.Abs(location - mouseStruct.pt.y) < clickMargin) {
					dragging = true;
				}
			} else {
				if (Math.Abs(location - mouseStruct.pt.x) < clickMargin) {
					dragging = true;
				}
			}
		}
		public void OnMiddleMouseDown(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			if (horiz) {
				if (Math.Abs(location - mouseStruct.pt.y) < clickMargin) {
					location = mouseStruct.pt.y;
					horiz = false;
				}
			} else {
				if (Math.Abs(location - mouseStruct.pt.x) < clickMargin) {
					location = mouseStruct.pt.x;
					horiz = true;
				}
			}
		}
		public void OnLeftMouseUp(MouseHook.MSLLHOOKSTRUCT mouseStruct) {
			dragging = false;
		}
	}
}
