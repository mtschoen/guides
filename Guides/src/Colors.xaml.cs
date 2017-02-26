using Guides.Properties;
using System.Windows.Media;

namespace Guides
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Colors
	{
		public static SolidColorBrush ActiveBrush = Brushes.Cyan;
		public static SolidColorBrush InactiveBrush = Brushes.Red;

		static Colors() {
			ActiveBrush = Settings.Default.ActiveBrush;
			InactiveBrush = Settings.Default.InactiveBrush;
		}

		public Colors()
		{
			InitializeComponent();
		}

		private void Window_Initialized(object sender, System.EventArgs e) {
			ActiveColorPicker.SetValue(ActiveBrush);
			InactiveColorPicker.SetValue(InactiveBrush);

			ActiveColorPicker.OnSelectionChanged += brush => {
				ActiveBrush = brush;
				Settings.Default.ActiveBrush = brush;
				Settings.Default.Save();
			};
			InactiveColorPicker.OnSelectionChanged += brush => {
				InactiveBrush = brush;
				Settings.Default.InactiveBrush = brush;
				Settings.Default.Save();
			};
		}
	}
}
