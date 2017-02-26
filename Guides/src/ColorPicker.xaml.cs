using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Guides.src
{
	/// <summary>
	/// Interaction logic for ColorPicker.xaml
	/// </summary>
	public partial class ColorPicker : UserControl
	{
		public ColorPicker() {
			InitializeComponent();

			var brushesType = typeof(Brushes);

			// Get all static properties
			var properties = brushesType.GetProperties(BindingFlags.Static | BindingFlags.Public);
			//var bc = new BrushConverter();

			foreach (var prop in properties) {
				var brush = (SolidColorBrush) prop.GetValue(null, null);

				ComboBox.Items.Add(brush);
			}
		}
	}
}
