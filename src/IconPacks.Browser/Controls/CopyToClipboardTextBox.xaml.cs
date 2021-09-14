using System.Windows;
using System.Windows.Controls;

namespace IconPacks.Browser.Controls
{
    /// <summary>
    /// Interaction logic for CopyToClipboardTextBox.xaml
    /// </summary>
    public partial class CopyToClipboardTextBox : UserControl
    {
        public CopyToClipboardTextBox()
        {
            InitializeComponent();
        }


        public string TextToCopy
        {
            get => (string)GetValue(TextToCopyProperty);
            set => SetValue(TextToCopyProperty, value);
        }

        // Using a DependencyProperty as the backing store for TextToCopy.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextToCopyProperty =
            DependencyProperty.Register("TextToCopy", typeof(string), typeof(CopyToClipboardTextBox), new PropertyMetadata(null));


    }
}
