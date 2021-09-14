using MahApps.Metro.Converters;
using System;
using System.Globalization;

namespace IconPacks.Browser
{
    public class StringToDoubleConverter : MarkupConverter
    {
        static StringToDoubleConverter _Instance;

        protected override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return double.TryParse(value.ToString(), out double result) ? result : 0;
        }

        protected override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _Instance ??= new StringToDoubleConverter();
        }
    }
}
