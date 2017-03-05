using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Guides
{
	/// <summary>
	/// An extension of the Guide class that draws a line
	/// </summary>
	public class LineGuide : Guide {
		/// <summary>
		/// Whether this guide is horizontal
		/// </summary>
		public bool horiz {
			get { return geometry.EndPoint.X > 0; }
			set {
				rotated = false;
				var oldlocation = location;
				geometry.StartPoint = new Point(0, 0);
				geometry.EndPoint = value ? new Point(owner.Width, 0) : new Point(0, owner.Height);
				location = oldlocation;
			}
		}
		/// <summary>
		/// The screen location of this guide (from left if horiz, from top if vert)
		/// </summary>
		public double location { get {
				return horiz ? Canvas.GetTop(this) : Canvas.GetLeft(this);
			}
			set {
				if (horiz) {
					Canvas.SetTop(this, value - StrokeThickness * 0.5f);
					Canvas.SetLeft(this, 0);
				} else {
					Canvas.SetLeft(this, value - StrokeThickness * 0.5f);
					Canvas.SetTop(this, 0);
				}
				UpdateInfo(rotateCenter);
			}
		}

		double slope, intercept, interceptHold;
		Point rotateCenter;
		bool rotating, rotated, rotationChanged;

		Info info;

		class Info {
			public Label location;
		}

		protected override Geometry DefiningGeometry => geometry;
		readonly LineGeometry geometry = new LineGeometry();
		Ellipse rotateCircle;

		public LineGuide(Overlay owner, double location) : base(owner) {
			horiz = true;
			this.location = location;
		}

		/// <summary>
		/// Respond to mouse motion
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		/// <returns>True if this guide did anything</returns>
		public override bool OnMouseMove(Point mousePoint) {
			var result = false;
			if(dragging) {
				if(rotated) {
					intercept = interceptHold + mousePoint.Y - dragStart.Y - (mousePoint.X - dragStart.X) * slope;
					CalcPosition();
				} else {
					location = horiz ? mousePoint.Y : mousePoint.X;
				}
				result = true;

				if (!rotating) {
					rotateCenter = mousePoint;
					UpdateInfo(rotateCenter);
				}
			}
			if(rotating) {
				ActiveGuide = this;
				const float circleWidth = 10f;
				const float circleHeight = 10f;

				if (!rotationChanged || dragging) {
					rotateCenter = mousePoint;
					var rotationWasChanged = rotationChanged;
					if (!rotationChanged) {
						rotateCircle = new Ellipse {
							Fill = Colors.ActiveBrush,
							Width = circleWidth,
							Height = circleHeight
						};
						owner.Canvas.Children.Add(rotateCircle);
						rotationChanged = true;
					}

					Canvas.SetLeft(rotateCircle, mousePoint.X - circleWidth * 0.5f);
					Canvas.SetTop(rotateCircle, mousePoint.Y - circleHeight * 0.5f);

					if (!rotationWasChanged)
						return true;
				}

				Canvas.SetTop(this, 0);
				Canvas.SetLeft(this, 0);
				if (!dragging) {
					var newSlope = (rotateCenter.Y - mousePoint.Y) / (rotateCenter.X - mousePoint.X);
					if (!double.IsNaN(newSlope)) {
						slope = newSlope;
						rotated = true;
					}
				}
				intercept = rotateCenter.Y - slope * rotateCenter.X;
				CalcPosition();
				UpdateInfo(rotateCenter);
				result = true;
			}

			return result;
		}

		void CalcPosition() {
			if (Math.Abs(slope) > 1000) {
				horiz = false;
				location = rotateCenter.X;
			} else if (Math.Abs(slope) < 0.001) {
				horiz = true;
				location = rotateCenter.Y;
			} else {
				geometry.StartPoint = new Point((int)Math.Round(-intercept / slope), 0);
				geometry.EndPoint = new Point((int)Math.Round((owner.Height - intercept) / slope), owner.Height);
			}
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
				return true;
			}
			return false;
		}
		/// <summary>
		/// The Up event for the rotate button
		/// </summary>
		/// <param name="mousePoint">Mouse position</param>
		public override bool OnRightMouseUp(Point mousePoint) {
			if (ActiveGuide != null && !ReferenceEquals(ActiveGuide, this))
				return false;

			ActiveGuide = null;

			rotating = false;
			if (rotateCircle != null)
				owner.Canvas.Children.Remove(rotateCircle);

			if (rotationChanged) {
				rotationChanged = false;
				return true;
			}

			if(horiz) {
				if(Intersects(mousePoint)) {
					horiz = false;
					location = mousePoint.X;
					rotated = false;
					return true;
				}
			} else {
				if(Intersects(mousePoint)) {
					horiz = true;
					location = mousePoint.Y;
					rotated = false;
					return true;
				}
			}

			return false;
		}
		/// <summary>
		/// Checks if pointer intersects within clickMargin of line
		/// </summary>
		/// <param name="pt"></param>
		/// <returns></returns>
		public override bool Intersects(Point pt) {
			if(rotated) {
				//Debug.WriteLine($"rotated {pt.Y} - {pt.X} * {slope} + {intercept})) {Math.Abs(pt.Y - Math.Abs(pt.X * slope + intercept))}");
				if (Math.Abs(pt.Y - Math.Abs(pt.X * slope + intercept)) < ClickMargin) {
					return true;
				}
			} else {
				if(horiz) {
					//Debug.WriteLine(location + ", " + pt.Y + ", " + Math.Abs(location - pt.Y));
					if (Math.Abs(location - pt.Y) < ClickMargin) {
						return true;
					}
				} else {
					//Debug.WriteLine(location + ", " + pt.X + ", " + Math.Abs(location - pt.X));
					if (Math.Abs(location - pt.X) < ClickMargin) {
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
			if (!active) return;

			if (mouseData > 7864320) //This is some internally defined value that I can't find
				location += delta;
			else
				location -= delta;
		}

		public override void OnInfoKey() {
			base.OnInfoKey();

			if (info == null) {
				var locationLabel = new Label();
				owner.Canvas.Children.Add(locationLabel);
				info = new Info {location = locationLabel};
				UpdateInfo(rotateCenter);
			} else {
				owner.Canvas.Children.Remove(info.location);
				info = null;
			}
		}

		void UpdateInfo(Point mousePoint) {
			if (info == null) return;

			var locationLabel = info.location;
			if (rotated) {
				locationLabel.Content = $@"({mousePoint.X * owner.ResolutionScaleX:f0}, {mousePoint.Y * owner.ResolutionScaleY:f0})
{slope:f5}
{Math.Atan(slope) * (180.0 / Math.PI):f2}°";
				Canvas.SetTop(locationLabel, mousePoint.Y);
				Canvas.SetLeft(locationLabel, mousePoint.X);
			} else {
				locationLabel.Content = $"{location * owner.ResolutionScaleX:f0}";
				if (horiz) {
					Canvas.SetTop(locationLabel, location);
					Canvas.SetLeft(locationLabel, 0);
				}
				else {
					Canvas.SetTop(locationLabel, 0);
					Canvas.SetLeft(locationLabel, location + StrokeThickness);
				}
			}
		}

		public override string ToString() {
			var startPoint = geometry.StartPoint;
			var endPoint = geometry.EndPoint;
			return $"LineGuide horiz:{horiz} location:{location:f2} rotated:{rotated} ({startPoint.X}, {startPoint.Y}) - ({endPoint.X}, {endPoint.Y}) active:{active} dragging:{dragging}";
		}
	}
}