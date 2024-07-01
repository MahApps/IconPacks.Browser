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

            // Let's check if the previewsize is valid
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

        /// <summary>
        /// Forward Cell click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridCell c && c.TryFindParent<Window>()!=null)
            {
                var frm = c.TryFindParent<Window>();
                if (frm.DataContext !=null && frm.DataContext is MainViewModel mvm)
                {
                    mvm.ThemeResourcesViewModel.DataGridCell_MouseDoubleClick(sender,e);
                }
            }
        }
    }
}