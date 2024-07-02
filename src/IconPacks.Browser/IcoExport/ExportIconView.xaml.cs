using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IconPacks.Browser.IcoExport
{
    /// <summary>
    /// Interaktionslogik für ExportIconView.xaml
    /// </summary>
    public partial class ExportIconView : MetroWindow
    {
        public ExportIconView()
        {
            InitializeComponent();
            PreviewHolder.LayoutUpdated += PreviewHolder_LayoutUpdated;
        }

        private void PreviewHolder_LayoutUpdated(object sender, EventArgs e)
        {
            //PreviewBorder.SetCurrentValue(Path.WidthProperty, PreviewHolder.Width);
            //PreviewBorder.SetCurrentValue(Path.HeightProperty, PreviewHolder.Height);
            //PreviewBorder.SetCurrentValue(Path.MarginProperty, PreviewHolder.Margin);

        }

        private void Slider_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ((Slider)sender).SetCurrentValue(Slider.ValueProperty,0.0);
        }
    }
}
