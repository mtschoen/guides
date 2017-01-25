using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Guides {
	/// <summary>
	/// A class to represent individual on-screen guides.  These objects draw themselves and interpret input thorugh a series of callbacks
	/// </summary>
	public abstract class Guide : Shape {
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
		/// The window that owns this guide
		/// </summary>
		public Overlay owner { get; set; }

		/// <summary>
		/// The point where dragging started
		/// </summary>
		//protected Point dragStart { get; set; }

		/// <summary>
		/// Shared pen for drawing
		/// </summary>
		//protected Pen pen { get; set; }

		public Guide(Overlay owner) {
			if (owner == null)
				throw new ArgumentNullException();
			this.owner = owner;
			active = true;
		}

		/// <summary>
		/// Draw function for each individual guide
		/// </summary>
		/// <param name="g"></param>
		//public virtual void Draw(Graphics g) {
		//	pen = new Pen(Color.Red);
		//	if (active)
		//		pen = new Pen(Color.Cyan);
		//	pen.Width = 2; //Make it a little wider so you can click it
		//}

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
				//dragStart = mousePoint;
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
		public virtual bool OnKeyDown(Key key) {
			return false;
		}

		/// <summary>
		/// Dispose method (disposes pen if exists)
		/// </summary>
		/// <param name="disposing"></param>
		//protected virtual void Dispose(bool disposing) {
		//	if (disposing) {
		//		if (pen != null)
		//			pen.Dispose();
		//	}
		//}

		///// <summary>
		///// Dispose method (disposes pen if exists)
		///// </summary>
		//public void Dispose() {
		//	Dispose(true);
		//	GC.SuppressFinalize(this);
		//}
	}
}