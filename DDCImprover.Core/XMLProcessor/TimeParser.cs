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
        /// <param name="inputStr">Input string</param>
        /// <returns>The parsed time value in seconds, null if unsuccessful</returns>
        public static float? Parse(string inputStr)
        {
            float? time;

            Match match = Regex.Match(inputStr, RegPatternMinSecMs);
            if (match.Success)
            {
                // Minutes
                if (int.TryParse(match.Groups[1].Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int minutes))
                {
                    time = 60 * minutes;
                }
                else
                {
                    return null;
                }

                // Seconds and milliseconds
                if (float.TryParse(match.Groups[2].Value.Replace('s', '.'), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out float seconds))
                {
                    time += seconds;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                match = Regex.Match(inputStr, RegPatternSecMs);
                if (match.Success)
                {
                    time = float.Parse(match.Value.Replace('s', '.'), NumberFormatInfo.InvariantInfo);
                }
                else
                {
                    match = Regex.Match(inputStr, @"\d+$");
                    if (!match.Success)
                    {
                        return null;
                    }

                    time = float.Parse(match.Value, NumberFormatInfo.InvariantInfo);
                }
            }

            return time;
        }
    }
}
