using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Guides {
	/// <summary>
	/// An extension of the Guide class that draws a line
	/// </summary>
	public class LineGuide : Guide {
		/// <summary>
		/// Whether this guide is horizontal
		/// </summary>
		bool horiz;
		/// <summary>
		/// The screen location of this guide (from left if horiz, from top if vert)
		/// </summary>
		public double location { get {
				return horiz ? Canvas.GetTop(this) : Canvas.GetLeft(this);
			}
			set {
				if (horiz)
					Canvas.SetTop(this, value);
				else
					Canvas.SetLeft(this, value);
			}
		}

		double slope, intercept, interceptHold;
		Point rotateCenter, a, b;
		bool rotating;
		bool rotated, showRotated; //Showrotated is separated out so that the OnRotateDown doesn't cancel rotation prematurely

		protected override Geometry DefiningGeometry { get { return geometry; } }
		readonly LineGeometry geometry;

		public LineGuide(Overlay owner, double location) : base(owner) {
			var top = new Point(0, 0);
			var bot = new Point(0, owner.Height);
			geometry = new LineGeometry(top, bot);
			this.location = location;
		}

		/// <summary>
		/// Draws the guide
		/// </summary>
		/// <param name="g">Graphics context from form</param>
		//public override void Draw(Graphics g) {
		//	base.Draw(g);
		//	if(showRotated) {
		//		SolidBrush br = new SolidBrush(Color.Red);
		//		g.FillEllipse(br, owner.resolutionScale * rotateCenter.X - 5, owner.resolutionScale * rotateCenter.Y - 5, 10, 10);
		//		g.DrawLine(pen, a, b);
		//		br.Dispose();
		//	} else {
		//		if(horiz)
		//			g.DrawLine(pen, 0, owner.resolutionScale * location, owner.resolutionScale * owner.ScreenWidth, owner.resolutionScale * location);
		//		else
		//			g.DrawLine(pen, owner.resolutionScale * location, 0, owner.resolutionScale * location, owner.resolutionScale * owner.ScreenHeight);
		//	}
		//	pen.Dispose();
		//}
		/// <summary>
		/// Respond to mouse motion
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		/// <returns>True if this guide did anything</returns>
		public override bool OnMouseMove(Point mousePoint) {
			if(dragging) {
				if(rotated) {
					intercept = interceptHold + mousePoint.Y - dragStart.Y
						- (mousePoint.X - dragStart.X) * slope;
					CalcPosition();
				} else {
					if(horiz) {
						location = mousePoint.Y;
					} else {
						location = mousePoint.X;
					}
				}
				return true;
			}
			if(rotating) {
				showRotated = rotated = true;
				if(rotateCenter.X == mousePoint.X) {
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

		void CalcPosition() {
			a = new Point((int)Math.Round(-intercept / slope), 0);
			b = new Point((int)Math.Round((owner.Height - intercept) / slope), owner.Height);
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
			if(horiz) {
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
			if(rotated) {
				if(Math.Abs(pt.Y - Math.Abs((pt.X * slope) + intercept)) < clickMargin) {
					return true;
				}
			} else {
				if(horiz) {
					if(Math.Abs(location - pt.Y) < clickMargin) {
						return true;
					}
				} else {
					//Debug.WriteLine(location + ", " + pt.X + ", " + Math.Abs(location - pt.X));
					if(Math.Abs(location - pt.X) < clickMargin) {
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
				if (mouseData > 7864320) //This is some internally defined value that I can't find
					location += delta;
				else
					location -= delta;
			}
		}
	}
}