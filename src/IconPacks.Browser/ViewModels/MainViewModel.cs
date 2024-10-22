using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MahApps.Metro.IconPacks;

namespace IconPacks.Browser.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly Dispatcher _dispatcher;
        private string _filterText;
        private IconPackViewModel _selectedIconPack;

        public MainViewModel(Dispatcher dispatcher)
        {
            this._dispatcher = dispatcher;
            this.AppVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

            var availableIconPacks = new List<(Type EnumType, Type IconPackType)>(
                new[]
                {
                    (typeof(PackIconBootstrapIconsKind), typeof(PackIconBootstrapIcons)),
                    (typeof(PackIconBoxIconsKind), typeof(PackIconBoxIcons)),
                    (typeof(PackIconCircumIconsKind), typeof(PackIconCircumIcons)),
                    (typeof(PackIconCodiconsKind), typeof(PackIconCodicons)),
                    (typeof(PackIconCooliconsKind), typeof(PackIconCoolicons)),
                    (typeof(PackIconEntypoKind), typeof(PackIconEntypo)),
                    (typeof(PackIconEvaIconsKind), typeof(PackIconEvaIcons)),
                    (typeof(PackIconFeatherIconsKind), typeof(PackIconFeatherIcons)),
                    (typeof(PackIconFileIconsKind), typeof(PackIconFileIcons)),
                    (typeof(PackIconFontaudioKind), typeof(PackIconFontaudio)),
                    (typeof(PackIconFontAwesomeKind), typeof(PackIconFontAwesome)),
                    (typeof(PackIconFontistoKind), typeof(PackIconFontisto)),
                    (typeof(PackIconForkAwesomeKind), typeof(PackIconForkAwesome)),
                    (typeof(PackIconGameIconsKind), typeof(PackIconGameIcons)),
                    (typeof(PackIconIoniconsKind), typeof(PackIconIonicons)),
                    (typeof(PackIconJamIconsKind), typeof(PackIconJamIcons)),
                    (typeof(PackIconLucideKind), typeof(PackIconLucide)),
                    (typeof(PackIconMaterialKind), typeof(PackIconMaterial)),
                    (typeof(PackIconMaterialLightKind), typeof(PackIconMaterialLight)),
                    (typeof(PackIconMaterialDesignKind), typeof(PackIconMaterialDesign)),
                    (typeof(PackIconMemoryIconsKind), typeof(PackIconMemoryIcons)),
                    (typeof(PackIconMicronsKind), typeof(PackIconMicrons)),
                    (typeof(PackIconModernKind), typeof(PackIconModern)),
                    (typeof(PackIconOcticonsKind), typeof(PackIconOcticons)),
                    (typeof(PackIconPhosphorIconsKind), typeof(PackIconPhosphorIcons)),
                    (typeof(PackIconPicolIconsKind), typeof(PackIconPicolIcons)),
                    (typeof(PackIconPixelartIconsKind), typeof(PackIconPixelartIcons)),
                    (typeof(PackIconRadixIconsKind), typeof(PackIconRadixIcons)),
                    (typeof(PackIconRemixIconKind), typeof(PackIconRemixIcon)),
                    (typeof(PackIconRPGAwesomeKind), typeof(PackIconRPGAwesome)),
                    (typeof(PackIconSimpleIconsKind), typeof(PackIconSimpleIcons)),
                    (typeof(PackIconTypiconsKind), typeof(PackIconTypicons)),
                    (typeof(PackIconUniconsKind), typeof(PackIconUnicons)),
                    (typeof(PackIconVaadinIconsKind), typeof(PackIconVaadinIcons)),
                    (typeof(PackIconWeatherIconsKind), typeof(PackIconWeatherIcons)),
                    (typeof(PackIconZondiconsKind), typeof(PackIconZondicons)),
                });

            var coll = new ObservableCollection<IconPackViewModel>();

            foreach (var (enumType, iconPackType) in availableIconPacks)
            {
                coll.Add(new IconPackViewModel(this, enumType, iconPackType));
            }

            this.IconPacks = coll;

            this.AllIconPacksCollection = new List<IconPackViewModel>(new[]
            {
                new IconPackViewModel(
                    this,
                    "All Icons",
                    availableIconPacks.Select(x => x.EnumType).ToArray(),
                    availableIconPacks.Select(x => x.IconPackType).ToArray()
                )
            });

            this.IconPacksVersion = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(PackIconMaterial)).Location).FileVersion;

            this.Settings = new SettingsViewModel();
        }

        public IconPackViewModel SelectedIconPack
        {
            get => _selectedIconPack;
            set
            {
                if (Set(ref _selectedIconPack, value))
                {
                    this.ApplyFilterText(this.FilterText);
                }
            }
        }

        public ObservableCollection<IconPackViewModel> IconPacks { get; }

        public List<IconPackViewModel> AllIconPacksCollection { get; }

        public string AppVersion { get; }

        public string IconPacksVersion { get; }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (Set(ref _filterText, value))
                {
                    this.ApplyFilterText(value);
                }
            }
        }

        private void ApplyFilterText(string filterText)
        {
            if (this.SelectedIconPack is not null)
            {
                this._dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.SelectedIconPack.FilterText = filterText));
                foreach (var iconPack in this.IconPacks.Concat(this.AllIconPacksCollection).Except(new[] { this.SelectedIconPack }))
                {
                    this._dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => iconPack.FilterText = filterText));
                }
            }
            else
            {
                foreach (var iconPack in this.IconPacks.Concat(this.AllIconPacksCollection))
                {
                    this._dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => iconPack.FilterText = filterText));
                }
            }
        }

        private static void DoCopyTextToClipboard(string text)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => { Clipboard.SetDataObject(text); }));
        }

        public static ICommand CopyTextToClipboardCommand { get; } =
            new SimpleCommand
            {
                CanExecuteDelegate = x => (x is string),
                ExecuteDelegate = x => DoCopyTextToClipboard((string)x)
            };

        public static ICommand CopyToClipboardTextCommand { get; } =
            new SimpleCommand
            {
                CanExecuteDelegate = x => (x is IIconViewModel),
                ExecuteDelegate = x => DoCopyTextToClipboard(((IIconViewModel)x).CopyToClipboardText)
            };

        public static ICommand CopyToClipboardAsContentTextCommand { get; } =
            new SimpleCommand
            {
                CanExecuteDelegate = x => (x is IIconViewModel),
                ExecuteDelegate = x => DoCopyTextToClipboard(((IIconViewModel)x).CopyToClipboardAsContentText)
            };

        public static ICommand CopyToClipboardAsPathIconTextCommand { get; } =
            new SimpleCommand
            {
                CanExecuteDelegate = x => (x is IIconViewModel),
                ExecuteDelegate = x => DoCopyTextToClipboard(((IIconViewModel)x).CopyToClipboardAsPathIconText)
            };

        public static ICommand CopyToClipboardAsGeometryTextCommand { get; } =
            new SimpleCommand
            {
                CanExecuteDelegate = x => (x is IIconViewModel),
                ExecuteDelegate = x => DoCopyTextToClipboard(((IIconViewModel)x).CopyToClipboardAsGeometryText)
            };

        public SettingsViewModel Settings { get; }
    }
}