using IconPacks.Browser.Properties;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using io = System.IO;

namespace IconPacks.Browser.Controls
{
    /// <summary>
    /// Interaction logic for SideBar.xaml
    /// </summary>
    public partial class SideBar : UserControl
    {
        public SideBar()
        {
            InitializeComponent();
        }

        private void IconPreview_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta != 0 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Settings.Default.IconPreviewSize += Math.Sign(e.Delta) * 4;
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                this.PreviewScrollViewer.ScrollToHorizontalOffset(this.PreviewScrollViewer.HorizontalOffset - e.Delta / 3);
                e.Handled = true;
            }
        }


        
    }
}
