using System;
using System.Windows.Forms;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Guides {
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
		public double radius { get; set; }
		double reticuleLength = 7;
		double radHold;
		double centerDist, scaleDist, scaleAngle;

		bool scaling, anchorScaling, wheelScaling;
		/// <summary>
		/// Whether to draw reticule lines perpendicular to the horizontal/vertical tangents
		/// </summary>
		public bool reticule { get; set; }
		//Rectangle circRect {
		//	get {
		//		return new Rectangle(center.X - radius, center.Y - radius, radius + radius, radius + radius);
		//	}
		//}

		public CircleGuide(Overlay owner, Point center) : this(owner, center, 50) { }
		public CircleGuide(Overlay owner, Point center, int radius)
			: base(owner) {
			this.center = center;
			this.radius = radius;
		}

		/// <summary>
		/// Draws the guide to the screen
		/// </summary>
		/// <param name="g"></param>
		//public override void Draw(Graphics g) {
		//	base.Draw(g);
		//	if (anchorScaling) {
		//		SolidBrush br = new SolidBrush(Color.Red);
		//		g.FillEllipse(br, owner.resolutionScale * dragStart.X - 5, owner.resolutionScale * dragStart.Y - 5, 10, 10);
		//		br.Dispose();
		//	}
		//	Rectangle localCircRect = new Rectangle();
		//	localCircRect.X = (int)(circRect.X * owner.resolutionScale);
		//	localCircRect.Y = (int)(circRect.Y * owner.resolutionScale);
		//	localCircRect.Width = (int)(circRect.Width * owner.resolutionScale);
		//	localCircRect.Height = (int)(circRect.Height * owner.resolutionScale);

		//	g.DrawEllipse(pen, localCircRect);
		//	pen.Color = Color.Black;
		//	pen.Width = 1;
		//	if (reticule) {

		//		Point localCenter = new Point();
		//		localCenter.X = (int)(center.X * owner.resolutionScale);
		//		localCenter.Y = (int)(center.Y * owner.resolutionScale);

		//		g.DrawLine(pen, localCenter.X, localCenter.Y + radius + reticuleLength, localCenter.X, localCenter.Y + radius - reticuleLength);
		//		g.DrawLine(pen, localCenter.X + radius + reticuleLength, localCenter.Y, localCenter.X + radius - reticuleLength, localCenter.Y);
		//		g.DrawLine(pen, localCenter.X, localCenter.Y - radius + reticuleLength, localCenter.X, localCenter.Y - radius - reticuleLength);
		//		g.DrawLine(pen, localCenter.X - radius + reticuleLength, localCenter.Y, localCenter.X - radius - reticuleLength, localCenter.Y);
		//	}
		//	pen.Dispose();
		//}
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
				if (!App.alt)
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
			if (base.OnLeftMouseDown(mousePoint)) {
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
			if (base.OnRightMouseDown(mousePoint)) {
				if (App.shift) {
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
				if (mouseData > 7864320)        //This is some internally defined value that I can't find
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
				if (App.ctrl && App.alt && key == Keys.R) {
					reticule = !reticule;
					return true;
				}
			}
			return false;
		}

		protected override Geometry DefiningGeometry { get; }
	}
}
