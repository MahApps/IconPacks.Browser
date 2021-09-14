using IconPacks.Browser.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace IconPacks.Browser.Model
{
    class NavigationItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate IconPackTempalte { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is IconPackViewModel)
            {
                return IconPackTempalte;
            }
            else
            {
                return base.SelectTemplate(item, container);
            }
        }
    }
}
