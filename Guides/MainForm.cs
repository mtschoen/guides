using System;
using System.Collections.Generic;	  
using System.Drawing;				  
using System.Windows.Forms;			  
using System.Diagnostics;
using InputHook;					  

namespace Guides {

	//TODO: Add opacity setting
	//TODO: Change cursor for horizontal/vertical lines
	//TODO: Save/load guide sets
	//TODO: Color settings

	/// <summary>
	/// The main form for the app
	/// </summary>
	public partial class MainForm : Form {

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
		int updateSleep = 25;                               //Time between invalidates on mouse move.  Lower for smoother animation, higher for better performance
		public float resolutionScale = 1;							//Scaling parameter for global DPI scaling

		List<Guide> guides = new List<Guide>();

		/// <summary>
		/// Form constructor
		/// </summary>
		public MainForm() {
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e) {
			Screen screen = Screen.FromControl(this);
			//ScreenHeight = screen.Bounds.Height;
			//ScreenWidth = screen.Bounds.Width;
			//ScreenOffsetX = screen.Bounds.X;
			//ScreenOffsetY = screen.Bounds.Y;

			updateWatch = new Stopwatch();
			updateWatch.Start();
		}

		/// <summary>
		/// The paint function
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e) {
			//TODO: Add OSD for things like pause state and current coordinates

			//HACK: Not sure why ctrl gets stuck on.  Here's a bandaid.
			if(Program.controlWatch.ElapsedMilliseconds > Program.controlResetTime) {
				Program.controlWatch.Reset();
				Program.ctrl = false;
			}			  
			base.OnPaint(e);
			e.Graphics.Clear(BackColor);
			if (!Program.hidden) {
				foreach (Guide guide in guides)
					guide.Draw(e.Graphics);
			}
		}
		/// <summary>
		/// Mouse Move event
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnMouseMove(MSLLHOOKSTRUCT mouseStruct) {
			Point offset;
			if(guides.Count > 0 && updateWatch.ElapsedMilliseconds > updateSleep && ScreenInit(mouseStruct.pt, out offset)) {
				if(guides.Count > 0) {
					bool hit = false;
					foreach(Guide guide in guides) {
						if(guide.OnMouseMove(offset)) {
							hit = true;
							break;
						}
					}
					if(hit) {					//Don't invalidate every time or we slow the computer down
						Invalidate();
						updateWatch.Restart();
					}
				}
			}
		}
		/// <summary>
		/// Mouse Down event for Left mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnLeftMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			Point offset;
			//Debug.WriteLine("mousedown");
			//Debug.WriteLine(mouseStruct.pt.y + ", " + ScreenHeight + " - " + ScreenOffsetY);
			//Debug.WriteLine(mouseStruct.pt.x + ", " + ScreenWidth + " - " + ScreenOffsetX);
			if (ScreenInit(mouseStruct.pt, out offset)) {
				Guide hit = null;
				foreach(Guide guide in guides) {
					if(guide.OnLeftMouseDown(offset)) {
						hit = guide;
						break;
					}
				}
				if(hit != null) {
					ResetAllGuidesActive(hit);
				}
				Invalidate();
			}
		}

		/// <summary>
		/// Mouse Up event for Left mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnLeftMouseUp(MSLLHOOKSTRUCT mouseStruct) {	
			Point offset;
			if(ScreenInit(mouseStruct.pt, out offset)) {
				foreach(Guide guide in guides)
					guide.OnLeftMouseUp(offset);
				Invalidate();
			}
		}

		/// <summary>
		/// Mouse Down event for Middle mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnMiddleMousedown(MSLLHOOKSTRUCT mouseStruct) {
			Point offset;										 
			if (ScreenInit(mouseStruct.pt, out offset)) {
				Guide hit = null;
				foreach(Guide guide in guides) {
					if(guide.OnLeftMouseDown(offset)) {	//Use left button down to do the same as "select"
						hit = guide;
						break;
					}
				}
				if(hit != null) {
					guides.Remove(hit);
					Invalidate();
					return;
				}
				ResetAllGuidesActive();
				if(Program.ctrl) {
					guides.Add(new CircleGuide(this, offset));
				} else {
					guides.Add(new LineGuide(this, offset.X));
				}
				Invalidate();
			}
		}
		/// <summary>
		/// Mouse Down event for Rigth mosue button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnRightMouseDown(MSLLHOOKSTRUCT mouseStruct) {		
			Point offset;
			if(ScreenInit(mouseStruct.pt, out offset)) {
				Guide hit = null;
				foreach(Guide guide in guides) {
					if(guide.OnRightMouseDown(offset)) {
						hit = guide;
						break;
					}
				}
				if(hit != null) {
					ResetAllGuidesActive(hit);
				}
				Invalidate();
			}
		}
		/// <summary>
		/// Mouse Up event for Right mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnRightMouseUp(MSLLHOOKSTRUCT mouseStruct) {
			Point offset;
			if(ScreenInit(mouseStruct.pt, out offset)) {
				foreach(Guide guide in guides)
					guide.OnRightMouseUp(offset);
				Invalidate();
			}
		}
		/// <summary>
		/// Mouse Wheel response
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnMouseWheel(MSLLHOOKSTRUCT mouseStruct) {
			Point offset;
			if(ScreenInit(mouseStruct.pt, out offset)) {
				if(Program.shift) {
					int delta = 5;
					if(Program.ctrl)
						delta = 10;
					if(Program.alt)
						delta = 1;
					foreach(Guide guide in guides)
						guide.OnMouseWheel(offset, mouseStruct.mouseData, delta);
					Invalidate();
				}
			}
		}
		/// <summary>
		/// KeyDown response
		/// </summary>
		/// <param name="key">What key is pressed</param>
		public void OnKeyDown(Keys key) {
			if(guides.Count > 0) {
				bool invalidate = false;
				foreach(Guide guide in guides) {
					if(guide.OnKeyDown(key))
						invalidate = true;
				}
				Invalidate();
			}
		}
		/// <summary>
		/// Clear the guides array
		/// </summary>
		public void ClearGuides() {
			guides.Clear();
			Invalidate();
		}
		/// <summary>
		/// Toggling pause state
		/// </summary>
		public void PauseToggle() {
			if (Program.paused) {
				Icon = Guides.Properties.Resources.MainIconPause;
			} else {
				Icon = Guides.Properties.Resources.MainIcon;
			}
		}
		/// <summary>
		/// Called when Show Guides is toggled.  This just calls Invalidate to update drawing
		/// </summary>
		public void ShowToggle() {
			Invalidate();
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
			foreach (Guide guide in guides)
				if(guide != except)
					guide.active = false;
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
			if(mousePoint.x > ScreenOffsetX && mousePoint.x < ScreenOffsetX + ScreenWidth &&
				mousePoint.y > ScreenOffsetY && mousePoint.y < ScreenOffsetY + ScreenHeight) {
				point.X = mousePoint.x - ScreenOffsetX;
				point.Y = mousePoint.y - ScreenOffsetY;
				return true;
			}
			return false;
		}
	}
	/// <summary>
	/// A class to represent individual on-screen guides.  These objects draw themselves and interpret input thorugh a series of callbacks
	/// </summary>
	public abstract class Guide : IDisposable {
		/// <summary>
		/// How far from an intersection is considered a "hit"
		/// </summary>
		public const int clickMargin = 6;
		/// <summary>
		/// Whether this guide is being dragged
		/// </summary>
		protected bool dragging { get; set; }
		/// <summary>
		/// Whether this was the last active guide (colored cyan)
		/// </summary>
		public bool active { get; set; }
		/// <summary>
		/// The form that owns this guide
		/// </summary>
		public MainForm owner { get; set; }
							
		/// <summary>
		/// The point where dragging started
		/// </summary>
		protected Point dragStart { get; set; }

		/// <summary>
		/// Shared pen for drawing
		/// </summary>
		protected Pen pen { get; set; }

		public Guide(MainForm owner) {
			if(owner == null)
				throw new ArgumentNullException();
			this.owner = owner;
			active = true;
		}
		/// <summary>
		/// Draw function for each individual guide
		/// </summary>
		/// <param name="g"></param>
		public virtual void Draw(Graphics g) {
			pen = new Pen(Color.Red);
			if (active)
				pen = new Pen(Color.Cyan);
			pen.Width = 2;								//Make it a little wider so you can click it
		}
		/// <summary>
		/// Respond to mouse motion
		/// </summary>
		/// <param name="mousePoint">Mouse parameters</param>
		/// <returns>True if this guide did anything</returns>
		public abstract bool OnMouseMove(Point mousePoint);

		/// <summary>
		/// The Down event for the select button
		/// </summary>
		/// <param name="mousePoint">Mouse parameters</param>
		/// <returns>True if the mouse is over this guide</returns>
		public virtual bool OnLeftMouseDown(Point mousePoint) {
			if (Intersects(mousePoint)) {
				active = dragging = true;
				dragStart = mousePoint;
				return true;
			}
			return false;
		}
		/// <summary>
		/// The Down event for the rotate button
		/// </summary>
		/// <param name="mousePoint">Mouse parameters</param>
		/// <returns>True if the mouse is over this guide</returns>
		public virtual bool OnRightMouseDown(Point mousePoint) {
			if (Intersects(mousePoint)) {
				active = true;
				return true;
			}
			return false;
		}
		/// <summary>
		/// The Up event for the rotate button
		/// </summary>
		/// <param name="mousePoint">Mouse parameters</param>
		public abstract void OnRightMouseUp(Point mousePoint);
		/// <summary>
		/// Retruns true if the point is within clickMargin of the guide
		/// </summary>
		/// <param name="pt">Test point</param>
		/// <returns></returns>
		public abstract bool Intersects(Point pt);
		/// <summary>
		/// The Mouse wheel event
		/// </summary>													   
		/// <param name="mousePoint">Where the mouse is</param>
		/// <param name="mouseData">A number signifying whether the wheel is rotating up or down</param>
		/// <param name="delta">How far to move the guide per click</param>
		public abstract void OnMouseWheel(Point mousePoint, uint mouseData, int delta);
		/// <summary>
		/// The Up event for the select button
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		public virtual void OnLeftMouseUp(Point mousePoint) {
			dragging = false;
		}
		/// <summary>
		/// Key Down Event
		/// </summary>
		/// <param name="key">What key is pressed</param>
		public virtual bool OnKeyDown(Keys key) { return false; }

		/// <summary>
		/// Dispose method (disposes pen if exists)
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing) {
			if(disposing) {
				if(pen != null)
					pen.Dispose();
			}
		}
		/// <summary>
		/// Dispose method (disposes pen if exists)
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
	/// <summary>
	/// An extension of the Guide class that draws a line
	/// </summary>
	public class LineGuide : Guide{
		/// <summary>
		/// Whether this guide is horizontal
		/// </summary>
		bool horiz;
		/// <summary>
		/// The screen location of this guide (from left if horiz, from top if vert)
		/// </summary>
		public int location { get; set; }

		double slope, intercept, interceptHold;
		Point rotateCenter, a, b;
		bool rotating;
		bool rotated, showRotated;						//Showrotated is separated out so that the OnRotateDown doesn't cancel rotation prematurely

		public LineGuide(MainForm owner, int location) : base(owner) {
			this.location = location;
		}
		
		/// <summary>
		/// Draws the guide
		/// </summary>
		/// <param name="g">Graphics context from form</param>
		public override void Draw(Graphics g) {
			base.Draw(g);
			Debug.WriteLine(owner.resolutionScale);
			if (showRotated) {
				SolidBrush br = new SolidBrush(Color.Red);
				g.FillEllipse(br, rotateCenter.X - 5, rotateCenter.Y - 5, 10, 10);
				g.DrawLine(pen, a, b);
				br.Dispose();
			} else {
				if (horiz)
					g.DrawLine(pen, 0, location, owner.ScreenWidth, location);
				else
					g.DrawLine(pen, location, 0, location, owner.ScreenHeight);
			}
			pen.Dispose();
		}
		/// <summary>
		/// Respond to mouse motion
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		/// <returns>True if this guide did anything</returns>
		public override bool OnMouseMove(Point mousePoint) {
			if (dragging) {
				if (rotated) {
					intercept = interceptHold + mousePoint.Y - dragStart.Y
						- (mousePoint.X - dragStart.X) * slope;
					CalcPosition();
				} else {
					if (horiz) {
						location = mousePoint.Y;
					} else {
						location = mousePoint.X;
					}
				}
				return true;
			}
			if (rotating) {
				showRotated = rotated = true;
				if (rotateCenter.X == mousePoint.X){
					rotated = false;
					horiz = false;
					location = mousePoint.X;
					return true;
				}
				if(rotateCenter.Y == mousePoint.Y) {
					rotated = false;
					horiz = true;
					location = mousePoint.Y;
					return true;
				}
				slope = (double)(rotateCenter.Y - mousePoint.Y) / (rotateCenter.X - mousePoint.X);
				intercept = rotateCenter.Y - (slope * rotateCenter.X);
				CalcPosition();
				return true;
			}
			return false;
		}

		private void CalcPosition() {
			a = new Point((int)Math.Round((-intercept / slope)), 0);
			b = new Point((int)Math.Round(((owner.ScreenHeight - intercept) / slope)), owner.ScreenHeight);
		}
		/// <summary>
		/// The Down event for the select button
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		/// <returns>True if the mouse is over this guide</returns>
		public override bool OnLeftMouseDown(Point mousePoint) {
			if(base.OnLeftMouseDown(mousePoint)) {
				interceptHold = intercept;
				return true;
			}
			return false;
		}
		/// <summary>
		/// The Down event for the rotate button
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		public override bool OnRightMouseDown(Point mousePoint) {
			if(base.OnRightMouseDown(mousePoint)) {
				rotating = true;
				rotated = false;
				rotateCenter = mousePoint;
				return true;
			}
			return false;
		}
		/// <summary>
		/// The Up event for the rotate button
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		public override void OnRightMouseUp(Point mousePoint) {
			rotating = false;
			showRotated = rotated;
			if (horiz) {
				if(Intersects(mousePoint)) {
					location = mousePoint.X;
					horiz = false;
				}
			} else {
				if(Intersects(mousePoint)) {
					location = mousePoint.Y;
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
		/// <param name="mousePoint"></param>
		/// <param name="mouseData"></param>
		/// <param name="delta"></param>
		public override void OnMouseWheel(Point mousePoint, uint mouseData, int delta) {
			if (active) {
				if (mouseData > 7864320)		//This is some internally defined value that I can't find
					location += delta;
				else
					location -= delta;
			}
		}
	}
	/// <summary>
	/// Extension of Guide class to draw circular guides
	/// </summary>
	public class CircleGuide : Guide {

		/// <summary>
		/// The center of the circle
		/// </summary>
		public Point center;
		Point centerHold;
		/// <summary>
		/// The radius of the circle
		/// </summary>
		public int radius { get; set; }
		int reticuleLength = 7;
		int radHold;
		double centerDist, scaleDist, scaleAngle;

		bool scaling, anchorScaling, wheelScaling;
		/// <summary>
		/// Whether to draw reticule lines perpendicular to the horizontal/vertical tangents
		/// </summary>
		public bool reticule { get; set; }
		Rectangle circRect {
			get {
				return new Rectangle(center.X - radius, center.Y - radius, radius + radius, radius + radius);
			}
		}

		public CircleGuide(MainForm owner, Point center) : this(owner, center, 50) { }
		public CircleGuide(MainForm owner, Point center, int radius)
			: base(owner) {
				this.center = center;
				this.radius = radius;
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
				br.Dispose();
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
			pen.Dispose();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		/// <returns></returns>
		public override bool OnMouseMove(Point mousePoint) {
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
				if(!Program.alt)
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
		/// <param name="mousePoint">Mouse position</param>
		/// <returns></returns>
		public override bool OnLeftMouseDown(Point mousePoint) {
			if(base.OnLeftMouseDown(mousePoint)) {
				centerHold = center;
				return true;
			}
			return false;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		/// <returns></returns>
		public override bool OnRightMouseDown(Point mousePoint) {
			if(base.OnRightMouseDown(mousePoint)) {
				if(Program.shift) {
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
		/// <param name="mousePoint">Mouse position</param>
		public override void OnRightMouseUp(Point mousePoint) {
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
			return dist > (Math.Abs(radius) - clickMargin) && dist < (Math.Abs(radius) + clickMargin);
		}

		/// <summary>
		/// MouseWheel delegate
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		/// <param name="mouseData">Info about whether wheel is rotating up or down</param>
		/// <param name="delta"></param>
		public override void OnMouseWheel(Point mousePoint, uint mouseData, int delta) {
			if (active) {
				wheelScaling = true;
				if (mouseData > 7864320)		//This is some internally defined value that I can't find
					delta = -delta;
				if (anchorScaling) {
					scaleDist += delta;
					AnchorScale(mousePoint, scaleDist);
				} else
					radius += delta;
			}
		}
		/// <summary>
		/// Key Down response
		/// </summary>
		/// <param name="key">What key was pressed</param>
		public override bool OnKeyDown(Keys key) {
			if (active) {
				if(Program.ctrl && Program.alt && key == Keys.R) {
					reticule = !reticule;
					return true;
				}
			}
			return false;
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
