using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;
using Label = System.Windows.Controls.Label;

namespace Guides {
	/// <summary>
	/// Extension of Guide class to draw circular guides
	/// </summary>
	public class CircleGuide : Guide {

		/// <summary>
		/// The center of the circle
		/// </summary>
		public Point center {
			get { return geometry.Center; }
			set { geometry.Center = value; }
		}
		Point centerHold;

		/// <summary>
		/// The radius of the circle
		/// </summary>
		public double radius {
			get { return geometry.RadiusX; }
			set {
				geometry.RadiusX = value;
				geometry.RadiusY = value;
			}
		}

		const double ReticuleLength = 7;
		double radHold;
		double centerDist, scaleDist, scaleAngle;

		bool scaling, anchorScaling, wheelScaling;
		Ellipse rotateCircle;
		bool reticule;
		Info info;

		readonly Line[] reticuleLines = new Line[6];

		class Info {
			public Label center;
		}

		protected override Geometry DefiningGeometry => geometry;
		readonly EllipseGeometry geometry = new EllipseGeometry();

		public CircleGuide(Overlay owner, Point center) : this(owner, center, 50) { }
		public CircleGuide(Overlay owner, Point center, int radius)
			: base(owner) {
			this.center = center;
			this.radius = radius;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		/// <returns></returns>
		public override bool OnMouseMove(Point mousePoint) {
			if (dragging) {
				var center = geometry.Center;
				center.X = centerHold.X + (mousePoint.X - dragStart.X);
				center.Y = centerHold.Y + (mousePoint.Y - dragStart.Y);
				geometry.Center = center;
				UpdateReticule();
				UpdateInfo();
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
				UpdateReticule();
				UpdateInfo();
				return true;
			}
			return false;
		}

		void UpdateReticule() {
			if (!reticule) return;

			reticuleLines[0].X1 = center.X;
			reticuleLines[0].Y1 = center.Y + radius + ReticuleLength;
			reticuleLines[0].X2 = center.X;
			reticuleLines[0].Y2 = center.Y + radius - ReticuleLength;

			reticuleLines[1].X1 = center.X + radius + ReticuleLength;
			reticuleLines[1].Y1 = center.Y;
			reticuleLines[1].X2 = center.X + radius - ReticuleLength;
			reticuleLines[1].Y2 = center.Y;

			reticuleLines[2].X1 = center.X;
			reticuleLines[2].Y1 = center.Y - radius + ReticuleLength;
			reticuleLines[2].X2 = center.X;
			reticuleLines[2].Y2 = center.Y - radius - ReticuleLength;

			reticuleLines[3].X1 = center.X - radius + ReticuleLength;
			reticuleLines[3].Y1 = center.Y;
			reticuleLines[3].X2 = center.X - radius - ReticuleLength;
			reticuleLines[3].Y2 = center.Y;

			var halfReticuleLength = ReticuleLength * 0.5f;
			reticuleLines[4].X1 = center.X + halfReticuleLength;
			reticuleLines[4].Y1 = center.Y;
			reticuleLines[4].X2 = center.X - halfReticuleLength;
			reticuleLines[4].Y2 = center.Y;

			reticuleLines[5].X1 = center.X;
			reticuleLines[5].Y1 = center.Y + halfReticuleLength;
			reticuleLines[5].X2 = center.X;
			reticuleLines[5].Y2 = center.Y - halfReticuleLength;
		}

		void UpdateInfo() {
			if (info == null) return;

			info.center.Content = 
				$@"({center.X * owner.ResolutionScaleX:f0}, {center.Y * owner.ResolutionScaleY:f0})
{radius * owner.ResolutionScaleX}";
			Canvas.SetTop(info.center, center.Y);
			Canvas.SetLeft(info.center, center.X);
		}

		void AnchorScale(Point mousePoint, double dist) {
			radius = radHold + (int)Math.Round(dist);
			var dx = dragStart.X - mousePoint.X;
			var dy = dragStart.Y - mousePoint.Y;
			if (!(Math.Abs(dx) > 0.00001)) return;

			if (!App.Alt)
				scaleAngle = Math.Atan(dy / dx);

			var d = centerDist + dist;
			if (dx < 0) {
				dx = Math.Cos(scaleAngle) * d;
				dy = Math.Sin(scaleAngle) * d;
			} else {
				dx = -Math.Cos(scaleAngle) * d;
				dy = -Math.Sin(scaleAngle) * d;
			}

			var center = geometry.Center;
			center.X = dragStart.X + (int)Math.Round(dx);
			center.Y = dragStart.Y + (int)Math.Round(dy);
			geometry.Center = center;
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
				if (App.Shift) {
					anchorScaling = true;
					centerDist = Utility.Distance(center, mousePoint);
					radHold = radius;
					dragStart = mousePoint;
					const float circleWidth = 10f;
					const float circleHeight = 10f;

					rotateCircle = new Ellipse {
						Fill = Colors.ActiveBrush,
						Width = circleWidth,
						Height = circleHeight
					};
					owner.Canvas.Children.Add(rotateCircle);

					Canvas.SetLeft(rotateCircle, mousePoint.X - circleWidth * 0.5f);
					Canvas.SetTop(rotateCircle, mousePoint.Y - circleHeight * 0.5f);
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
		public override bool OnRightMouseUp(Point mousePoint) {
			scaling = false;
			anchorScaling = false;
			if (rotateCircle != null)
				owner.Canvas.Children.Remove(rotateCircle);
			return false;
		}
		/// <summary>
		/// Check if a point is on the circle itself.  Does this by checking if distance from point to radius is within clickMargin of the circle's radius
		/// </summary>
		/// <param name="pt">The point</param>
		/// <returns></returns>
		public override bool Intersects(Point pt) {
			var dist = Utility.Distance(center, pt);
			return dist > Math.Abs(radius) - ClickMargin && dist < Math.Abs(radius) + ClickMargin;
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
			if (base.OnKeyDown(key))
				return true;

			if (active) {
				if (App.Ctrl && App.Alt && key == Keys.R) {
					reticule = !reticule;
					if (reticule) {
						for (var i = 0; i < reticuleLines.Length; i++) {
							var line = new Line {
								StrokeThickness = 1,
								Stroke = Brushes.Black
							};
							owner.Canvas.Children.Add(line);
							reticuleLines[i] = line;
						}
						UpdateReticule();
					} else {
						foreach (var line in reticuleLines) {
							owner.Canvas.Children.Remove(line);
						}
					}
					return true;
				}
			}
			return false;
		}

		public override void OnInfoKey() {
			if (info == null) {
				var centerLabel = new Label();
				owner.Canvas.Children.Add(centerLabel);
				info = new Info {center = centerLabel};

				UpdateInfo();
			}
			else
			{
				owner.Canvas.Children.Remove(info.center);
				info = null;
			}
		}

		public override string ToString()
		{
			return $"CircleGuide center:({center.X:f2}, {center.Y:f2}), radius:{radius:f2} active:{active} dragging:{dragging}";
		}
	}
}
