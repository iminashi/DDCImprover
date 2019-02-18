using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Shell;

namespace DDCImprover.WPF
{
    internal sealed class BooleanToTaskbarProgressStateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isProcessing = (bool)values[0];
            bool isError = (bool)values[1];

            if(isProcessing)
            {
                if (isError)
                    return TaskbarItemProgressState.Error;

                return TaskbarItemProgressState.Normal;
            }

            return TaskbarItemProgressState.None;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
