using Rocksmith2014Xml;

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DDCImprover.Core
{
    internal static class TimeParser
    {
        private const string RegPatternMinSecMs = @"(\d+)m(\d+s\d+)$";
        private const string RegPatternSecMs = @"\d+s\d+$";

        /// <summary>
        /// Tries to parse a time value from the end of the input string either in the format "0m0s0" or "0s0".
        /// </summary>
        /// <param name="inputStr">Input string.</param>
        /// <returns>The parsed time value in milliseconds, null if unsuccessful.</returns>
        public static uint? Parse(string inputStr)
        {
            uint? time;
            try
            {
                Match match = Regex.Match(inputStr, RegPatternMinSecMs);
                if (match.Success)
                {
                    // Minutes
                    if (uint.TryParse(match.Groups[1].Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out uint minutes))
                    {
                        time = 60 * minutes * 1000;
                    }
                    else
                    {
                        return null;
                    }

                    // Seconds and milliseconds
                    uint milliseconds = Utils.TimeCodeFromFloatString(match.Groups[2].Value.Replace('s', '.'));
                    time += milliseconds;
                }
                else
                {
                    match = Regex.Match(inputStr, RegPatternSecMs);
                    if (match.Success)
                    {
                        time = Utils.TimeCodeFromFloatString(match.Value.Replace('s', '.'));
                    }
                    else
                    {
                        match = Regex.Match(inputStr, @"\d+$");
                        if (!match.Success)
                        {
                            return null;
                        }

                        time = uint.Parse(match.Value) * 1000;
                    }
                }

                return time;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
