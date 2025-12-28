using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WarcraftBattle.Converters
{
    public class CooldownToPathConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 4 ||
                !(values[0] is double currentCd) ||
                !(values[1] is double maxCd) ||
                !(values[2] is double width) ||
                !(values[3] is double height))
            {
                return null;
            }

            if (maxCd <= 0 || currentCd <= 0)
            {
                return null;
            }

            double percentage = Math.Clamp(currentCd / maxCd, 0, 1);
            double angle = 360 * percentage;

            Point center = new Point(width / 2, height / 2);
            double radius = Math.Min(width, height) / 2;

            double angleRad = (angle - 90) * (Math.PI / 180); // Start from top
            Point arcEnd = new Point(center.X + radius * Math.Cos(angleRad), center.Y + radius * Math.Sin(angleRad));

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = center, IsClosed = true };

            pathFigure.Segments.Add(new LineSegment(new Point(center.X, center.Y - radius), true));
            pathFigure.Segments.Add(new ArcSegment(arcEnd, new Size(radius, radius), 0, angle > 180, SweepDirection.Clockwise, true));

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}