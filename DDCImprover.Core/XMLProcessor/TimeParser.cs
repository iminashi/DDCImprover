using Rocksmith2014Xml;

using System;
using System.Globalization;
using System.Linq;
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
        public static int? Parse(string inputStr)
        {
            int? time;
            try
            {
                Match match = Regex.Match(inputStr, RegPatternMinSecMs);
                if (match.Success)
                {
                    // Minutes
                    if (int.TryParse(match.Groups[1].Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int minutes))
                    {
                        time = 60 * minutes * 1000;
                    }
                    else
                    {
                        return null;
                    }

                    // Seconds and milliseconds
                    int milliseconds = Utils.TimeCodeFromFloatString(match.Groups[2].Value.Replace('s', '.'));
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

                        time = int.Parse(match.Value) * 1000;
                    }
                }

                return time;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public static int FindTimeOfNthNoteFrom(Level level, int startTime, int nthNote)
        {
            var noteTimes = level.Notes
                .Where(n => n.Time >= startTime)
                .Select(n => n.Time)
                .Distinct() // Notes on the same timecode (e.g. split chords) count as one
                .Take(nthNote);

            var chordTimes = level.Chords
                .Where(c => c.Time >= startTime)
                .Select(c => c.Time)
                .Distinct()
                .Take(nthNote);

            var noteAndChordTimes = noteTimes.Concat(chordTimes).OrderBy(time => time);

            return noteAndChordTimes.Skip(nthNote - 1).First();
        }
    }
}
