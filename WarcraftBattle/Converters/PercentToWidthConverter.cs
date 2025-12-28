using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace WarcraftBattle.Converters
{
    public class PercentToWidthConverter :MarkupExtension, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 0;
            if (values[0] is double pct && values[1] is double totalWidth)
            {
                if (pct < 0) pct = 0;
                if (pct > 1) pct = 1;
                return pct * totalWidth;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}