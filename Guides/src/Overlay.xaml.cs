using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using InputHook;

namespace Guides {
	//TODO: Add opacity setting
	//TODO: Change cursor for horizontal/vertical lines
	//TODO: Save/load guide sets
	//TODO: Color settings

	/// <summary>
	/// Interaction logic for Overlay.xaml
	/// </summary>
	public partial class Overlay {
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

		public float resolutionScale = 1; //Scaling parameter for global DPI scaling

		public Overlay() {
			InitializeComponent();
		}

		/// <summary>
		/// Mouse Down event for Left mouse button
		/// </summary>
		/// <param name="mouseStruct">The mouse parameters</param>
		public void OnLeftMouseDown(MSLLHOOKSTRUCT mouseStruct) {
			var rect = new Rectangle();
			rect.Width = 10;
			rect.Height = 10;

			rect.Stroke = Brushes.LightBlue;
			rect.StrokeThickness = 2;

			canvas.Children.Add(rect);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			WindowState = WindowState.Maximized;
			Topmost = true;
			Console.WriteLine(Top + ", " + Left + ", " + Width + ", " + Height);
			//canvas.Height = Height;
			//canvas.Width = Width;

			var rect = new Rectangle();
			rect.Height = Height - 10;
			rect.Width = Width - 10;

			rect.Stroke = Brushes.LightBlue;
			rect.StrokeThickness = 5;

			canvas.Children.Add(rect);
			Canvas.SetTop(rect, 5);
			Canvas.SetLeft(rect, 5);

			rect = new Rectangle();
			rect.Height = 10;
			rect.Width = 10;

			rect.Stroke = Brushes.LightBlue;
			rect.StrokeThickness = 5;

			canvas.Children.Add(rect);
			Canvas.SetTop(rect, Height / 2);
			Canvas.SetLeft(rect, Width / 2);
		}
	}
}
