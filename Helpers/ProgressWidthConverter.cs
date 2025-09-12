using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Pet.TaskDevourer.Helpers
{
    public class ProgressWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return 0d;

            double totalWidth = values[0] is double dw ? dw : 0d;
            int completed = 0;
            int total = 0;

            if (values[1] is int c) completed = c;
            else if (values[1] is IConvertible conv1) completed = conv1.ToInt32(culture);

            if (values[2] is int t) total = t;
            else if (values[2] is IConvertible conv2) total = conv2.ToInt32(culture);

            if (total <= 0) return 0d;
            if (completed <= 0) return 0d;

            double ratio = Math.Max(0d, Math.Min(1d, (double)completed / total));
            return totalWidth * ratio;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
