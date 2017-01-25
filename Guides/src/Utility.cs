using System;
using System.Windows;

namespace Guides {
	/// <summary>
	/// Miscellaneous utility methods
	/// </summary>
	public static class Utility {
		/// <summary>
		/// Returns the distance between two points
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static double Distance(Point a, Point b) {
			var dx = a.X - b.X;
			var dy = a.Y - b.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}
	}
}
