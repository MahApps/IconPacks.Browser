using IconPacks.Browser.Properties;
using IconPacks.Browser.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace IconPacks.Browser.IcoExport
{
    internal class ExportIconViewModel : ViewModelBase
    {
        /// <summary>
        /// Reference to the selected Icon. Backing Var
        /// </summary>
        private IconViewModel icon;

        /// <summary>
        /// class new
        /// </summary>
        public ExportIconViewModel()
        {
            OkCommand = new SimpleCommand(x => OkExecute(x), (_) => true);
            CancelCommand = new SimpleCommand((_) => CancelExecute(), (_) => true);
        }
        /// <summary>
        /// Reference to the form. I know, not MVVM but simple :)
        /// </summary>
        public ExportIconView Frm { get; set; }

        /// <summary>
        /// Reference to the selected Icon. 
        /// </summary>
        public IconViewModel Icon
        {
            get => icon; set
            {
                if (Set(ref icon, value))
                {
                    this.OnPropertyChanged(nameof(RectGeometry));
                }
            }
        }

        /// <summary>
        /// The border around the Icon
        /// </summary>
        public Geometry RectGeometry
        {
            get
            {
                Geometry rg = null;
                if (IcoExportRectEnabled)
                {
                    var Offset = ((256 - IcoExportRectSize) / 2) + (IcoExportPathStrokeThickness / 2);
                    rg = new RectangleGeometry(new System.Windows.Rect(Offset, Offset, Math.Max(IcoExportRectSize - IcoExportPathStrokeThickness, 2), Math.Max(IcoExportRectSize - IcoExportPathStrokeThickness, 2)), IcoExportRectRadius, IcoExportRectRadius);
                }

                return rg;
            }
        }

        /// <summary>
        /// MahIcon Translation
        /// </summary>
        public Transform IconTranslate
        {
            get
            {
                TransformGroup t = new TransformGroup();
                t.Children.Add(new RotateTransform(IcoExportIconRotate));
                t.Children.Add(new TranslateTransform(IcoExportIconTranslateX, IcoExportIconTranslateY));
                return t;
            }
        }

        /// <summary>
        /// The forecolor of the mah-icon
        /// </summary>
        public Color IcoExportIconForeground
        {
            get => Settings.Default.IcoExportIconForeground; set
            {
                Settings.Default.IcoExportIconForeground = value;
                this.OnPropertyChanged(nameof(IcoExportIconForeground));
            }
        }

        /// <summary>
        /// the backcolor of the mahicon on canvas
        /// </summary>
        public Color IcoExportIconBackground
        {
            get => Settings.Default.IcoExportIconBackground; set
            {
                Settings.Default.IcoExportIconBackground = value;
                this.OnPropertyChanged(nameof(IcoExportIconBackground));
                this.OnPropertyChanged(nameof(IcoExportPathBackground));
            }
        }

        /// <summary>
        /// The size of the mahicon on canvas
        /// </summary>
        public int IcoExportIconSize
        {
            get => Settings.Default.IcoExportIconSize; set
            {
                Settings.Default.IcoExportIconSize = value;
                this.OnPropertyChanged(nameof(IcoExportIconSize));
            }
        }

        /// <summary>
        /// Rotation of the mahicon
        /// </summary>
        public int IcoExportIconRotate
        {
            get => Settings.Default.IcoExportIconRotate; set
            {
                Settings.Default.IcoExportIconRotate = value;
                this.OnPropertyChanged(nameof(IconTranslate));
            }
        }

        /// <summary>
        /// Translation of the mahicon
        /// </summary>
        public int IcoExportIconTranslateX
        {
            get => Settings.Default.IcoExportIconTranslateX; set
            {
                Settings.Default.IcoExportIconTranslateX = value;
                this.OnPropertyChanged(nameof(IconTranslate));
            }
        }

        /// <summary>
        /// Translation of the mahicon
        /// </summary>
        public int IcoExportIconTranslateY
        {
            get => Settings.Default.IcoExportIconTranslateY; set
            {
                Settings.Default.IcoExportIconTranslateY = value;
                this.OnPropertyChanged(nameof(IconTranslate));
            }
        }

        /// <summary>
        /// Color of the Background of the rect, when Path is off
        /// </summary>
        public Color IcoExportPathBackground
        {
            get
            {
                if (IcoExportRectEnabled) return Colors.Transparent;
                return Settings.Default.IcoExportIconBackground;
            }
        }

        

        /// <summary>
        /// Fill color of the rect
        /// </summary>
        public Color IcoExportPathFill
        {
            get => Settings.Default.IcoExportPathFill; set
            {
                Settings.Default.IcoExportPathFill = value;
                this.OnPropertyChanged(nameof(IcoExportPathFill));
            }
        }

        /// <summary>
        /// The color of the path stroke
        /// </summary>
        public Color IcoExportPathStroke
        {
            get => Settings.Default.IcoExportPathStroke; set
            {
                Settings.Default.IcoExportPathStroke = value;
                this.OnPropertyChanged(nameof(IcoExportPathStroke));
                this.OnPropertyChanged(nameof(RectGeometry));
            }
        }

        /// <summary>
        /// the stroke width of thge rect
        /// </summary>
        public int IcoExportPathStrokeThickness
        {
            get => Settings.Default.IcoExportPathStrokeThickness; set
            {
                Settings.Default.IcoExportPathStrokeThickness = value;
                this.OnPropertyChanged(nameof(IcoExportPathStrokeThickness));
                this.OnPropertyChanged(nameof(RectGeometry));
            }
        }

        /// <summary>
        /// Size of the rect
        /// </summary>
        public int IcoExportRectSize
        {
            get => Settings.Default.IcoExportRectSize; set
            {
                Settings.Default.IcoExportRectSize = value;
                this.OnPropertyChanged(nameof(RectGeometry));
            }
        }

        /// <summary>
        /// Border Radius of the Rect
        /// </summary>
        public int IcoExportRectRadius
        {
            get => Settings.Default.IcoExportRectRadius; set
            {
                Settings.Default.IcoExportRectRadius = value;
                this.OnPropertyChanged(nameof(RectGeometry));
            }
        }

       

        /// <summary>
        /// Enable the rect
        /// </summary>
        public bool IcoExportRectEnabled
        {
            get => Settings.Default.IcoExportRectEnabled; set
            {
                Settings.Default.IcoExportRectEnabled = value;
                this.OnPropertyChanged(nameof(RectGeometry));
                this.OnPropertyChanged(nameof(IcoExportPathBackground));
            }
        }

        public ICommand OkCommand { get; }

        public ICommand CancelCommand { get; }

        public List<System.Drawing.Bitmap> IconBitmaps { get; set; } = new List<System.Drawing.Bitmap>();

        private void OkExecute(object parameter)
        {
            if (parameter is Grid grid)
            {
                if (Settings.Default.IcoExport256) IconBitmaps.Add(RenderBitmap(grid, 256));
                if (Settings.Default.IcoExport180) IconBitmaps.Add(RenderBitmap(grid, 180));
                if (Settings.Default.IcoExport128) IconBitmaps.Add(RenderBitmap(grid, 128));
                if (Settings.Default.IcoExport96) IconBitmaps.Add(RenderBitmap(grid, 96));
                if (Settings.Default.IcoExport72) IconBitmaps.Add(RenderBitmap(grid, 72));
                if (Settings.Default.IcoExport64) IconBitmaps.Add(RenderBitmap(grid, 64));
                if (Settings.Default.IcoExport48) IconBitmaps.Add(RenderBitmap(grid, 48));
                if (Settings.Default.IcoExport32) IconBitmaps.Add(RenderBitmap(grid, 32));
                if (Settings.Default.IcoExport24) IconBitmaps.Add(RenderBitmap(grid, 24));
                if (Settings.Default.IcoExport16) IconBitmaps.Add(RenderBitmap(grid, 16));
                Frm.DialogResult = true;
            }
        }

        private System.Drawing.Bitmap RenderBitmap(Grid grid, double size)
        {
            grid.SetCurrentValue(Grid.RenderTransformProperty, new ScaleTransform(size / 256, size / 256));
            Canvas.SetLeft(grid, 0);
            Canvas.SetTop(grid, 0);
            grid.Measure(new Size(grid.Width, grid.Height));
            grid.Arrange(new Rect(new Size(grid.Width, grid.Height)));
            grid.UpdateLayout();

            var rtb = new RenderTargetBitmap(
                                    (int)size,
                                    (int)size,
                                    96,
                                    96,
                                    PixelFormats.Pbgra32);



            rtb.Render(grid);
            MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            encoder.Save(stream);

            return new System.Drawing.Bitmap(stream);
        }

        private void CancelExecute()
        {
            Frm.DialogResult = false;
        }

    }
}
