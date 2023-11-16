using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AsyncAwaitBestPractices;
using IconPacks.Browser.Model;
using IconPacks.Browser.Properties;
using JetBrains.Annotations;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Win32;
using IO = System.IO;

namespace IconPacks.Browser.ViewModels
{
    public class IconPackViewModel : ViewModelBase
    {
        private IEnumerable<IIconViewModel> _icons;
        private int _iconCount;
        private ICollectionView _iconsCollectionView;
        private string _filterText;
        private IIconViewModel _selectedIcon;
        private readonly IDialogCoordinator dialogCoordinator;

        private IconPackViewModel(MainViewModel mainViewModel, IDialogCoordinator dialogCoordinator)
        {
            this.MainViewModel = mainViewModel;
            this.dialogCoordinator = dialogCoordinator;

            // Export commands
            SaveAsSvgCommand = new SimpleCommand((_) => SaveAsSvg_Execute(), (_) => SelectedIcon is IconViewModel);
            SaveAsWpfCommand = new SimpleCommand((_) => SaveAsWpf_Execute(), (_) => SelectedIcon is not null);
            SaveAsUwpCommand = new SimpleCommand((_) => SaveAsUwp_Execute(), (_) => SelectedIcon is not null);

            SaveAsPngCommand = new SimpleCommand((_) => SaveAsBitmapExecute(new PngBitmapEncoder()), (_) => SelectedIcon is not null);
            SaveAsJpegCommand = new SimpleCommand((_) => SaveAsBitmapExecute(new JpegBitmapEncoder()), (_) => SelectedIcon is not null);
            SaveAsBmpCommand = new SimpleCommand((_) => SaveAsBitmapExecute(new BmpBitmapEncoder()), (_) => SelectedIcon is not null);
        }

        public IconPackViewModel(MainViewModel mainViewModel, Type enumType, Type packType, IDialogCoordinator dialogCoordinator)
            : this(mainViewModel, dialogCoordinator)
        {
            // Get the Name of the IconPack via Attributes
            this.MetaData = Attribute.GetCustomAttribute(packType, typeof(MetaDataAttribute)) as MetaDataAttribute;

            this.Caption = this.MetaData?.Name;

            this.LoadEnumsAsync(enumType, packType).SafeFireAndForget();
        }

        public IconPackViewModel(MainViewModel mainViewModel, string caption, Type[] enumTypes, Type[] packTypes, IDialogCoordinator dialogCoordinator)
            : this(mainViewModel, dialogCoordinator)
        {
            this.MainViewModel = mainViewModel;

            this.Caption = caption;

            this.LoadAllEnumsAsync(enumTypes, packTypes).SafeFireAndForget();
        }

        private async Task LoadEnumsAsync(Type enumType, Type packType)
        {
            var collection = await Task.Run(() => GetIcons(enumType, packType).OrderBy(i => i.Name, StringComparer.InvariantCultureIgnoreCase).ToList());

            this.Icons = new ObservableCollection<IIconViewModel>(collection);
            this.IconCount = collection.Count;
            this.PrepareFiltering();
            this.SelectedIcon = this.Icons.FirstOrDefault();
        }

        private async Task LoadAllEnumsAsync(Type[] enumTypes, Type[] packTypes)
        {
            var collection = await Task.Run(() =>
            {
                var allIcons = Enumerable.Empty<IIconViewModel>();
                for (var counter = 0; counter < enumTypes.Length; counter++)
                {
                    allIcons = allIcons.Concat(GetIcons(enumTypes[counter], packTypes[counter]));
                }

                return allIcons.OrderBy(i => i.Name, StringComparer.InvariantCultureIgnoreCase).ToList();
            });

            this.Icons = new ObservableCollection<IIconViewModel>(collection);
            this.IconCount = ((ICollection)this.Icons).Count;
            this.PrepareFiltering();
            this.SelectedIcon = this.Icons.First();
        }

        private void PrepareFiltering()
        {
            this._iconsCollectionView = CollectionViewSource.GetDefaultView(this.Icons);
            this._iconsCollectionView.Filter = o => FilterIconsPredicate(this.FilterText, (IIconViewModel)o);
        }

