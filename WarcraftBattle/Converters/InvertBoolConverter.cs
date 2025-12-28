using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WarcraftBattle.Converters
{
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            if (value is Visibility v) return v == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}