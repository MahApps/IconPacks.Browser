using System.Windows;
using System.Windows.Controls;

namespace IconPacks.Browser.Controls
{
    /// <summary>
    /// Interaction logic for CopyToClipboardTextBox.xaml
    /// </summary>
    public partial class CopyToClipboardTextBox : UserControl
    {
        /// <summary>Identifies the <see cref="TextToCopy"/> dependency property.</summary>
        public static readonly DependencyProperty TextToCopyProperty
            = DependencyProperty.Register(
                nameof(TextToCopy),
                typeof(string),
                typeof(CopyToClipboardTextBox),
                new PropertyMetadata(default(string)));

        public string TextToCopy
        {
            get => (string)GetValue(TextToCopyProperty);
            set => SetValue(TextToCopyProperty, value);
        }

        public CopyToClipboardTextBox()
        {
            InitializeComponent();
        }
    }
}