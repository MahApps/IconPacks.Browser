using System.Windows;
using IconPacks.Browser.Properties;
using IconPacks.Browser.ViewModels;

namespace IconPacks.Browser
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SettingsViewModel.SetTheme();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Settings.Default.Save();
            base.OnExit(e);
        }
    }
}