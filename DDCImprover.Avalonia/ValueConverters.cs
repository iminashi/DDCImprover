using Avalonia.Data.Converters;
using Avalonia.Media;

using System.Linq;

namespace DDCImprover.Avalonia
{
    public static class ValueConverters
    {
        public static readonly IMultiValueConverter IsEnabledToBrush = new FuncMultiValueConverter<object, IBrush>(
        v =>
        {
            var values = v.ToArray();
            if (values[0] is bool isEnabled && isEnabled && values[1] is string colorStr)
            {
                return Brush.Parse(colorStr);
            }

            return Brushes.Gray;
        });
    }
}
