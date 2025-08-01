using System;
using System.Globalization;
using System.Windows.Data;

namespace Pet.TaskDevourer.Helpers
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true ? Enum.Parse(targetType, parameter.ToString()) : Binding.DoNothing;
        }
    }
}