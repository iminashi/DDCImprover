using System;
using System.Globalization;
using System.Windows.Data;

namespace DDCImprover.WPF
{
    internal sealed class BooleanInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool valBool)
                return !valBool;

            throw new InvalidOperationException("Value must be a boolean.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
