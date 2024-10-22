using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Media;
using AsyncAwaitBestPractices.MVVM;
using ControlzEx.Theming;
using IconPacks.Browser.Properties;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace IconPacks.Browser.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        static SettingsViewModel()
        {
            AccentColorNamesDictionary = new Dictionary<Color?, string>();

            ResourceManager rm = new ResourceManager(typeof(AccentColorNames));
            ResourceSet resourceSet = rm.GetResourceSet(CultureInfo.CurrentUICulture, true, true);

            if (resourceSet != null)
            {
                foreach (DictionaryEntry entry in resourceSet.OfType<DictionaryEntry>())
                {
                    try
                    {
                        if (ColorConverter.ConvertFromString(entry.Key.ToString()) is Color color)
                        {
                            AccentColorNamesDictionary.Add(color, entry.Value.ToString());
                        }
                    }
                    catch (Exception)
                    {
                        Trace.TraceError($"{entry.Key} is not a valid color key!");
                    }
                }
            }

            AccentColors = new List<Color?>(AccentColorNamesDictionary.Keys);
        }

        public SettingsViewModel()
        {
            CopyOriginalTemplatesCommand = new AsyncCommand(CopyOriginalTemplatesAsync, _ => Directory.Exists(Settings.Default.ExportTemplatesDir));
            SelectTemplateFolderCommand = new AsyncCommand(SelectTemplateFolderAsync);
            ClearTemplatesDirCommand = new AsyncCommand(ClearTemplatesAsync, (_) => !string.IsNullOrEmpty(Settings.Default.ExportTemplatesDir));

            CopyOriginalTemplatesCommand.RaiseCanExecuteChanged();
            ClearTemplatesDirCommand.RaiseCanExecuteChanged();
        }

        public static Dictionary<Color?, string> AccentColorNamesDictionary { get; }

        public static List<Color?> AccentColors { get; }

        public static void SetTheme()
        {
            Theme newTheme = new Theme("AppTheme", "AppTheme", Settings.Default.AppTheme, Settings.Default.AppAccentColor.ToString(), Settings.Default.AppAccentColor, new SolidColorBrush(Settings.Default.AppAccentColor), true, false);
            ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, newTheme);
        }

        public IAsyncCommand SelectTemplateFolderCommand { get; }

        private async Task SelectTemplateFolderAsync()
        {
            try
            {
                var fileDialog = new CommonOpenFileDialog()
                {
                    IsFolderPicker = true,
                    Multiselect = false,
                    Title = "Select the Template Folder"
                };

                if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Settings.Default.ExportTemplatesDir = fileDialog.FileName;
                    CopyOriginalTemplatesCommand?.RaiseCanExecuteChanged();
                    ClearTemplatesDirCommand?.RaiseCanExecuteChanged();
                }
            }
            catch (Exception e)
            {
                await dialogCoordinator.ShowMessageAsync(this, "Error", e.Message);
            }
        }

        public IAsyncCommand CopyOriginalTemplatesCommand { get; }

        private async Task CopyOriginalTemplatesAsync()
        {
            var failedItems = new List<string>();
            var originalTemplates = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportTemplates"));

            foreach (var template in originalTemplates)
            {
                //Do your job with "file"  
                var destination = Path.Combine(Settings.Default.ExportTemplatesDir, Path.GetFileName(template));
                if (!File.Exists(destination))
                {
                    File.Copy(template, destination);
                }
                else
                {
                    failedItems.Add("• " + Path.GetFileName(template));
                }
            }

            if (failedItems.Count > 0)
            {
                await dialogCoordinator.ShowMessageAsync(
                    this,
                    "Templates already exists",
                    $"The following files already exist in the templates folder. Either delete them or choose an empty folder. \n\n{string.Join(Environment.NewLine, failedItems)}"
                );
            }

            await this.OpenUrlLink(Settings.Default.ExportTemplatesDir);
        }

        public IAsyncCommand ClearTemplatesDirCommand { get; }

        private Task ClearTemplatesAsync()
        {
            Settings.Default.ExportTemplatesDir = null;

            CopyOriginalTemplatesCommand?.RaiseCanExecuteChanged();
            ClearTemplatesDirCommand?.RaiseCanExecuteChanged();

            return Task.CompletedTask;
        }
    }
}