using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WarcraftBattle.Converters
{
    public class CooldownToGridHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is double currentCD) || !(values[1] is double maxCD))
            {
                return new GridLength(0, GridUnitType.Star);
            }

            if (maxCD <= 0 || currentCD <= 0)
            {
                return new GridLength(0, GridUnitType.Star);
            }

            double percentage = Math.Clamp(currentCD / maxCD, 0.0, 1.0);
            return new GridLength(percentage, GridUnitType.Star);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}