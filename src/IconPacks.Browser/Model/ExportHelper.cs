using System;
using System.Globalization;
using System.IO;
using System.Windows.Media;

using IconPacks.Browser.Properties;
using IconPacks.Browser.ViewModels;

namespace IconPacks.Browser.Model
{
    internal class ExportHelper
    {
        // SVG-File
        private static string _SvgFileTemplate;

        internal static string SvgFileTemplate => _SvgFileTemplate ??= LoadTemplateString("SVG.xml");

        // XAML-File (WPF)
        private static string _WpfFileTemplate;

        internal static string WpfFileTemplate => _WpfFileTemplate ??= LoadTemplateString("WPF.xml");

        // XAML-File (WPF)
        private static string _UwpFileTemplate;

        internal static string UwpFileTemplate => _UwpFileTemplate ??= LoadTemplateString("WPF.xml");

        // Clipboard - WPF
        private static string _ClipboardWpf;

        internal static string ClipboardWpf => _ClipboardWpf ??= File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportTemplates", "Clipboard.WPF.xml"));
        
        // Clipboard - WPF
        private static string _ClipboardWpfGeometry;

        internal static string ClipboardWpfGeometry => _ClipboardWpfGeometry ??= File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportTemplates", "Clipboard.WPF.Geometry.xml"));

        // Clipboard - UWP
        private static string _ClipboardUwp;

        internal static string ClipboardUwp => _ClipboardUwp ??= File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportTemplates", "Clipboard.UWP.xml"));

        // Clipboard - Content
        private static string _ClipboardContent;

        internal static string ClipboardContent => _ClipboardContent ??= File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportTemplates", "Clipboard.Content.xml"));

        // Clipboard - PathData
        private static string _ClipboardData;

        internal static string ClipboardData => _ClipboardData ??= File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportTemplates", "Clipboard.PathData.xml"));

        internal static string FillTemplate(string template, ExportParameters parameters)
        {
            return template.Replace("@IconKind", parameters.IconKind)
                .Replace("@IconPackName", parameters.IconPackName)
                .Replace("@IconPackHomepage", parameters.IconPackHomepage)
                .Replace("@IconPackLicense", parameters.IconPackLicense)
                .Replace("@PageWidth", parameters.PageWidth)
                .Replace("@PageHeight", parameters.PageHeight)
                .CheckedReplace("@PathData", () => parameters.PathData) // avoid allocation of Lazy<string>
                .Replace("@FillColor", parameters.FillColor)
                .Replace("@Background", parameters.Background)
                .Replace("@StrokeColor", parameters.StrokeColor)
                .Replace("@StrokeWidth", parameters.StrokeWidth)
                .Replace("@StrokeLineCap", parameters.StrokeLineCap)
                .Replace("@StrokeLineJoin", parameters.StrokeLineJoin)
                .Replace("@TransformMatrix", parameters.TransformMatrix);
        }

        internal static string LoadTemplateString(string fileName)
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.ExportTemplatesDir) || !File.Exists(Path.Combine(Settings.Default.ExportTemplatesDir, fileName)))
            {
                return File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportTemplates", fileName));
            }
            else
            {
                return File.ReadAllText(Path.Combine(Settings.Default.ExportTemplatesDir, fileName));
            }
        }
    }

    internal struct ExportParameters
    {
        /// <summary>
        /// Provides a default set of Export parameters. You should edit this to your needs.
        /// </summary>
        /// <param name="icon"></param>
        internal ExportParameters(IIconViewModel icon)
        {
            var metaData = icon.MetaData;

            this.IconKind = icon.Name;
            this.IconPackName = icon.IconPackType.Name.Replace("PackIcon", "");
            this.PageWidth = Settings.Default.IconPreviewSize.ToString(CultureInfo.InvariantCulture);
            this.PageHeight = Settings.Default.IconPreviewSize.ToString(CultureInfo.InvariantCulture);
            this.FillColor = Settings.Default.IconForeground.ToString(CultureInfo.InvariantCulture);
            this.Background = Settings.Default.IconBackground.ToString(CultureInfo.InvariantCulture);
            this.StrokeColor = Settings.Default.IconForeground.ToString(CultureInfo.InvariantCulture);
            this.StrokeWidth = "0";
            this.StrokeLineCap = PenLineCap.Round.ToString();
            this.StrokeLineJoin = PenLineJoin.Round.ToString();
            this.PathData = null;
            this.TransformMatrix = Matrix.Identity.ToString(CultureInfo.InvariantCulture);

            this.IconPackHomepage = metaData?.ProjectUrl;
            this.IconPackLicense = metaData?.LicenseUrl;

            //this.PathData = (icon as IconViewModel)?.GetPackIconControlBase().Data;
            _pathDataLazy = new Lazy<string>(() => (icon as IconViewModel)?.GetPackIconControlBase().Data);
        }

        private Lazy<string> _pathDataLazy;
        private string _pathData;

        internal string IconKind { get; set; }
        internal string IconPackName { get; set; }
        internal string IconPackHomepage { get; set; }
        internal string IconPackLicense { get; set; }
        internal string PageWidth { get; set; }
        internal string PageHeight { get; set; }

        internal string PathData
        {
            get => _pathData ?? _pathDataLazy.Value;
            set => _pathData = value;
        }

        internal string FillColor { get; set; }
        internal string Background { get; set; }
        internal string StrokeColor { get; set; }
        internal string StrokeWidth { get; set; }
        internal string StrokeLineCap { get; set; }
        internal string StrokeLineJoin { get; set; }
        internal string TransformMatrix { get; set; }
    }

    internal static class ExportHelperExtensions
    {
        internal static string CheckedReplace(this string input, string oldValue, Func<string> newValue)
        {
            if (input.Contains(oldValue))
            {
                return input.Replace(oldValue, newValue());
            }

            return input;
        }
    }
}