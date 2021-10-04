using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace IconPacks.Browser.Controls
{
    [TemplatePart(Name = nameof(PART_ResizingThumb), Type = typeof(Thumb))]
    public class SidebarExpander : Expander
    {
        /// <summary>Identifies the <see cref="AnimateWidthFactor"/> dependency property.</summary>
        public static readonly DependencyProperty AnimateWidthFactorProperty
            = DependencyProperty.Register(
                nameof(AnimateWidthFactor),
                typeof(double),
                typeof(SidebarExpander),
                new PropertyMetadata(0d));

        public double AnimateWidthFactor
        {
            get => (double)GetValue(AnimateWidthFactorProperty);
            set => SetValue(AnimateWidthFactorProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            PART_ResizingThumb = GetTemplateChild(nameof(PART_ResizingThumb)) as Thumb;
            if (PART_ResizingThumb != null)
            {
                PART_ResizingThumb.DragDelta += PART_ResizingThumb_DragDelta;
            }
        }

        private void PART_ResizingThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // We only want to resize if we are open
            if (IsExpanded)
            {
                var newWidth = ActualWidth - e.HorizontalChange;
                if (newWidth < MinWidth)
                {
                    newWidth = MinWidth;
                }

                if (newWidth > MaxWidth)
                {
                    newWidth = MaxWidth;
                }

                IconPacks.Browser.Properties.Settings.Default.SidebarExpandedWidth = newWidth;
            }
        }

        private Thumb PART_ResizingThumb;
    }
}