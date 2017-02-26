using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;

namespace Guides
{
	/// <summary>
	/// Interaction logic for ColorPicker.xaml
	/// </summary>
	public partial class ColorPicker : UserControl {
		readonly Dictionary<Color, ComboBoxItem> items = new Dictionary<Color, ComboBoxItem>();

		public event Action<SolidColorBrush> OnSelectionChanged;

		public ColorPicker() {
			InitializeComponent();

			var properties = typeof(Brushes).GetProperties(BindingFlags.Static | BindingFlags.Public);
			foreach (var prop in properties) {
				var brush = (SolidColorBrush) prop.GetValue(null, null);

				var item = new ComboBoxItem
				{
					Background = brush,
					Content = brush.Color.ToString()
				};
				items[brush.Color] = item;
				ComboBox.Items.Add(item);
			}
		}

		public void SetValue(SolidColorBrush brush) {
			ComboBox.SelectedItem = items[brush.Color];
		}

		void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var brush = ((ComboBoxItem) ComboBox.SelectedItem).Background;
			ComboBox.Foreground = brush;
			OnSelectionChanged?.Invoke((SolidColorBrush)brush);
		}
	}
}
