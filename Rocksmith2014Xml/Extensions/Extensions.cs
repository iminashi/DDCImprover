using System;

namespace Rocksmith2014Xml.Extensions
{
    public static class StringExtensions
    {
        public static bool IgnoreCaseContains(this string @this, string substring)
            => @this.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