        private static bool FilterIconsPredicate(string filterText, IIconViewModel iconViewModel)
        {
            if (string.IsNullOrWhiteSpace(filterText))
            {
                return true;
            }
            else
            {
                var filterSubStrings = filterText.Split(new[] { '+', ',', ';', '&' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var filterSubString in filterSubStrings)
                {
                    var filterOrSubStrings = filterSubString.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                    var isInName = filterOrSubStrings.Any(x => iconViewModel.Name.IndexOf(x.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0);
                    var isInDescription = filterOrSubStrings.Any(x => (iconViewModel.Description?.IndexOf(x.Trim(), StringComparison.CurrentCultureIgnoreCase) ?? -1) >= 0);

                    if (!(isInName || isInDescription)) return false;
                }

                return true;
            }
        }

        private static MetaDataAttribute GetMetaData(Type packType)
        {
            var metaData = Attribute.GetCustomAttribute(packType, typeof(MetaDataAttribute)) as MetaDataAttribute;
            return metaData;
        }

        private static IEnumerable<IIconViewModel> GetIcons(Type enumType, Type packType)
        {
            var metaData = GetMetaData(packType);
            return Enum.GetValues(enumType)
                .OfType<Enum>()
                .Where(k => k.ToString() != "None")
                .Select(k => new IconViewModel(enumType, packType, k, metaData));
        }

        public MainViewModel MainViewModel { get; }

        public string Caption { get; }

        [CanBeNull] public MetaDataAttribute MetaData { get; }

        public IEnumerable<IIconViewModel> Icons
        {
            get => _icons;
            set => Set(ref _icons, value);
        }

        public int IconCount
        {
            get => _iconCount;
            set => Set(ref _iconCount, value);
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (Set(ref _filterText, value))
                {
                    this._iconsCollectionView?.Refresh();
                    this.OnPropertyChanged(nameof(SelectedIcon));
                }
            }
        }

        public IIconViewModel SelectedIcon
        {
            get => _selectedIcon;
            set
            {
                if (Set(ref _selectedIcon, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand SaveAsSvgCommand { get; }

        private async void SaveAsSvg_Execute()
        {
            var progress = await dialogCoordinator.ShowProgressAsync(MainViewModel, "Export", "Saving selected icon as SVG-file");
            progress.SetIndeterminate();

            try
            {
                var fileSaveDialog = new SaveFileDialog()
                {
                    AddExtension = true,
                    DefaultExt = "svg",
                    FileName = $"{SelectedIcon.IconPackName}-{SelectedIcon.Name}",
                    Filter = "SVG Drawing (*.svg)|*.svg",
                    OverwritePrompt = true
                };

                if (fileSaveDialog.ShowDialog() == true && SelectedIcon is IconViewModel icon)
                {
                    var iconControl = icon.GetPackIconControlBase();

                    iconControl.BeginInit();
                    iconControl.Width = Settings.Default.IconPreviewSize;
                    iconControl.Height = Settings.Default.IconPreviewSize;
                    iconControl.EndInit();
                    iconControl.ApplyTemplate();

                    var iconPath = iconControl.FindChild<Path>();

                    var bBox = iconPath.Data.Bounds;

                    var svgSize = Math.Max(bBox.Width, bBox.Height);
                    var scaleFactor = Settings.Default.IconPreviewSize / svgSize;
                    var T = iconPath.LayoutTransform.Value;

                    T.Translate(-bBox.Left - (T.M11 < 0 ? bBox.Width : 0) + Math.Sign(T.M11) * (svgSize - bBox.Width) / 2,
                        -bBox.Top - (T.M22 < 0 ? bBox.Height : 0) + Math.Sign(T.M22) * (svgSize - bBox.Height) / 2);
                    T.Scale(scaleFactor, scaleFactor);

                    var transform = string.Join(",", new[]
                    {
                        T.M11.ToString(CultureInfo.InvariantCulture),
                        T.M21.ToString(CultureInfo.InvariantCulture),
                        T.M12.ToString(CultureInfo.InvariantCulture),
                        T.M22.ToString(CultureInfo.InvariantCulture),
                        (Math.Sign(T.M11) * T.OffsetX).ToString(CultureInfo.InvariantCulture),
                        (Math.Sign(T.M22) * T.OffsetY).ToString(CultureInfo.InvariantCulture)
                    });

                    var parameters = new ExportParameters(SelectedIcon)
                    {
                        FillColor = iconPath.Fill is not null ? Settings.Default.IconForeground.ToString(CultureInfo.InvariantCulture).Remove(1, 2) : "none", // We need to remove the alpha channel for svg
                        Background = Settings.Default.IconBackground.ToString(CultureInfo.InvariantCulture).Remove(1, 2),
                        PathData = iconControl.Data,
                        StrokeColor = iconPath.Stroke is not null ? Settings.Default.IconForeground.ToString(CultureInfo.InvariantCulture).Remove(1, 2) : "none", // We need to remove the alpha channel for svg
                        StrokeWidth = iconPath.Stroke is null ? "0" : (scaleFactor * iconPath.StrokeThickness).ToString(CultureInfo.InvariantCulture),
                        StrokeLineCap = iconPath.StrokeEndLineCap.ToString().ToLower(),
                        StrokeLineJoin = iconPath.StrokeLineJoin.ToString().ToLower(),
                        TransformMatrix = transform
                    };

                    var svgFileTemplate = ExportHelper.SvgFileTemplate;

                    var svgFileContent = ExportHelper.FillTemplate(svgFileTemplate, parameters);

                    using IO.StreamWriter file = new IO.StreamWriter(fileSaveDialog.FileName);
                    await file.WriteAsync(svgFileContent);
                }
            }
            catch (Exception e)
            {
                await dialogCoordinator.ShowMessageAsync(MainViewModel, "Error", e.Message);
            }

            await progress.CloseAsync();
        }

        public ICommand SaveAsWpfCommand { get; }

        private async void SaveAsWpf_Execute()
        {
            var progress = await dialogCoordinator.ShowProgressAsync(MainViewModel, "Export", "Saving selected icon as WPF-XAML-file");
            progress.SetIndeterminate();

            try
            {
                var fileSaveDialog = new SaveFileDialog()
                {
                    AddExtension = true,
                    DefaultExt = "xaml",
                    FileName = $"{SelectedIcon.IconPackName}-{SelectedIcon.Name}",
                    Filter = "WPF-XAML (*.xaml)|*.xaml",
                    OverwritePrompt = true
                };

                if (fileSaveDialog.ShowDialog() == true && SelectedIcon is IconViewModel icon)
                {
                    var iconControl = icon.GetPackIconControlBase();

                    iconControl.BeginInit();
                    iconControl.Width = Settings.Default.IconPreviewSize;
                    iconControl.Height = Settings.Default.IconPreviewSize;
                    iconControl.EndInit();
                    iconControl.ApplyTemplate();

                    var iconPath = iconControl.FindChild<Path>();

                    var bBox = iconPath.Data.Bounds;

                    var xamlSize = Math.Max(bBox.Width, bBox.Height);
                    var T = iconPath.LayoutTransform.Value;

                    var scaleFactor = Settings.Default.IconPreviewSize / xamlSize;

                    var wpfFileTemplate = ExportHelper.WpfFileTemplate;

                    var parameters = new ExportParameters(SelectedIcon)
                    {
                        FillColor = iconPath.Fill is not null ? Settings.Default.IconForeground.ToString(CultureInfo.InvariantCulture) : "{x:Null}",
                        PathData = iconControl.Data,
                        StrokeColor = iconPath.Stroke is not null ? Settings.Default.IconForeground.ToString(CultureInfo.InvariantCulture) : "{x:Null}",
                        StrokeWidth = iconPath.Stroke is null ? "0" : (scaleFactor * iconPath.StrokeThickness).ToString(CultureInfo.InvariantCulture),
                        StrokeLineCap = iconPath.StrokeEndLineCap.ToString().ToLower(),
                        StrokeLineJoin = iconPath.StrokeLineJoin.ToString().ToLower(),
                        TransformMatrix = T.ToString(CultureInfo.InvariantCulture)
                    };

                    var wpfFileContent = ExportHelper.FillTemplate(wpfFileTemplate, parameters);

                    using IO.StreamWriter file = new IO.StreamWriter(fileSaveDialog.FileName);
                    await file.WriteAsync(wpfFileContent);
                }
            }
            catch (Exception e)
            {
                await dialogCoordinator.ShowMessageAsync(MainViewModel, "Error", e.Message);
            }

            await progress.CloseAsync();
        }

        public ICommand SaveAsUwpCommand { get; }

        private async void SaveAsUwp_Execute()
        {
            var progress = await dialogCoordinator.ShowProgressAsync(MainViewModel, "Export", "Saving selected icon as WPF-XAML-file");
            progress.SetIndeterminate();

            try
            {
                var fileSaveDialog = new SaveFileDialog()
                {
                    AddExtension = true,
                    DefaultExt = "xaml",
                    FileName = $"{SelectedIcon.IconPackName}-{SelectedIcon.Name}",
                    Filter = "UWP-XAML (*.xaml)|*.xaml",
                    OverwritePrompt = true
                };

                if (fileSaveDialog.ShowDialog() == true && SelectedIcon is IconViewModel icon)
                {
                    var iconControl = icon.GetPackIconControlBase();

                    iconControl.BeginInit();
                    iconControl.Width = Settings.Default.IconPreviewSize;
                    iconControl.Height = Settings.Default.IconPreviewSize;
                    iconControl.EndInit();
                    iconControl.ApplyTemplate();

                    var iconPath = iconControl.FindChild<Path>();

                    var bBox = iconPath.Data.Bounds;

                    var xamlSize = Math.Max(bBox.Width, bBox.Height);
                    var scaleFactor = Settings.Default.IconPreviewSize / xamlSize;
                    var T = iconPath.LayoutTransform.Value;

                    var wpfFileTemplate = ExportHelper.UwpFileTemplate;

                    var parameters = new ExportParameters(SelectedIcon)
                    {
                        FillColor = iconPath.Fill is not null ? iconPath.Fill.ToString(CultureInfo.InvariantCulture) : "{x:Null}",
                        PathData = iconControl.Data,
                        StrokeColor = iconPath.Stroke is not null ? iconPath.Stroke.ToString(CultureInfo.InvariantCulture) : "{x:Null}",
                        StrokeWidth = iconPath.Stroke is null ? "0" : (scaleFactor * iconPath.StrokeThickness).ToString(CultureInfo.InvariantCulture),
                        StrokeLineCap = iconPath.StrokeEndLineCap.ToString().ToLower(),
                        StrokeLineJoin = iconPath.StrokeLineJoin.ToString().ToLower(),
                        TransformMatrix = T.ToString(CultureInfo.InvariantCulture)
                    };

                    var wpfFileContent = ExportHelper.FillTemplate(wpfFileTemplate, parameters);

                    using IO.StreamWriter file = new IO.StreamWriter(fileSaveDialog.FileName);
                    await file.WriteAsync(wpfFileContent);
                }
            }
            catch (Exception e)
            {
                await dialogCoordinator.ShowMessageAsync(MainViewModel, "Error", e.Message);
            }

            await progress.CloseAsync();
        }

        public ICommand SaveAsPngCommand { get; }

        public ICommand SaveAsJpegCommand { get; }

        public ICommand SaveAsBmpCommand { get; }

        private async void SaveAsBitmapExecute(BitmapEncoder encoder)
        {
            var progress = await dialogCoordinator.ShowProgressAsync(MainViewModel, "Export", "Saving selected icon as bitmap image");
            progress.SetIndeterminate();

            try
            {
                var fileSaveDialog = new SaveFileDialog()
                {
                    AddExtension = true,
                    FileName = $"{SelectedIcon.IconPackName}-{SelectedIcon.Name}",
                    OverwritePrompt = true
                };

                fileSaveDialog.Filter = encoder switch
                {
                    PngBitmapEncoder => "Png-File (*.png)|*.png",
                    JpegBitmapEncoder => "Jpeg-File (*.jpg)|*.jpg",
                    BmpBitmapEncoder => "Bmp-File (*.bmp)|*.bmp",
                    _ => fileSaveDialog.Filter
                };

                if (fileSaveDialog.ShowDialog() == true && SelectedIcon is IconViewModel icon)
                {
                    var canvas = new Canvas
                    {
                        Width = Settings.Default.IconPreviewSize,
                        Height = Settings.Default.IconPreviewSize,
                        Background = new SolidColorBrush(Settings.Default.IconBackground)
                    };

                    var packIconControl = new PackIconControl();
                    packIconControl.BeginInit();
                    packIconControl.Kind = icon.Value as Enum;
                    packIconControl.Width = Settings.Default.IconPreviewSize;
                    packIconControl.Height = Settings.Default.IconPreviewSize;
                    packIconControl.Foreground = new SolidColorBrush(Settings.Default.IconForeground);

                    packIconControl.EndInit();
                    packIconControl.ApplyTemplate();

                    canvas.Children.Add(packIconControl);

                    var size = new Size(Settings.Default.IconPreviewSize, Settings.Default.IconPreviewSize);
                    canvas.Measure(size);
                    canvas.Arrange(new Rect(size));

                    var renderTargetBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render(canvas);

                    encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                    using var fileStream = new IO.FileStream(fileSaveDialog.FileName, IO.FileMode.Create);
                    encoder.Save(fileStream);
                }
            }
            catch (Exception e)
            {
                await dialogCoordinator.ShowMessageAsync(MainViewModel, "Error", e.Message);
            }

            await progress.CloseAsync();
        }
    }

    public interface IIconViewModel
    {
        string Name { get; set; }
        string IconPackName { get; }
        string Description { get; set; }
        Type IconPackType { get; set; }
        Type IconType { get; set; }
        object Value { get; set; }
        MetaDataAttribute MetaData { get; set; }
        string CopyToClipboardText { get; }
        string CopyToClipboardAsContentText { get; }
        string CopyToClipboardAsPathIconText { get; }
        string CopyToClipboardAsGeometryText { get; }
    }

    public class IconViewModel : IIconViewModel
    {
        public IconViewModel(Type enumType, Type packType, Enum k, MetaDataAttribute metaData)
        {
            Name = k.ToString();
            Description = GetDescription(k);
            IconPackType = packType;
            IconType = enumType;
            Value = k;
            MetaData = metaData;
        }

        public string CopyToClipboardText => ExportHelper.FillTemplate(ExportHelper.ClipboardWpf, new ExportParameters(this)); // $"<iconPacks:{IconPackType.Name} Kind=\"{Name}\" />";
        public string CopyToClipboardWpfGeometry => ExportHelper.FillTemplate(ExportHelper.ClipboardWpfGeometry, new ExportParameters(this)); // $"<iconPacks:{IconPackType.Name} Kind=\"{Name}\" />";

        public string CopyToClipboardAsContentText => ExportHelper.FillTemplate(ExportHelper.ClipboardContent, new ExportParameters(this)); // $"{{iconPacks:{IconPackType.Name.Replace("PackIcon", "")} Kind={Name}}}";

        public string CopyToClipboardAsPathIconText => ExportHelper.FillTemplate(ExportHelper.ClipboardUwp, new ExportParameters(this)); // $"<iconPacks:{IconPackType.Name.Replace("PackIcon", "PathIcon")} Kind=\"{Name}\" />";

        public string CopyToClipboardAsGeometryText => ExportHelper.FillTemplate(ExportHelper.ClipboardData, new ExportParameters(this)); // GetPackIconControlBase().Data;

        public string Name { get; set; }

        public string IconPackName => IconPackType.Name.Replace("PackIcon", "");

        public string Description { get; set; }

        public Type IconPackType { get; set; }

        public Type IconType { get; set; }

        public object Value { get; set; }

        public MetaDataAttribute MetaData { get; set; }

        internal PackIconControlBase GetPackIconControlBase()
        {
            if (Activator.CreateInstance(IconPackType) is not PackIconControlBase iconPack) return null;

            var kindProperty = IconPackType.GetProperty("Kind");
            if (kindProperty == null) return null;
            kindProperty.SetValue(iconPack, Value);

            return iconPack;
        }

        internal static string GetDescription(Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            return fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() is DescriptionAttribute attribute ? attribute.Description : value.ToString();
        }
    }
}