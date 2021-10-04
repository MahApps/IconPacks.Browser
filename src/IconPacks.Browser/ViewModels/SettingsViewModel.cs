using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows.Media;
using ControlzEx.Theming;
using IconPacks.Browser.Properties;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace IconPacks.Browser.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IDialogCoordinator dialogCoordinator;

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

        public SettingsViewModel(IDialogCoordinator dialogCoordinator)
        {
            this.dialogCoordinator = dialogCoordinator;
            CopyOriginalTemplatesCommand = new SimpleCommand((_) => CopyOriginalTemplatesCommand_Executed(), (_) => Directory.Exists(Settings.Default.ExportTemplatesDir));
            SelectTemplateFolderCommand = new SimpleCommand((_) => SelectTemplateFolderCommand_Executed());
            ClearTemplatesDirCommand = new SimpleCommand((_) => ClearTemplatesDirCommand_Executed(), (_) => !string.IsNullOrEmpty(Settings.Default.ExportTemplatesDir));
        }

        public static Dictionary<Color?, string> AccentColorNamesDictionary { get; }

        public static List<Color?> AccentColors { get; }

        public static void SetTheme()
        {
            Theme newTheme = new Theme("AppTheme", "AppTheme", Settings.Default.AppTheme, Settings.Default.AppAccentColor.ToString(), Settings.Default.AppAccentColor, new SolidColorBrush(Settings.Default.AppAccentColor), true, false);
            ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, newTheme);
        }

        public SimpleCommand SelectTemplateFolderCommand { get; }

        private async void SelectTemplateFolderCommand_Executed()
        {
            try
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog()
                {
                    IsFolderPicker = true,
                    Multiselect = false,
                    Title = "Select the Template Folder"
                };

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Settings.Default.ExportTemplatesDir = dialog.FileName;
                }
            }
            catch (Exception e)
            {
                await dialogCoordinator.ShowMessageAsync(this, "Error", e.Message);
            }
        }

        public SimpleCommand CopyOriginalTemplatesCommand { get; }

        private async void CopyOriginalTemplatesCommand_Executed()
        {
            List<string> failedItems = new List<string>();
            string[] originalTemplates = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportTemplates"));

            foreach (string template in originalTemplates)
            {
                //Do your job with "file"  
                string destination = Path.Combine(Settings.Default.ExportTemplatesDir, Path.GetFileName(template));
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
                await dialogCoordinator.ShowMessageAsync(this, "Templates already exists", $"The following files already exist in the templates folder. Either delete them or choose an empty folder. \n\n{string.Join(Environment.NewLine, failedItems)}");
            }

            MainViewModel.OpenUrlCommand.Execute(Settings.Default.ExportTemplatesDir);
        }

        public SimpleCommand ClearTemplatesDirCommand { get; }

        private void ClearTemplatesDirCommand_Executed()
        {
            Settings.Default.ExportTemplatesDir = null;
        }
    }
}