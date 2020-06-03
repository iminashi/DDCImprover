using System;
using System.Diagnostics;

namespace Rocksmith2014Xml
{
    public static class Utils
    {
        /// <summary>
        /// Parses a number from a string that is assumed to be either "0" or "1".
        /// </summary>
        /// <param name="text">The input string.</param>
        /// <returns>Either 0 or 1.</returns>
        public static byte ParseBinary(string text)
        {
            //Debug.Assert(text.Length == 1 && (text[0] == '0' || text[0] == '1'));

            char c = text[0];
            unchecked
            {
                c -= '0';
            }

            if (c >= 1)
                return 1;
            else
                return 0;
        }

        /// <summary>
        /// Converts a time in milliseconds into a string in seconds with three decimal places.
        /// </summary>
        /// <param name="timeCode">The time in milliseconds.</param>
        /// <returns>A string containing the time in seconds.</returns>
        public static string TimeCodeToString(int timeCode)
        {
            string str = timeCode.ToString();
            if (str.Length == 1)
            {
                return "0.00" + str;
            }
            else if (str.Length == 2)
            {
                return "0.0" + str;
            }
            else if (str.Length == 3)
            {
                return "0." + str;
            }
            else
            {
                // Do the hot path without string concatenation
                Span<char> result = stackalloc char[str.Length + 1];

                str.AsSpan(0, str.Length - 3).CopyTo(result);
                result[^4] = '.';

                Span<char> slice = result.Slice(result.Length - 3);
                str.AsSpan(str.Length - 3).CopyTo(slice);

                return result.ToString();
            }
        }

        /// <summary>
        /// Parses a time in milliseconds from a string that is in seconds.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The parsed time in milliseconds.</returns>
        public static int TimeCodeFromFloatString(string input)
        {
            int separatorIndex = input.IndexOf('.');

            // No separator, just convert from seconds to milliseconds
            if (separatorIndex == -1)
                return int.Parse(input) * 1000;

            // Copy the numbers before the decimal separator
            Span<char> temp = stackalloc char[separatorIndex + 3];
            input.AsSpan(0, separatorIndex).CopyTo(temp);

            // Copy at most three numbers after the decimal separator
            var decimals = input.AsSpan(separatorIndex + 1, Math.Min(input.Length - 1 - separatorIndex, 3));
            decimals.CopyTo(temp.Slice(separatorIndex));

            // If there were less than three numbers after the decimal separator, fill the end with zeros
            int i = temp.Length - 1;
            while (temp[i] == '\0')
                temp[i--] = '0';

            return int.Parse(temp);
        }

        internal static int ShiftAndWrap(int value, int positions)
        {
            positions &= 0x1F;

            // Save the existing bit pattern, but interpret it as an unsigned integer.
            uint number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);

            // Preserve the bits to be discarded.
            uint wrapped = number >> (32 - positions);

            // Shift and wrap the discarded bits.
            return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0);
        }
    }
}
