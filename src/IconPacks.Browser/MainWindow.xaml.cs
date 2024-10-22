using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IconPacks.Browser.Properties;
using IconPacks.Browser.ViewModels;
using MahApps.Metro.Controls;

namespace IconPacks.Browser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            this.DataContext = new MainViewModel(this.Dispatcher);

            // Let's check if the preview size is valid
            Settings.Default.PropertyChanged += Settings_PropertyChanged;

            InitializeComponent();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.Default.IconPreviewSize):
                    if (Settings.Default.IconPreviewSize < 4) Settings.Default.IconPreviewSize = 4;
                    break;
            }
        }

        private void Find_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            FilterTextBox.Focus();
            Keyboard.Focus(FilterTextBox);
        }

        public void OnIconListPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (Keyboard.FocusedElement is not DependencyObject focusedElement) return;
            var listView = sender as ListBox;
            var currentItem = listView?.ItemContainerGenerator.ItemFromContainer(focusedElement);
            if (currentItem == null) return;

            int targetIndex;
            switch (e.Key)
            {
                case Key.Home:
                    var first = listView.ItemsSource.Cast<object>().First();
                    if (first != null)
                    {
                        listView.ScrollIntoView(first);
                    }

                    targetIndex = 0;
                    break;
                case Key.End:
                    var last = listView.ItemsSource.Cast<object>().Last();
                    if (last != null)
                    {
                        listView.ScrollIntoView(last);
                    }

                    targetIndex = listView.Items.Count - 1;
                    break;
                case Key.Left:
                    targetIndex = listView.Items.IndexOf(currentItem) - 1;
                    break;
                case Key.Right:
                    targetIndex = listView.Items.IndexOf(currentItem) + 1;
                    break;
                default:
                    return;
            }

            if (targetIndex >= 0 && targetIndex < listView.Items.Count)
            {
                (listView.ItemContainerGenerator.ContainerFromIndex(targetIndex) as UIElement)?.Focus();

                e.Handled = true;
            }
        }
    }
}