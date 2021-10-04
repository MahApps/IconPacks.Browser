using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IconPacks.Browser.ViewModels;

namespace IconPacks.Browser.Controls
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void AccentColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            SettingsViewModel.SetTheme();
        }

        private void AppThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            SettingsViewModel.SetTheme();
        }
    }
}