using System;
using System.Globalization;
using System.Windows.Data;

namespace DDCImprover.WPF
{
    internal sealed class TaskbarProgressConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double currentValue = (double)values[0];
            double maximum = (double)values[1];

            if (maximum != 0.0)
                return currentValue / maximum;
            else
                return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
