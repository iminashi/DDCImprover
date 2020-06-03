using Rocksmith2014Xml;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DDCImprover.Core
{
    public static class Extensions
    {
        /// <summary>
        /// Converts a time in milliseconds into a string according to the user preferences.
        /// </summary>
        /// <param name="time">The time to convert.</param>
        /// <returns>The time as a string in seconds and milliseconds, or minutes, seconds and milliseconds.</returns>
        public static string TimeToString(this int time)
        {
            if (XMLProcessor.Preferences.DisplayTimesInSeconds)
            {
                return Utils.TimeCodeToString(time);
            }
            else
            {
                int minutes = time / 1000 / 60;
                int seconds = (time / 1000) - (minutes * 60);
                int milliSeconds = time - (minutes * 60 * 1000) - (seconds * 1000);
                return $"{minutes:D2}:{seconds:D2}.{milliSeconds:D3}";
            }
        }

        /// <summary>
        /// Skips a specified number of elements at the end of a sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="enumerable"></param>
        /// <param name="skipCount">Number of elements to skip.</param>
        /// <remarks>https://blogs.msdn.microsoft.com/ericwhite/2008/11/14/the-skiplast-extension-method/</remarks>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> enumerable, int skipCount)
        {
            if (skipCount < 0)
                throw new ArgumentException("Number of elements to skip cannot be negative.");

            return SkipLastImpl(enumerable, skipCount);

            // Implementation as local function
            static IEnumerable<T> SkipLastImpl(IEnumerable<T> source, int count)
            {
                Queue<T> saveList = new Queue<T>(count + 1);
                foreach (T item in source)
                {
                    saveList.Enqueue(item);
                    if (count > 0)
                    {
                        --count;
                        continue;
                    }

                    yield return saveList.Dequeue();
                }
            }
        }

        /// <summary>
        /// Skips the last element at the end of a sequence
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="enumerable"></param>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> enumerable)
            => enumerable.SkipLast(1);

        /// <summary>
        /// Starts a string as a file name for a shell-executed process.
        /// </summary>
        /// <param name="fileName">The file name for the process.</param>
        public static void StartAsProcess(this string fileName)
            => Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
    }
}
