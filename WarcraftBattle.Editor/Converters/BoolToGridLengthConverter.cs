using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WarcraftBattle.Editor.Converters
{
    public class BoolToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible && isVisible)
            {
                return ParseLength(parameter);
            }

            return new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static GridLength ParseLength(object parameter)
        {
            if (parameter is string lengthText)
            {
                if (lengthText.EndsWith("*", StringComparison.Ordinal))
                {
                    var weightText = lengthText.TrimEnd('*');
                    var weight = string.IsNullOrWhiteSpace(weightText) ? 1 : double.Parse(weightText, CultureInfo.InvariantCulture);
                    return new GridLength(weight, GridUnitType.Star);
                }

                if (double.TryParse(lengthText, NumberStyles.Any, CultureInfo.InvariantCulture, out var pixels))
                {
                    return new GridLength(pixels);
                }
            }

            return GridLength.Auto;
        }
    }
}
