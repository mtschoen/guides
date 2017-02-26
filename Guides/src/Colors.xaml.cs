using System.Windows;
using System.Windows.Media;

namespace Guides
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Colors : Window
	{
		public static Brush ActiveBrush => Brushes.Cyan;
		public static Brush InactiveBrush => Brushes.Red;

		public Colors()
		{
			InitializeComponent();
		}
	}
}
